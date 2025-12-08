using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;


namespace SEOBoostAI.API.Controllers
{
	[Route("api/content-optimizations")]
	[ApiController]
	[Authorize]
	public class ContentOptimizationsController : ControllerBase
	{
		private readonly IContentOptimizationService _contentOptimizationService;
		public ContentOptimizationsController(IContentOptimizationService contentOptimizationService)
		{
			_contentOptimizationService = contentOptimizationService;
		}

		[HttpGet("Search")]
		public async Task<IActionResult> Get([FromQuery] SearchTransactionRequest searchRequest)
		{
			try
			{
				var userIdString = User.FindFirst("user_ID")?.Value;
				if (string.IsNullOrEmpty(userIdString))
				{
					return BadRequest(new { message = "Không tìm thấy UserID trong token." });
				}

				int userId = int.Parse(userIdString);

				var result = await _contentOptimizationService.GetContentOptimizationsWithPaginateAsync(searchRequest, userId);

				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		// GET api/<ContentOptimizationsController>/5
		[HttpGet]
		public async Task<IActionResult> GetUserById()
		{
			var userIdString = User.FindFirst("user_ID")?.Value;
			if (string.IsNullOrEmpty(userIdString))
			{
				BadRequest(new { message = "Không tìm thấy UserID trong token." });
			}
			int userId = int.Parse(userIdString);

			var result = await _contentOptimizationService.GetContentOptimizationsByUserIdAsync(userId);
			return Ok(result);
		}

		[HttpPost]
		public async Task<IActionResult> Post([FromBody] OptimizeRequestDto requestDto)
		{
			if (requestDto == null)
			{
				return BadRequest(new { message = "Dữ liệu yêu cầu không được để trống." });
			}

			if (!ModelState.IsValid)
			{
				return BadRequest(new { message = "Dữ liệu không hợp lệ.", errors = ModelState });
			}

			try
			{
				// 1. Lấy UserID từ Token (Bảo mật hơn là tin vào requestDto)
				var userIdString = User.FindFirst("user_ID")?.Value;
				if (string.IsNullOrEmpty(userIdString))
				{
					return Unauthorized(new { message = "Không tìm thấy UserID trong token." });
				}

				int userId = int.Parse(userIdString);

				// 2. Gọi Service
				ContentOptimizationDto result = await _contentOptimizationService.OptimizeAndCreateAsync(requestDto, userId);

				// 3. Trả về kết quả 201 Created
				return CreatedAtAction(nameof(Get), new { id = result.ContentOptimizationID }, result);
			}
			// --- BẮT LỖI NGHIỆP VỤ ---
			catch (ArgumentException ex)
			{
				// Lỗi do nội dung chứa từ cấm hoặc tham số không hợp lệ
				return BadRequest(new { message = ex.Message, errorCode = "SENSITIVE_CONTENT" });
			}
			catch (InvalidOperationException ex)
			{
				return StatusCode(403, new { message = ex.Message, errorCode = "QUOTA_EXCEEDED" });
			}
			// --- BẮT LỖI KỸ THUẬT---
			catch (Exception ex)
			{
				// Lỗi kết nối Database, lỗi Gemini API sập, v.v.
				return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				await _contentOptimizationService.DeleteAsync(id);
				return Ok(new { message = "Xóa bản ghi tối ưu nội dung thành công." });
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}
	}
}
