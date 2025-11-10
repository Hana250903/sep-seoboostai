using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Service.DTOs;
using SEOBoostAI.Service.Services.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging; // <-- THÊM DÒNG NÀY

namespace SEOBoostAI.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestAiController : ControllerBase
    {
        // --- KHAI BÁO CÁC SERVICE ---
        private readonly IGeminiAiKeywordService _keywordService;
        private readonly IGeminiAiAnalysisService _analysisService;
        private readonly ISerpApiService _serpApiService;

        // --- KHAI BÁO LOGGER (ĐÃ THÊM) ---
        private readonly ILogger<TestAiController> _logger;

        // --- CẬP NHẬT CONSTRUCTOR ---
        public TestAiController(
            IGeminiAiKeywordService keywordService,
            IGeminiAiAnalysisService analysisService,
            ISerpApiService serpApiService,
            ILogger<TestAiController> logger) // <-- THÊM THAM SỐ LOGGER
        {
            _keywordService = keywordService;
            _analysisService = analysisService;
            _serpApiService = serpApiService;
            _logger = logger; // <-- GÁN LOGGER
        }

        // --- CÁC HÀM TEST CŨ (GIỮ NGUYÊN) ---
        [HttpGet("test-keywords")]
        public async Task<IActionResult> TestKeywords([FromQuery] string question)
        {
            if (string.IsNullOrEmpty(question))
            {
                return BadRequest("Vui lòng cung cấp 'question' trên URL.");
            }

            try
            {
                TrendParameters result = await _keywordService.ExtractKeywordsFromQuestionAsync(question);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // Sử dụng _logger (đã có)
                _logger.LogError(ex, "Lỗi khi gọi KeywordService");
                return StatusCode(500, $"Lỗi khi gọi KeywordService: {ex.Message}");
            }
        }

        [HttpGet("test-analysis")]
        public async Task<IActionResult> TestAnalysis([FromQuery] string question)
        {
            // (Code cũ giữ nguyên...)
            if (string.IsNullOrEmpty(question))
            {
                return BadRequest("Vui lòng cung cấp 'question' trên URL.");
            }

            var mockTrendData = new { /* ... */ };
            string mockDataJson = JsonSerializer.Serialize(mockTrendData);

            try
            {
                string result = await _analysisService.GetTrendAnalysisSuggestionAsync(question, mockDataJson);
                return Ok(new { AiResponse = result });
            }
            catch (Exception ex)
            {
                // Sử dụng _logger (đã có)
                _logger.LogError(ex, "Lỗi khi gọi AnalysisService");
                return StatusCode(500, $"Lỗi khi gọi AnalysisService: {ex.Message}");
            }
        }

        // --- HÀM TEST SERPAPI (KHÔNG CÒN LỖI) ---
        [HttpGet("test-serpapi-over-time")]
        public async Task<IActionResult> TestSerpApiOverTime([FromQuery] string question)
        {
            if (string.IsNullOrEmpty(question))
            {
                return BadRequest("Vui lòng cung cấp 'question'.");
            }

            try
            {
                // Giờ đây _logger đã tồn tại và sẽ hoạt động
                _logger.LogInformation("Bắt đầu test SerpApi 'Over Time'");

                TrendParameters parameters = await _keywordService.ExtractKeywordsFromQuestionAsync(question);

                if (parameters == null || string.IsNullOrWhiteSpace(parameters.Query))
                {
                    return BadRequest("AI không thể trích xuất từ khóa.");
                }

                _logger.LogInformation("AI đã trích xuất: {query}", parameters.Query);

                var serpApiResult = await _serpApiService.GetInterestOverTimeAsync(parameters);

                _logger.LogInformation("SerpApi trả về thành công.");

                return Ok(serpApiResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi test SerpApiService");
                // Đã sửa lại thông báo lỗi để bao gồm cả InnerException
                return StatusCode(500, $"LỖI: {ex.Message} \n INNER EXCEPTION: {ex.InnerException?.Message}");
            }
        }
    }
}
