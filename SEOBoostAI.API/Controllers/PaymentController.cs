using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Net.payOS.Types;
using PayOS;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.Enums;
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
		private readonly IUserService _userService;
		private readonly string _returnUrl;
		private readonly string _cancelUrl;

		public PaymentController(Net.payOS.PayOS payOS, ITransactionService transactionService, ISystemConfigService systemConfigService, IUserService userService)
		{
			_payOS = payOS;
			_transactionService = transactionService;
			_systemConfigService = systemConfigService;
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

				if (request.Amount > 100000000)
				{
					return BadRequest(new { message = "Số tiền nạp quá lớn. Vui lòng liên hệ admin." });
				}

				// 1. TẠO MÃ ĐƠN HÀNG
				long orderCode = long.Parse(DateTime.UtcNow.ToString("yyMMddHHmmss") + new Random().Next(100, 999));

				// 3. TẠO TRANSACTION "PENDING"
				var newTransaction = await _transactionService.CreatePendingDeposit(
					userId,
					request.Amount,
					"PayOS",
					null ,// GatewayTransactionId sẽ cập nhật sau
					orderCode
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

				string paymentLinkId = verifiedData.paymentLinkId;
				long orderCode = verifiedData.orderCode;

				// 1. TÌM GIAO DỊCH (CƠ CHẾ DỰ PHÒNG)
				// Ưu tiên tìm bằng mã chuỗi (paymentLinkId)
				var transaction = await _transactionService.GetByGatewayTransactionIdAsync(paymentLinkId);

				// Nếu không thấy, tìm bằng mã số (orderCode)
				if (transaction == null)
				{
					transaction = await _transactionService.GetByGatewayTransactionIdAsync(orderCode.ToString());
				}

				// Nếu vẫn không thấy -> Trả về OK để PayOS không spam lỗi nữa
				if (transaction == null) return Ok(new { message = "Transaction not found" });

				// 2. XỬ LÝ TRẠNG THÁI
				if (verifiedData.code == "00") // Thành công
				{
					string bankTransInfo = !string.IsNullOrEmpty(verifiedData.accountNumber) ? verifiedData.accountNumber : "";

					// Cập nhật trạng thái COMPLETED
					await _transactionService.UpdateTransactionStatusAsync(
						transaction.GatewayTransactionId, // Dùng ID chuẩn từ DB
						PaymentStatus.COMPLETED.ToString(),
						verifiedData.reference,
						bankTransInfo
					);

					// 3. CỘNG TIỀN (CHỈ CỘNG NẾU CHƯA CỘNG)
					// Refresh lại dữ liệu từ DB để lấy BalanceAfter mới nhất
					var refreshedTransaction = await _transactionService.GetTransactionByIdAsync(transaction.TransactionID);

					// Logic quan trọng: Chỉ cộng khi BalanceAfter là NULL (nghĩa là chưa từng cộng tiền)
					// Và tiền phải khớp
					if (refreshedTransaction.BalanceAfter == null && refreshedTransaction.Money == verifiedData.amount)
					{
						await _userService.TopUpAsync(
							refreshedTransaction.UserID,
							refreshedTransaction.Money,
							refreshedTransaction.TransactionID
						);
					}
				}
				else // Thất bại
				{
					await _transactionService.UpdateTransactionStatusAsync(
						transaction.GatewayTransactionId,
						PaymentStatus.FAILED.ToString(),
						verifiedData.reference,
						"Lỗi: " + verifiedData.desc
					);
				}
				return Ok(new { message = "Webhook processed" });
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
				if (transaction.Status == PaymentStatus.COMPLETED.ToString()) return Ok(new { status = PaymentStatus.COMPLETED.ToString(), message = "Giao dịch đã thành công" });

				if (transaction.Status == PaymentStatus.CANCELED.ToString() || transaction.Status == PaymentStatus.FAILED.ToString()) return Ok(new { status = PaymentStatus.CANCELED.ToString(), message = "Giao dịch đã bị hủy hoặc thất bại" });

				// --- TRƯỜNG HỢP THÀNH CÔNG ---
				if (paymentLinkInfo.status == PaymentStatus.PAID.ToString())
				{
					// Lấy thông tin người chuyển (nếu có)
					string bankInfo = "";
					var transactionInfo = paymentLinkInfo.transactions.FirstOrDefault();
					if (transactionInfo != null)
					{
						bankInfo = $"{transactionInfo.counterAccountBankName} {transactionInfo.counterAccountNumber}";
					}

					await _transactionService.UpdateTransactionStatusAsync(
						paymentLinkId,
						PaymentStatus.COMPLETED.ToString(),
						paymentLinkInfo.id,
						bankInfo
					);

					// Cập nhật vào DB
					if (transaction.BalanceAfter == null)
					{
						await _userService.TopUpAsync(
							transaction.UserID,
							transaction.Money,
							transaction.TransactionID
						);
					}

					return Ok(new { status = PaymentStatus.COMPLETED.ToString(), message = "Giao dịch thành công" });
				}
				// --- TRƯỜNG HỢP HỦY HOẶC HẾT HẠN ---
				else if (paymentLinkInfo.status == PaymentStatus.CANCELED.ToString() || paymentLinkInfo.status == PaymentStatus.EXPIRED.ToString())
				{
					await _transactionService.UpdateTransactionStatusAsync(
						paymentLinkId,
						PaymentStatus.CANCELED.ToString(),
						paymentLinkInfo.id,
						"Người dùng hủy hoặc link hết hạn"
					);

					return Ok(new { status = PaymentStatus.CANCELED.ToString(), message = "Giao dịch đã bị hủy" });
				}
				// --- VẪN TREO ---
				return Ok(new { status = PaymentStatus.PENDING.ToString(), message = "Đang chờ thanh toán" });
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