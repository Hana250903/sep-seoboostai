using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Service.Services.Interfaces;
using System.Security.Claims;

namespace SEOBoostAI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/trends")]
    public class TrendSearchController : ControllerBase
    {
        private readonly ITrendSearchService _trendSearchService;
        private readonly ILogger<TrendSearchController> _logger;

        public TrendSearchController(
            ITrendSearchService trendSearchService,
            ILogger<TrendSearchController> logger)
        {
            _trendSearchService = trendSearchService;
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
                var userIdString = User.FindFirstValue("user_ID");
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized(new { message = "Token không hợp lệ hoặc không tìm thấy User ID." });
                }
                
                var result = await _trendSearchService.AnalyzeTrendQueryAsync(userId, request.Question, request.FeatureID);
                
                return Ok(result);
            }
            catch (ArgumentException ex) // Bắt lỗi validation
            {
                _logger.LogWarning(ex, "Yêu cầu không hợp lệ.");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi nghiêm trọng trong quá trình AnalyzeTrendQuery");
                // Trả về lỗi 500 nhưng giấu chi tiết kỹ thuật với người dùng (chỉ log lại)
                return StatusCode(500, new { message = "Đã xảy ra lỗi máy chủ nội bộ. Vui lòng thử lại sau." });
            }
        }


        [HttpGet("show-keywords/{historyId}")]
        public async Task<IActionResult> ShowAdsKeywords(
            int historyId,
            [FromQuery] bool onlySuggestions = false) // Nhận tham số từ URL
        {
            try
            {
                var userIdString = User.FindFirstValue("user_ID");
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized();
                }
                var result = await _trendSearchService.GetAdsKeywordsDetailAsync(
                    historyId,
                    userId,
                    onlySuggestions
                );
                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi...");
                return StatusCode(500, "Lỗi máy chủ");
            }
        }
        // 3. ĐÃ XÓA ENDPOINT 'show-keywords' 
        // (Vì đã chuyển sang QueryHistoryController)
    }
}