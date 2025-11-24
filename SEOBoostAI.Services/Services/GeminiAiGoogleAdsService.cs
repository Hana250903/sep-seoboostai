using Microsoft.Extensions.Logging; // Thêm Logger để debug
using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Service.DTOs;
using SEOBoostAI.Service.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace SEOBoostAI.Service.Services
{
    public class GeminiAiGoogleAdsService : IGeminiAiGoogleAdsService
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly string _apikey;
        private readonly string _url;
        // Thêm Logger để bạn soi lỗi
        private readonly ILogger<GeminiAiGoogleAdsService> _logger;

        public GeminiAiGoogleAdsService(ISystemConfigService systemConfigService, ILogger<GeminiAiGoogleAdsService> logger)
        {
            _systemConfigService = systemConfigService;
            _logger = logger;
            _apikey = _systemConfigService.GetValue<string>("giaapi", "");
            _url = _systemConfigService.GetValue<string>("giaurl", "");
        }

        public async Task<List<AdsEvaluationItem>> EvaluateAdsKeywordsAsync(string aiAdvice, List<AdsPlannerItemDto> adsData)
        {
            // 1. Vẫn lấy 50 dòng đầu vào để AI có nhiều dữ liệu phân tích
            var dataToSend = adsData.Take(50).ToList();

            string adsDataJson = JsonSerializer.Serialize(dataToSend);
            string fullUrl = $"{_url}?key={_apikey}";

            using HttpClient client = new HttpClient();

            // === PROMPT MỚI: YÊU CẦU LỌC VÀ GIỚI HẠN ===
            string promptTemplate = $@"Bạn là một chuyên gia Google Ads (SEM).
            
            **INPUT:**
            1. **Chiến lược (Context):** ""{aiAdvice.Substring(0, Math.Min(aiAdvice.Length, 500))}...""
            2. **Dữ liệu thô:** Danh sách {dataToSend.Count} từ khóa bên dưới:
            {adsDataJson}

            **NHIỆM VỤ (LỌC & CHỌN):**
            1.  Đánh giá tất cả các từ khóa dựa trên Volume, Cạnh tranh, Giá thầu và Chiến lược.
            2.  **CHỌN LỌC:** Chỉ giữ lại những từ khóa thực sự tiềm năng (`IsPotential = true`). Loại bỏ những từ khóa kém hiệu quả.
            3.  **GIỚI HẠN:** Danh sách kết quả trả về **TỐI ĐA 25 TỪ KHÓA** tốt nhất.

            **QUY TẮC ĐẦU RA:**
            -   CHỈ trả về những từ khóa được chọn (`IsPotential` luôn là `true`).
            -   Message: Giải thích cực ngắn (dưới 15 từ) tại sao từ khóa này tốt.

            **OUTPUT (JSON ARRAY ONLY):**
            Trả về MỘT mảng JSON duy nhất. Không giải thích thêm.
            [
              {{ ""Keyword"": ""..."", ""IsPotential"": true, ""Message"": ""Volume cao, giá rẻ..."" }},
              ... (Tối đa 25 mục)
            ]";
            // ==============================================

            var requestData = new GeminiAIRequestModel
            {
                Contents = new[] { new ContentRequest { Parts = new[] { new PartRequest { Text = promptTemplate } } } }
            };

            string json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(fullUrl, content);
            string result = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var geminiResponse = JsonSerializer.Deserialize<GeminiAIResponseModel>(result, options);

            return DeserializeResponse(geminiResponse);
        }

        private List<AdsEvaluationItem> DeserializeResponse(GeminiAIResponseModel geminiResponse)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            if (geminiResponse?.Candidates == null || !geminiResponse.Candidates.Any())
            {
                _logger.LogWarning("Gemini trả về rỗng hoặc lỗi Candidates.");
                return new List<AdsEvaluationItem>();
            }

            string dirtyJsonString = geminiResponse.Candidates.First().Content.Parts.First().Text;

            // LOG RA ĐỂ KIỂM TRA XEM AI TRẢ VỀ 1 DÒNG HAY NHIỀU DÒNG
            _logger.LogInformation("RAW JSON FROM AI (ADS): " + dirtyJsonString);

            string cleanJsonString = dirtyJsonString
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();

            try
            {
                var result = JsonSerializer.Deserialize<List<AdsEvaluationItem>>(cleanJsonString, options)
                       ?? new List<AdsEvaluationItem>();

                _logger.LogInformation($"Đã deserialize thành công {result.Count} đánh giá.");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError("Lỗi Parse JSON Ads: " + ex.Message);
                return new List<AdsEvaluationItem>();
            }
        }
    }
}