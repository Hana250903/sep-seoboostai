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

		// GET: api/<ContentOptimizationsController>
		[HttpGet]
		public async Task<IEnumerable<ContentOptimizationDto>> Get()
		{
			return await _contentOptimizationService.GetContentOptimizationsAsync();
		}

		[HttpGet("Search")]
		public async Task<IActionResult> Get([FromQuery] SearchTransactionRequest searchRequest)
		{
			try
			{
				if (searchRequest == null)
				{
					searchRequest = new SearchTransactionRequest();
				}
				var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (!string.IsNullOrEmpty(userIdString))
				{
					searchRequest.UserId = int.Parse(userIdString);
				}

				var result = await _contentOptimizationService.GetContentOptimizationsWithPaginateAsync(searchRequest);

				return Ok(result);
			}
			catch (Exception ex)
			{
				return BadRequest(new { message = ex.Message });
			}
		}

		// GET api/<ContentOptimizationsController>/5
		[HttpGet("{id}")]
		public async Task<ContentOptimizationDto> Get(int id)
		{
			return await _contentOptimizationService.GetContentOptimizationByIdAsync(id);
		}

		[HttpPost]
		public async Task<IActionResult> Post([FromBody] OptimizeRequestDto requestDto)
		{
			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			try
			{
				// 1. Lấy UserID từ Token (Bảo mật hơn là tin vào requestDto)
				// Nếu bạn muốn chắc chắn UserID là của người đang đăng nhập
				var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
				if (!string.IsNullOrEmpty(userIdString))
				{
					requestDto.UserId = int.Parse(userIdString);
				}

				// 2. Gọi Service
				ContentOptimizationDto result = await _contentOptimizationService.OptimizeAndCreateAsync(requestDto);

				// 3. Trả về kết quả 201 Created
				// Lưu ý: Đảm bảo bạn có hàm "Get" hoặc "GetContentOptimizationById" để nameof() hoạt động đúng
				return CreatedAtAction(nameof(Get), new { id = result.ContentOptimizationID }, result);
			}
			// --- BẮT LỖI NGHIỆP VỤ (QUAN TRỌNG) ---
			catch (InvalidOperationException ex)
			{
				// Đây là lỗi do Hết Quota hoặc Từ khóa cấm (do Service ném ra)
				// Trả về 400 Bad Request hoặc 403 Forbidden hoặc 402 Payment Required
				return StatusCode(403, new { message = ex.Message, errorCode = "QUOTA_EXCEEDED" });
			}
			// --- BẮT LỖI KỸ THUẬT (SERVER CRASH) ---
			catch (Exception ex)
			{
				// Lỗi kết nối Database, lỗi Gemini API sập, v.v.
				return StatusCode(500, new { message = $"Lỗi hệ thống: {ex.Message}" });
			}
		}
	}
}
