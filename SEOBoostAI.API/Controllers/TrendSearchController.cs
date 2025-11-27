using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Service.DTOs;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Ultils;

namespace SEOBoostAI.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/trends")]
    public class TrendSearchController : ControllerBase
    {
        private readonly ITrendSearchService _trendSearchService;
        private readonly ICurrentUserService _currentUserService;
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
                // 2. SỬ DỤNG USER ID THẬT TỪ TOKEN
                var memberId = _currentUserService.GetUserId(); 
                
                if (memberId == 0)
                {
                     return Unauthorized(new { message = "Token không hợp lệ hoặc không tìm thấy User ID." });
                }

                _logger.LogInformation("Bắt đầu 'analyze' cho MemberId: {memberId}", memberId);
                
                var result = await _trendSearchService.AnalyzeTrendQueryAsync(memberId, request.Question);
                
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
        public async Task<IActionResult> ShowAdsKeywords(int historyId)
        {
            try
            {

                var currentUserId = _currentUserService.GetUserId();

                // Truyền thêm userId vào service
                var result = await _trendSearchService.GetAdsKeywordsDetailAsync(historyId, currentUserId);

                return Ok(result);


            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết keywords");
                return StatusCode(500, new { message = "Lỗi máy chủ" });
            }
        }
        // 3. ĐÃ XÓA ENDPOINT 'show-keywords' 
        // (Vì đã chuyển sang QueryHistoryController)
    }
}