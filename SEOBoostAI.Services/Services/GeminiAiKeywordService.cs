using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Service.DTOs;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class GeminiAiKeywordService : IGeminiAiKeywordService
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly string _apikey;
        private readonly string _url;

        public GeminiAiKeywordService(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
            // Dùng chung key và url với service Gemini cũ
            _apikey = _systemConfigService.GetValue<string>("giaapi", "");
            _url = _systemConfigService.GetValue<string>("giaurl", "");
        }

        // --- Thực thi Phân tích Lần 1 ---
        public async Task<TrendParameters> ExtractKeywordsFromQuestionAsync(string originalQuestion)
        {
            string fullUrl = $"{_url}?key={_apikey}";
            using HttpClient client = new HttpClient();

            // === PROMPT ĐÃ ĐƯỢC CẬP NHẬT (THÔNG MINH HƠN) ===
            string promptTemplate = $@"Bạn là một chuyên gia phân tích ý định (Intent) của người dùng.
            Nhiệm vụ của bạn là trích xuất CHỦ ĐỀ SẢN PHẨM/THỊ TRƯỜNG (thứ mà khách hàng tìm kiếm) để phân tích.

            **QUY TẮC QUAN TRỌNG:**
            1.  **Nếu ý định là 'So sánh'** (ví dụ: 'phở hay cháo', 'A với B'), trường Query sẽ là ""topic1,topic2"".
            2.  **Nếu ý định là 'Phân tích'** (ví dụ: 'kinh doanh X', 'làm sao để Y'), trường Query **CHỈ** được chứa **CHỦ ĐỀ SẢN PHẨM** (ví dụ: 'cà phê', 'phở').
                **TUYỆT ĐỐI KHÔNG** bao gồm các từ về *hoạt động kinh doanh* (như 'kinh doanh', 'vốn', 'chi phí', 'làm sao để', 'ở đâu') vào trường Query.

            Bạn PHẢI trả về CHỈ một đối tượng JSON hợp lệ, không dùng markdown.
            Cấu trúc JSON bắt buộc:
            {{
              ""Query"": ""chủ đề sản phẩm"",
              ""Geolocation"": ""VN"",
              ""Language"": ""vi"",
              ""Timeframe"": ""today 12-m""
            }}

            --- VÍ DỤ QUAN TRỌNG ---
            Câu hỏi: ""so sánh phở và cháo ở việt nam 12 tháng qua""
            (Ý định: So sánh. Chủ đề: 'phở', 'cháo')
            Kết quả JSON:
            {{
              ""Query"": ""phở,cháo"",
              ""Geolocation"": ""VN"",
              ""Language"": ""vi"",
              ""Timeframe"": ""today 12-m""
            }}
            
            Câu hỏi: ""Tôi đang muốn kinh doanh cà phê tại Đak Lak thì nên làm thế nào, tôi cần bao nhiêu vốn""
            (Ý định: Phân tích. Chủ đề sản phẩm: 'cà phê'. Câu hỏi phụ: 'cần bao nhiêu vốn')
            Kết quả JSON:
            {{
              ""Query"": ""cà phê"",
              ""Geolocation"": ""VN"",
              ""Language"": ""vi"",
              ""Timeframe"": ""today 12-m""
            }}

            Câu hỏi: ""tôi đang kinh doanh quán Phở ở sài gòn và muốn mọi người biết đến mình hơn""
            (Ý định: Phân tích. Chủ đề sản phẩm: 'Phở'. Mục tiêu: 'tăng nhận diện')
            Kết quả JSON:
            {{
              ""Query"": ""Phở"",
              ""Geolocation"": ""VN"",
              ""Language"": ""vi"",
              ""TimefGFrame"": ""today 12-m""
            }}

            Câu hỏi của người dùng:
            {originalQuestion}";

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

            // Tái sử dụng hàm private để dọn dẹp JSON
            return DeserializeResponse<TrendParameters>(geminiResponse);
        }

        // --- Hàm Private dọn dẹp JSON (Giống service cũ) ---
        private T DeserializeResponse<T>(GeminiAIResponseModel geminiResponse)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            string dirtyJsonString = geminiResponse.Candidates.First().Content.Parts.First().Text;
            string cleanJsonString = dirtyJsonString
                    .Replace("```json", "")
                    .Replace("```", "")
                    .Trim();
            var result = JsonSerializer.Deserialize<T>(cleanJsonString, options);
            return result;
        }
    }
}
