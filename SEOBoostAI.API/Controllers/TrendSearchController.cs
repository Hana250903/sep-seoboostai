using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Service.DTOs;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Ultils;

namespace SEOBoostAI.API.Controllers
{
    //[Authorize]
    [ApiController]
    [Route("api/trends")] // Đặt tên route cho tính năng
    public class TrendSearchController : ControllerBase
    {
        private readonly ITrendSearchService _trendSearchService;
        private readonly ICurrentUserService _currentUserService; // Dùng để lấy ID user
        private readonly ILogger<TrendSearchController> _logger;

        public TrendSearchController(
            ITrendSearchService trendSearchService,
            ICurrentUserService currentUserService,
            ILogger<TrendSearchController> logger)
        {
            _trendSearchService = trendSearchService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        /// <summary>
        /// Phân tích một câu hỏi của người dùng để đưa ra tư vấn về Google Trends.
        /// </summary>
        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeTrendQuery([FromBody] TrendQueryRequest request)
        {
            try
            {
                // 1. TẠM THỜI HARDCODE ID ĐỂ TEST
                var memberId = 1; // <-- Giả mạo ID người dùng là 1

                // (Tạm thời vô hiệu hóa 2 dòng check)
                // var memberId = _currentUserService.GetUserId(); 
                // if (memberId == 0)
                // {
                //     return Unauthorized("Token không hợp lệ hoặc không tìm thấy User ID.");
                // }

                _logger.LogInformation("Bắt đầu 'analyze' (TEST MODE) cho MemberId: {memberId}", memberId);
                var result = await _trendSearchService.AnalyzeTrendQueryAsync(memberId, request.Question);
                return Ok(result);
            }
            catch (ArgumentException ex) // Bắt lỗi "Câu hỏi không được rỗng"
            {
                _logger.LogWarning(ex, "Yêu cầu không hợp lệ từ MemberId: {memberId}", _currentUserService.GetUserId());
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng trong quá trình AnalyzeTrendQuery");
                return StatusCode(500, new { message = $"Lỗi máy chủ: {ex.Message}" });
            }
        }
    }
}
