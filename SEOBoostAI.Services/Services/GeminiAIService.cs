using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class GeminiAIService : IGeminiAIService
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly string _apikey;
        private readonly string _url;

        public GeminiAIService(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
            _apikey = _systemConfigService.GetValue<string>("GeminiKey", "");
            _url = _systemConfigService.GetValue<string>("GeminiUrl", "");
        }

        public async Task<AiAssessment> SuggestionAnalysisPerformance(string metrics)
        {
            string fullUrl = $"{_url}?key={_apikey}";

            using HttpClient client = new HttpClient();

            string promptTemplate = $@"Bạn là một chuyên gia phân tích và tối ưu hiệu suất website (Core Web Vitals). Tôi sẽ cung cấp cho bạn dữ liệu từ Google PageSpeed ở định dạng JSON.

                Nhiệm vụ của bạn là:
                1.  Phân tích các chỉ số này và viết một **đánh giá chung** (GeneralAssessment) về tình trạng hiệu suất của trang web (ví dụ: Tốt, Cần cải thiện, Chậm).
                2.  Đưa ra một vài **gợi ý/đề xuất** (Suggestion) quan trọng nhất, có tính hành động để cải thiện các chỉ số yếu kém.

                Bạn **PHẢI** trả về kết quả **CHỈ** bằng một đối tượng JSON hợp lệ, không có bất kỳ văn bản giải thích nào khác (không dùng markdown). Nội dung bên trong JSON phải bằng tiếng Việt.

                Sử dụng cấu trúc sau:
                {{
                  ""GeneralAssessment"": ""Nội dung đánh giá của bạn..."",
                  ""Suggestion"": ""Các gợi ý của bạn...""
                }}

                Dữ liệu PageSpeed đầu vào:
                {metrics}";

            var requestData = new GeminiAIRequestModel
            {
                Contents = new[]
                {
                    new ContentRequest
                    {
                        Parts = new[]
                        {
                            new PartRequest
                            {
                                Text = promptTemplate,
                            }
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(fullUrl, content);
            string result = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var geminiResponse = JsonSerializer.Deserialize<GeminiAIResponseModel>(result, options);

            var assessmentResult = DeserializeResponse<AiAssessment>(geminiResponse);

            return assessmentResult;
        }

        private T DeserializeResponse<T>(GeminiAIResponseModel geminiResponse)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            string dirtyJsonString = geminiResponse.Candidates.First().Content.Parts.First().Text;

            string cleanJsonString = dirtyJsonString
                    .Replace("```json", "")  // Xóa ```json ở đầu
                    .Replace("```", "")      // Xóa ``` ở cuối
                    .Trim();                // Xóa các khoảng trắng/xuống dòng thừa

            var result = JsonSerializer.Deserialize<T>(cleanJsonString, options);

            return result;
        }  
    }
}
