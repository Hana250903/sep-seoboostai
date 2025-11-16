using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
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

            var assessmentResult = DeserializeResponse<AiAssessment>(geminiResponse)
;
            return assessmentResult;
        }

        public async Task<List<AiElementAnalysis>> SuggestionElement(List<ElementRequest> elements)
        {
            string fullUrl = $"{_url}?key={_apikey}";

            using HttpClient client = new HttpClient();

            string jsonRequest = JsonSerializer.Serialize(elements);

            string promptTemplate = $@"Bạn là một chuyên gia phân tích HTML, tối ưu hiệu suất website (Core Web Vitals) và SEO. Tôi sẽ cung cấp cho bạn một **danh sách (JSON array)** các phần tử HTML. Mỗi phần tử sẽ có `ElementID`, `TagName`, và `OuterHTML`.

                Nhiệm vụ của bạn là:
                1.  Phân tích **từng element** trong danh sách để tìm ra các vấn đề tiềm ẩn về hiệu suất (ví dụ: gây CLS, chặn hiển thị) hoặc SEO (ví dụ: thiếu alt text).
                2.  Trả về **DUY NHẤT** một **JSON array** hợp lệ, không dùng markdown, không giải thích.
                3.  Array này phải chứa một đối tượng cho **mỗi element** đã được phân tích.

                **Quan trọng:** Mỗi đối tượng trong array trả về **PHẢI** chứa:
                * `ElementID`: (Giữ nguyên `ElementID` từ đầu vào để map dữ liệu).
                * `Description`: Mô tả ngắn gọn vấn đề (ví dụ: ""Thẻ <img> thiếu 'alt'"").
                * `AIRecommendation`: Gợi ý cụ thể để sửa lỗi (ví dụ: ""Hãy bổ sung thuộc tính alt='mô tả ảnh'"").

                Sử dụng cấu trúc JSON array bắt buộc sau (ví dụ cho 2 element):
                [
                  {{
                    ""ElementID"": 1,
                    ""AIRecommendation"": ""..."",
                    ""Description"": ""...""
                  }},
                  {{
                    ""ElementID"": 2,
                    ""Attribute"": ""..."",
                    ""Description"": ""...""
                  }}
                ]

                Dữ liệu Elements đầu vào (dạng JSON array):
                {jsonRequest}";

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

            var suggestResult = DeserializeResponse<List<AiElementAnalysis>>(geminiResponse);

            return suggestResult;
        }

		public async Task<AiOptimizationResponse> OptimizeContentAsync(OptimizeRequestDto request)
		{
			string fullUrl = $"{_url}?key={_apikey}";

			using HttpClient client = new HttpClient();

			string citationText = request.IncludeCitation ? "Có, hãy thêm các trích dẫn chất lượng cao để hỗ trợ luận điểm." : "Không, đừng thêm trích dẫn bên ngoài.";

			string promptTemplate = $$"""
                Bạn là một chuyên gia phân tích và tối ưu hóa nội dung SEO.
                Nhiệm vụ của bạn:
                1.  **PHÂN TÍCH GỐC:** Chấm điểm "Nội dung Gốc" (0-100).
                2.  **TỐI ƯU HÓA:** Viết lại nội dung đó.
                3.  **PHÂN TÍCH MỚI:** Chấm điểm "Nội dung đã Tối ưu" (0-100).

                YÊU CẦU QUAN TRỌNG:
                - Bạn BẮT BUỘC phải chấm điểm (KHÔNG được trả về 0) và cung cấp lý do.
                - Bạn BẮT BUỘC phải trả về một đối tượng JSON DUY NHẤT.

                ---
                **CHI TIẾT ĐẦU VÀO:**

                **1. Từ khóa Mục tiêu:**
                '{{request.Keyword}}'

                **2. Nội dung Gốc (Cần Phân tích):**
                {{request.Content}}


                **3. Yêu cầu Tối ưu (Dùng để Viết lại):**
                - **Độ dài:** {{request.ContentLength}}
                - **Mức độ Tối ưu:** {{request.OptimizationLevel}}
                - **Mức độ Dễ đọc:** {{request.ReadabilityLevel}}
                - **Bao gồm Trích dẫn:** {{citationText}}

                ---
                **ĐỊNH DẠNG JSON BẮT BUỘC:**
                ```json
                {
                  "comparison": {
                    "original": {
                      "seo_score": 0,
                      "seo_justification": "Lý do ngắn gọn cho điểm SEO gốc...",
                      "readability_score": 0,
                      "readability_justification": "Lý do ngắn gọn cho điểm dễ đọc gốc...",
                      "engagement_score": 0,
                      "engagement_justification": "Lý do ngắn gọn cho điểm tương tác gốc..."
                    },
                    "optimized": {
                      "seo_score": 0,
                      "seo_justification": "Lý do ngắn gọn cho điểm SEO mới...",
                      "readability_score": 0,
                      "readability_justification": "Lý do ngắn gọn cho điểm dễ đọc mới...",
                      "engagement_score": 0,
                      "engagement_justification": "Lý do ngắn gọn cho điểm tương tác mới..."
                    }
                  },
                  "optimized_content": "..."
                }
                """;

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
								Text = promptTemplate
							}
						}
					}
				}
			};

			string json = JsonSerializer.Serialize(requestData);
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var response = await client.PostAsync(fullUrl, content);
			string result = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				// Thêm kiểm tra lỗi
				throw new HttpRequestException($"Lỗi từ Gemini API: {response.StatusCode}. Chi tiết: {result}");
			}

			// 6. Phân tích Response (giống hệt cách của bạn)
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var geminiResponse = JsonSerializer.Deserialize<GeminiAIResponseModel>(result, options);

			// 7. Gọi hàm DeserializeResponse (giống hệt cách của bạn)
			// Lưu ý: Tên DTO ở đây là AiOptimizationResponse
			var optimizationResult = DeserializeResponse<AiOptimizationResponse>(geminiResponse);

			return optimizationResult;
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