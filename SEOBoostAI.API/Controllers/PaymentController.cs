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
	public class PaymentController : ControllerBase
	{
		private readonly Net.payOS.PayOS _payOS;
		private readonly ITransactionService _transactionService;
		private readonly IWalletService _walletService;
		private readonly IUserService _userService;

		public PaymentController(Net.payOS.PayOS payOS, ITransactionService transactionService, IWalletService walletService, IUserService userService)
		{
			_payOS = payOS;
			_transactionService = transactionService;
			_walletService = walletService;
			_userService = userService;
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

				// 2. Lấy WalletID từ UserID
				var wallet = await _walletService.GetWalletByUserIdAsync(userId);

				if (wallet == null)
				{
					return BadRequest(new { message = "Ví người dùng không tồn tại." });
				}

				// 3. TẠO TRANSACTION "PENDING"
				var newTransaction = await _transactionService.CreatePendingDeposit(
					wallet.WalletID,
					request.Amount,
					"PayOS"
				);

				// 4. Dùng TransactionID làm Order Code
				int orderCode = newTransaction.TransactionID;

				var paymentData = new PaymentData(
					orderCode: orderCode,
					amount: request.Amount,
					description: $"SEOBoostAI - Nap tien (ID: {orderCode})",
					items: new List<ItemData>(), // Provide an empty list or populate as needed
					cancelUrl: "http://your-react-app-domain.com/payment/failed",
					returnUrl: "http://your-react-app-domain.com/payment/success"
				);

				// SỬA LỖI 1: createPaymentLink -> CreatePaymentLink
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

				if (verifiedData.code == "00") // Thanh toán thành công
				{
					int transactionId = (int)verifiedData.orderCode;
					string gatewayTransId = verifiedData.reference;
					decimal amountPaid = verifiedData.amount;
					string bankTransInfo = "";

					if (!string.IsNullOrEmpty(verifiedData.accountNumber))
					{
						bankTransInfo = verifiedData.accountNumber;
					}

					// GỌI HÀM MỚI TRONG SERVICE
					// Không cần lấy transaction ra rồi set từng dòng nữa
					await _transactionService.UpdateTransactionStatusAsync(
						transactionId,
						"COMPLETED",
						gatewayTransId,
						bankTransInfo
					);

					// Cộng tiền vào ví (Logic ví giữ nguyên)
					// Lưu ý: Bạn nên kiểm tra xem UpdateTransactionStatusAsync có thực sự update không
					// trước khi cộng tiền (để tránh cộng tiền 2 lần)
					var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);
					if (transaction.Status == "COMPLETED" && transaction.Money == amountPaid)
					{
						await _walletService.TopUp(transaction.WalletID, transaction.Money);
					}
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
		public async Task<IActionResult> GetPaymentStatus(int orderCode)
		{
			try
			{
				// 1. Lấy thông tin giao dịch từ Database
				var transaction = await _transactionService.GetTransactionByIdAsync(orderCode);
				if (transaction == null)
				{
					return NotFound("Không tìm thấy giao dịch");
				}

				// 2. Nếu Webhook đã chạy và cập nhật rồi -> Trả về luôn
				if (transaction.Status == "COMPLETED")
				{
					return Ok(new { status = "COMPLETED", message = "Giao dịch đã thành công" });
				}

				// 3. Nếu vẫn là PENDING -> Chủ động hỏi PayOS ngay lập tức
				// (Phòng trường hợp Webhook đến chậm hoặc bị lỗi)
				PaymentLinkInformation paymentLinkInfo = await _payOS.getPaymentLinkInformation(orderCode);

				if (paymentLinkInfo.status == "PAID")
				{
					var transactionInfo = paymentLinkInfo.transactions.FirstOrDefault();
					if (transactionInfo != null)
					{
						string bankAccount = transactionInfo.counterAccountNumber; // Số tài khoản người chuyển
						string bankName = transactionInfo.counterAccountBankName;   // Tên ngân hàng (nếu có)
						string bankTransId = transactionInfo.reference;     // Mã tham chiếu ngân hàng (nếu có)

						// Cập nhật vào DB
						await _transactionService.UpdateTransactionStatusAsync(
							orderCode,
							"COMPLETED",
							paymentLinkInfo.id, // GatewayTransactionId (Mã Payment Link)
							$"{bankName} {bankAccount}" // Lưu kết hợp Tên NH + Số TK làm BankTransId cho dễ tra cứu
						);
					}

					// Cộng tiền (nếu chưa cộng)
					await _walletService.TopUp(transaction.WalletID, transaction.Money);

					return Ok(new { status = "COMPLETED", message = "Giao dịch thành công (đã cập nhật)" });
				}

				// Nếu vẫn chưa thanh toán
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