using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class GeminiAiAnalysisService : IGeminiAiAnalysisService
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly string _apikey;
        private readonly string _url;

        public GeminiAiAnalysisService(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
            _apikey = _systemConfigService.GetValue<string>("gia", "");
            _url = _systemConfigService.GetValue<string>("giaurl", "");
        }

        // --- Thực thi Phân tích Lần 2 ---
        public async Task<string> GetTrendAnalysisSuggestionAsync(string originalQuestion, string trendDataJson)
        {
            string fullUrl = $"{_url}?key={_apikey}";
            using HttpClient client = new HttpClient();

            string promptTemplate = $@"Bạn là một chuyên gia phân tích chiến lược SEO và thị trường.
            Một người dùng đã hỏi câu sau: ""{originalQuestion}""

            Chúng tôi đã thu thập dữ liệu Google Trends (dưới dạng JSON) để trả lời họ.
            Dữ liệu Google Trends:
            {trendDataJson}

            Nhiệm vụ của bạn:
            1. Phân tích dữ liệu trends (InterestOverTime, RelatedTopics, RegionComparison, v.v.).
            2. Trả lời trực tiếp câu hỏi của người dùng.
            3. Đưa ra các gợi ý và chiến lược cụ thể dựa trên dữ liệu bạn thấy.
            4. Trả lời bằng tiếng Việt.
            
            Chỉ trả về nội dung câu trả lời, không thêm bất kỳ lời chào hay văn bản thừa nào.";

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

            // Lấy văn bản thô, vì đây là câu trả lời cuối cùng
            string finalAnswer = geminiResponse.Candidates.First().Content.Parts.First().Text;
            return finalAnswer.Trim();
        }
    }
}
