using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayOS;
using Net.payOS.Types;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Service.Services.Interfaces;
using System.Security.Claims;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/payment")]
	[ApiController]
	public class PaymentController : ControllerBase
	{
		private readonly Net.payOS.PayOS _payOS;
		private readonly ITransactionService _transactionService;
		private readonly IWalletService _walletService;

		public PaymentController(Net.payOS.PayOS payOS, ITransactionService transactionService, IWalletService walletService)
		{
			_payOS = payOS;
			_transactionService = transactionService;
			_walletService = walletService;
		}

		//[Authorize] // Bạn đang tạm thời tắt, OK
		[HttpPost("create-payment-link")]
		public async Task<IActionResult> CreatePaymentLink([FromBody] PaymentLinkRequest request)
		{
			try
			{
				// 1. Lấy UserID từ JWT token
				//var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				//if (string.IsNullOrEmpty(userIdString))
				//{
				//	return Unauthorized("User ID not found.");
				//}
				//var userId = int.Parse(userIdString);
				var userId = 1;

				// 2. Lấy WalletID từ UserID
				var wallet = await _walletService.GetWalletByUserIdAsync(userId);

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

				return Ok(new { checkoutUrl = result.checkoutUrl });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[AllowAnonymous]
		[HttpPost("webhook")]
		public async Task<IActionResult> HandleWebhook([FromBody] WebhookType webhookData)
		{
			try
			{
				// SỬA LỖI 5: Tên phương thức xác thực là 'verifyPaymentWebhookData'
				WebhookData verifiedData = _payOS.verifyPaymentWebhookData(webhookData);

				// SỬA LỖI 6: Kiểm tra trạng thái từ 'verifiedData.data.status'
				if (verifiedData.currency == "PAID")
				{
					int transactionId = (int)verifiedData.orderCode;

					var transaction = await _transactionService.GetTransactionByIdAsync(transactionId);

					if (transaction != null && transaction.Status == "PENDING")
					{
						// Cập nhật giao dịch
						transaction.Status = "COMPLETED";
						transaction.CompletedTime = DateTime.UtcNow;
						transaction.GatewayTransactionId = verifiedData.reference;
						transaction.BankTransId = verifiedData.accountNumber;
						await _transactionService.UpdateAsync(transaction);

						// Cộng tiền vào ví
						await _walletService.TopUp(transaction.WalletID, transaction.Money);
					}
				}

				return Ok(); // Luôn trả về 200 OK cho PayOS
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Webhook failed: {ex.Message}");
				return BadRequest();
			}
		}
	}
}