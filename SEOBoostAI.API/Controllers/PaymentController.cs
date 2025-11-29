using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using PayOS;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;
using System.Security.Claims;
using static OpenQA.Selenium.PrintOptions;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/payment")]
	[ApiController]
	[Authorize]
	public class PaymentController : ControllerBase
	{
		private readonly Net.payOS.PayOS _payOS;
		private readonly ITransactionService _transactionService;
		private readonly ISystemConfigService _systemConfigService;
		private readonly IWalletService _walletService;
		private readonly IUserService _userService;
		private readonly string _returnUrl;
		private readonly string _cancelUrl;

		public PaymentController(Net.payOS.PayOS payOS, ITransactionService transactionService, ISystemConfigService systemConfigService , IWalletService walletService, IUserService userService)
		{
			_payOS = payOS;
			_transactionService = transactionService;
			_systemConfigService = systemConfigService;
			_walletService = walletService;
			_userService = userService;
			_returnUrl = _systemConfigService.GetValue<string>("Payment:ReturnUrl","");
			_cancelUrl = _systemConfigService.GetValue<string>("Payment:CancelUrl","");
		}

		[Authorize]
		[HttpPost("create-payment-link")]
		public async Task<IActionResult> CreatePaymentLink([FromBody] PaymentLinkRequest request)
		{
			try
			{
				// 1. Validate User (Lấy từ Token cho an toàn)
				var userIdString = User.FindFirst("user_ID")?.Value;
				if (string.IsNullOrEmpty(userIdString))
				{
					return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong Token." });
				}

				var userId = int.Parse(userIdString);

				if (request.Amount < 10000)
				{
					return BadRequest(new { message = "Số tiền nạp tối thiểu là 10.000 VNĐ." });
				}

				// PayOS cũng có giới hạn tối đa (thường là 100 triệu hoặc tùy hạn mức)
				if (request.Amount > 100000000)
				{
					return BadRequest(new { message = "Số tiền nạp quá lớn. Vui lòng liên hệ admin." });
				}

				// 2. Lấy WalletID từ UserID
				var wallet = await _walletService.GetWalletByUserIdAsync(userId);

				if (wallet == null)
				{
					return BadRequest(new { message = "Ví người dùng không tồn tại." });
				}

				// 1. TẠO MÃ ĐƠN HÀNG
				long orderCode = long.Parse(DateTime.UtcNow.AddHours(7).ToString("yyMMddHHmmss") + new Random().Next(100, 999));

				// 3. TẠO TRANSACTION "PENDING"
				var newTransaction = await _transactionService.CreatePendingDeposit(
					wallet.WalletID,
					request.Amount,
					"PayOS",
					null // GatewayTransactionId sẽ cập nhật sau
				);

				var expiredAt = (int)DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();

				var paymentData = new PaymentData(
					orderCode: orderCode,
					amount: request.Amount,
					description: $"SEOBoostAI - Nap tien ",
					items: new List<ItemData>(),
					cancelUrl: _cancelUrl,
					returnUrl: _returnUrl,
					expiredAt: expiredAt
				);

				CreatePaymentResult result = await _payOS.createPaymentLink(paymentData);

				if (!string.IsNullOrEmpty(result.checkoutUrl))
				{
					// Lấy ID từ URL: "https://pay.payos.vn/web/..."
					string paymentLinkId = result.checkoutUrl.Substring(result.checkoutUrl.LastIndexOf("/") + 1);

					// Cập nhật vào Transaction ngay lúc này
					newTransaction.GatewayTransactionId = paymentLinkId;
					await _transactionService.UpdateAsync(newTransaction);
				}
				return Ok(new { checkoutUrl = result.checkoutUrl });
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				return BadRequest(new { message = ex.Message });
			}
		}

		[AllowAnonymous]
		[HttpPost("webhook")]
		public async Task<IActionResult> HandleWebhook([FromBody] WebhookType webhookBody)
		{
			try
			{
				WebhookData verifiedData = _payOS.verifyPaymentWebhookData(webhookBody);

				// --- Lấy mã chuỗi từ Webhook ---
				string paymentLinkId = verifiedData.paymentLinkId;
				// ------------------------------------------

				if (verifiedData.code == "00") // Thành công
				{
					string bankTransInfo = "";
					if (!string.IsNullOrEmpty(verifiedData.accountNumber)) bankTransInfo = verifiedData.accountNumber;

					// GỌI SERVICE: Truyền paymentLinkId (chuỗi) vào để tìm
					await _transactionService.UpdateTransactionStatusAsync(
						paymentLinkId, 
						"COMPLETED",
						verifiedData.reference,
						bankTransInfo
					);

					// Tìm lại để cộng tiền (Tìm bằng paymentLinkId)
					var transaction = await _transactionService.GetByGatewayTransactionIdAsync(paymentLinkId);

					if (transaction != null && transaction.Status == "COMPLETED" && transaction.Money == verifiedData.amount)
					{
						await _walletService.TopUp(transaction.WalletID, transaction.Money);
					}
				}
				else // Thất bại
				{
					await _transactionService.UpdateTransactionStatusAsync(
						paymentLinkId, // <-- Truyền chuỗi
						"FAILED",
						verifiedData.reference,
						"Lỗi: " + verifiedData.desc
					);
				}
				return Ok();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Webhook failed: {ex.Message}");
				return BadRequest();
			}
		}

		[HttpGet("{orderCode}")]
		public async Task<IActionResult> GetPaymentStatus(long orderCode)
		{
			try
			{
				// 1. Hỏi PayOS trước để lấy thông tin mới nhất (bao gồm paymentLinkId)
				PaymentLinkInformation paymentLinkInfo = await _payOS.getPaymentLinkInformation(orderCode);

				// 2. Lấy paymentLinkId từ PayOS trả về
				string paymentLinkId = paymentLinkInfo.id;

				// 3. Tìm trong DB bằng paymentLinkId
				var transaction = await _transactionService.GetByGatewayTransactionIdAsync(paymentLinkId);

				if (transaction == null)
				{
					return NotFound("Không tìm thấy giao dịch");
				}

				// 2. Nếu DB đã chốt trạng thái -> Trả về luôn
				if (transaction.Status == "COMPLETED")
					return Ok(new { status = "COMPLETED", message = "Giao dịch đã thành công" });

				if (transaction.Status == "CANCELED" || transaction.Status == "FAILED")
					return Ok(new { status = "CANCELED", message = "Giao dịch đã bị hủy hoặc thất bại" });


				// --- TRƯỜNG HỢP THÀNH CÔNG ---
				if (paymentLinkInfo.status == "PAID")
				{
					// Lấy thông tin người chuyển (nếu có)
					string bankInfo = "";
					var transactionInfo = paymentLinkInfo.transactions.FirstOrDefault();
					if (transactionInfo != null)
					{
						bankInfo = $"{transactionInfo.counterAccountBankName} {transactionInfo.counterAccountNumber}";
					}

					// Cập nhật vào DB
					await _transactionService.UpdateTransactionStatusAsync(
						paymentLinkId,
						"COMPLETED",
						paymentLinkInfo.id,
						bankInfo
					);

					// Cộng tiền (Chỉ cộng nếu trạng thái lúc lấy ra từ DB là PENDING)
					// Để tránh cộng dồn nếu hàm này được gọi nhiều lần cùng lúc
					if (transaction.Status == "PENDING")
					{
						await _walletService.TopUp(transaction.WalletID, transaction.Money);
					}

					return Ok(new { status = "COMPLETED", message = "Giao dịch thành công" });
				}

				// --- TRƯỜNG HỢP HỦY HOẶC HẾT HẠN ---
				else if (paymentLinkInfo.status == "CANCELLED" || paymentLinkInfo.status == "EXPIRED")
				{
					await _transactionService.UpdateTransactionStatusAsync(
						paymentLinkId,
						"CANCELED",
						paymentLinkInfo.id,
						"Người dùng hủy hoặc link hết hạn"
					);

					return Ok(new { status = "CANCELED", message = "Giao dịch đã bị hủy" });
				}

				// --- VẪN TREO ---
				return Ok(new { status = "PENDING", message = "Đang chờ thanh toán" });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[Authorize]
		[HttpGet("history")]
		public async Task<IActionResult> GetPaymentHistory([FromQuery] int page =1, [FromQuery] int pageSize = 10)
		{
			try
			{
				// 1. Lấy UserID từ Token
				var userIdString = User.FindFirst("user_ID")?.Value;
				if (string.IsNullOrEmpty(userIdString))
				{
					return Unauthorized("User ID not found.");
				}
				var userId = int.Parse(userIdString);

				// 2. Gọi Service lấy dữ liệu
				var result = await _transactionService.GetUserPaymentHistoryAsync(userId, page, pageSize);

				return Ok(new { data = result });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[Authorize]
		[HttpPost("buy-quota")]
		public async Task<IActionResult> BuyQuota([FromBody] PurchaseRequest request)
		{
			try
			{
				// 1. Validate User (Lấy từ Token cho an toàn)
				var userIdString = User.FindFirst("user_ID")?.Value;
				if (string.IsNullOrEmpty(userIdString))
				{
					return Unauthorized(new { message = "Không tìm thấy thông tin người dùng trong Token." });
				}

				var userId = int.Parse(userIdString);

				// 2. Gọi Service xử lý mua
				await _transactionService.PurchaseFeatureAsync(userId, request.FeatureId, request.Quantity);

				return Ok(new { message = "Mua gói thành công! Số lượt đã được cộng thêm." });
			}
			catch (InvalidOperationException ex) // Lỗi do không đủ tiền
			{
				return BadRequest(new { message = ex.Message, errorCode = "INSUFFICIENT_FUNDS" });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Lỗi hệ thống: " + ex.Message });
			}
		}
	}
}