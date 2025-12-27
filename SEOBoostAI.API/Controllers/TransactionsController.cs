using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.Enums;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;
using System.Security.Claims;

namespace SEOBoostAI.API.Controllers
{
	[Route("api/transactions")]
	[ApiController]
	[Authorize]
	public class TransactionsController : ControllerBase
	{
		private readonly ITransactionService _transactionService;
		private readonly IPdfService _pdfService;

		public TransactionsController(ITransactionService transactionService, IPdfService pdfService)
		{
			_transactionService = transactionService;
			_pdfService = pdfService;
		}

		// GET: api/<TransactionsController>
		[HttpGet]
		public async Task<IEnumerable<Transaction>> Get()
		{
			return await _transactionService.GetTransactionsAsync();
		}

		[HttpGet("{currentPage}/{pageSize}")]
		public async Task<PaginationResult<List<Transaction>>> Get(int currentPage, int pageSize)
		{
			return await _transactionService.GetTransactionsWithPaginateAsync(currentPage, pageSize);
		}

		[HttpGet("history/{currentPage}/{pageSize}")]
		public async Task<IActionResult> GetTransactionByUser(int currentPage, int pageSize)
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
				var result = await _transactionService.GetUserPaymentHistoryAsync(userId, currentPage, pageSize);

				return Ok(new { data = result });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		// GET api/<TransactionsController>/5
		[HttpGet("{id}")]
		public async Task<Transaction> Get(int id)
		{
			return await _transactionService.GetTransactionByIdAsync(id);
		}

		// POST api/<TransactionsController>
		[HttpPost]
		public async Task<IActionResult> Post([FromBody] Transaction transaction)
		{
			await _transactionService.CreateAsync(transaction);
			return Ok(transaction);
        }

		// PUT api/<TransactionsController>/
		[HttpPut]
		public async Task<IActionResult> Put([FromBody] Transaction transaction)
		{
			await _transactionService.UpdateAsync(transaction);
			return Ok(transaction);
        }

		// DELETE api/<TransactionsController>/5
		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			await _transactionService.DeleteAsync(id);
			return Ok();
        }

		[HttpPost("admin-deposit")]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> AdminDeposit([FromBody] AdminDepositRequest request)
		{
			try
			{
				// Validate cơ bản
				if (request.Money <= 10000)
				{
					return BadRequest(new { message = "Số tiền nạp phải lớn hơn 10000." });
				}

				// Gọi Service xử lý
				var transaction = await _transactionService.CreateAdminDepositAsync(
					request.UserId,
					request.Money,
					request.Description
				);

				return Ok(new
				{
					message = "Nạp tiền thành công!",
					newBalance = transaction.BalanceAfter, // Trả về số dư mới
					transactionId = transaction.TransactionID
				});
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpGet("{id}/receipt")]
		public async Task<IActionResult> GetReceipt(int id)
		{
			try
			{
				// Lấy UserID và Role từ Token để check quyền
				var userIdString = User.FindFirst("user_ID")?.Value;
				var userId = int.Parse(userIdString);
				var role = User.FindFirst("role")?.Value;

				var receipt = await _transactionService.GetReceiptAsync(id, userId, role);
				return Ok(receipt);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		[HttpGet("{id}/receipt/download")]
		public async Task<IActionResult> DownloadReceipt(int id)
		{
			try
			{
				// 1. Lấy User ID từ Token
				var userIdClaim = User.FindFirst("user_ID");
				if (userIdClaim == null) return Unauthorized("Token không hợp lệ");
				var userId = int.Parse(userIdClaim.Value);

				var roleClaim = User.FindFirst(ClaimTypes.Role) ?? User.FindFirst("role");
				var userRole = roleClaim?.Value ?? UserRole.Member.ToString();

				// 2. Lấy dữ liệu hóa đơn (DTO)
				var receiptData = await _transactionService.GetReceiptAsync(id, userId, userRole);

				// Kiểm tra nếu không tìm thấy hóa đơn
				if (receiptData == null)
				{
					return NotFound(new { message = "Không tìm thấy hóa đơn hoặc bạn không có quyền truy cập." });
				}

				// 3. Gọi Service để tạo PDF
				// LƯU Ý: Không cần truyền đường dẫn template nữa vì QuestPDF tự vẽ
				byte[] pdfFile = _pdfService.GenerateReceiptPdf(receiptData);

				// 4. Trả về file
				return File(pdfFile, "application/pdf", $"HoaDon_{receiptData.TransactionCode}.pdf");
			}
			catch (Exception ex)
			{
				// Log lỗi để debug (nếu cần)
				// _logger.LogError(ex, "Lỗi khi tải hóa đơn");
				return BadRequest(new { message = "Không thể tạo hóa đơn: " + ex.Message });
			}
		}
	}
}
