using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Service.Helpers;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    public class GeminiAIService : IGeminiAIService
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly GeminiRateLimitHelper _rateLimitHelper;
        private readonly string _url;
        public GeminiAIService(
            ISystemConfigService systemConfigService,
            GeminiRateLimitHelper rateLimitHelper)
        {
            _systemConfigService = systemConfigService;
            _rateLimitHelper = rateLimitHelper;
            _url = _systemConfigService.GetValue<string>("GeminiUrl", "");
        }

        public async Task<AiAssessment> SuggestionAnalysisPerformance(string newMetrics, string oldMetrics)
        {
            string dataInputSection;
            string taskInstruction;

            if (string.IsNullOrEmpty(oldMetrics))
            {
                // TRƯỜNG HỢP 1: CHỈ CÓ DỮ LIỆU MỚI (Phân tích thông thường)
                taskInstruction = @"
                    1. Phân tích các chỉ số này và viết một **đánh giá chung** (GeneralAssessment) về tình trạng hiệu suất hiện tại (ví dụ: Tốt, Cần cải thiện, Chậm).
                    2. Đưa ra các **gợi ý/đề xuất** (Suggestion) để cải thiện các chỉ số yếu kém nhất.";

                dataInputSection = $@"
                    Dữ liệu PageSpeed:
                    {newMetrics}";
            }
            else
            {
                // TRƯỜNG HỢP 2: CÓ DỮ LIỆU CŨ (So sánh sự thay đổi)
                taskInstruction = @"
                    1. **So sánh** dữ liệu 'MỚI' so với 'CŨ'. Trong phần **GeneralAssessment**, bạn PHẢI nhận xét xem hiệu suất đã **TĂNG** hay **GIẢM**, chỉ ra cụ thể chỉ số nào thay đổi đáng kể (ví dụ: 'Điểm hiệu suất tăng từ 50 lên 70, LCP cải thiện 0.5s').
                    2. Trong phần **Suggestion**, đưa ra lời khuyên dựa trên sự thay đổi. Nếu hiệu suất giảm, hãy cảnh báo. Nếu tăng nhưng chưa tối ưu, hãy gợi ý bước tiếp theo.";

                dataInputSection = $@"
                    Dữ liệu CŨ (Lần trước):
                    {oldMetrics}

                    Dữ liệu MỚI (Lần này - Cần đánh giá):
                    {newMetrics}";
            }

            // 2. Ghép vào Prompt Template chính
            string promptTemplate = $@"Bạn là một chuyên gia phân tích và tối ưu hiệu suất website (Core Web Vitals). 
    
                Nhiệm vụ của bạn là:
                {taskInstruction}

                Bạn **PHẢI** trả về kết quả **CHỈ** bằng một đối tượng JSON hợp lệ, không có bất kỳ văn bản giải thích nào khác, không dùng markdown code block (```json ... ```). Nội dung bên trong JSON phải bằng tiếng Việt.

                Sử dụng đúng cấu trúc JSON sau:
                {{
                    ""GeneralAssessment"": ""Nội dung đánh giá/so sánh..."",
                    ""Suggestion"": ""Các gợi ý hành động...""
                }}

                Dữ liệu đầu vào:
                {dataInputSection}";

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
                },
                GenerationConfig = new GenerationConfig
                {
                    //MaxOutputTokens = 8192,
                    Temperature = 0.2,      // Giữ nhiệt độ thấp để JSON chuẩn
                    ResponseMimeType = "application/json" // Bắt buộc Gemini trả về JSON chuẩn (không markdown)
                }
            };

            // GỌI QUA HELPER ĐỂ XỬ LÝ RATE LIMIT & AUTO SWITCH KEY
            // Hàm này sẽ tự động: Lấy key -> Gọi -> Nếu 429 -> Lấy key khác -> Gọi lại
            var assessmentResult = await _rateLimitHelper.ExecuteWithRateLimitAsync<AiAssessment>(_url,
                async (urlWithKey) => // Delegate: nhận url đã kèm key từ helper
                {
                    using HttpClient client = new HttpClient();
                    string json = JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    // Gọi vào URL động (đã chứa key A hoặc key B...)
                    var response = await client.PostAsync(urlWithKey, content);

                    // Quan trọng: Phải throw exception nếu gặp lỗi để Helper bắt được catch
                    if (!response.IsSuccessStatusCode)
                    {
                        // Ném lỗi HttpRequestException kèm StatusCode để Helper check 429
                        throw new HttpRequestException($"API Error: {response.ReasonPhrase}", null, response.StatusCode);
                    }

                    string result = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var geminiResponse = JsonSerializer.Deserialize<GeminiAIResponseModel>(result, options);
                    return DeserializeResponse<AiAssessment>(geminiResponse);
                });

            return assessmentResult;
        }

        public async Task<List<AiElementAnalysis>> SuggestionElement(List<ElementRequest> elements)
        {
            var finalResults = new List<AiElementAnalysis>();

            var batches = elements.Chunk(50).ToList();

            foreach (var batch in batches)
            {
                try
                {
                    string jsonRequest = JsonSerializer.Serialize(batch);

                    string promptTemplate = $@"Bạn là chuyên gia Audit SEO & Core Web Vitals (LCP, CLS, INP).
            
                        Nhiệm vụ: Phân tích danh sách các elements HTML được cung cấp dưới dạng JSON.
            
                        Yêu cầu bắt buộc:
                        1. Ngôn ngữ: TRẢ VỀ 100% TIẾNG VIỆT.
                        2. Output format: Chỉ trả về JSON Array hợp lệ.
                        3. Xử lý logic cho từng loại thẻ:
                           - `img`: Kiểm tra `alt`, `width`, `height` (tránh CLS), `loading='lazy'`.
                           - `a`: Kiểm tra `href` có hợp lệ, có `aria-label` hoặc text mô tả không.
                           - `link`: 
                             + Nếu là CSS/Font (`rel='stylesheet'`, `fonts.googleapis`...): Kiểm tra xem có gây chặn hiển thị (Render blocking) không. Đề xuất `preload` hoặc `preconnect`.
                             + Kiểm tra tính bảo mật (https).
                           - `script`: Kiểm tra `async` hoặc `defer` để tránh chặn main-thread.
                        4. Quy định về nội dung trả về:
                           - Nếu phát hiện lỗi/thiếu sót: Set `HasSuggestion` = true, `Important` = true (nếu lỗi nghiêm trọng như CLS/LCP), viết `Description` và `AIRecommendation`.
                           - Nếu thẻ ĐÃ TỐI ƯU (Không lỗi): Set `HasSuggestion` = false. TRONG TRƯỜNG HỢP NÀY, `Description` phải ghi là ""Đã tối ưu chuẩn SEO/Performance"" (KHÔNG ĐƯỢC ĐỂ RỖNG HOẶC NULL).

                        Dữ liệu Input:
                        {jsonRequest}

                        Cấu trúc Output mẫu (JSON):
                        [
                            {{
                                ""ElementID"": 1,
                                ""HasSuggestion"": true,
                                ""Important"": true,
                                ""Description"": ""Thẻ link tải font Google gây chặn hiển thị."",
                                ""AIRecommendation"": ""Thêm thuộc tính `preconnect` hoặc `display: swap` để tối ưu tải font.""
                            }},
                            {{
                                ""ElementID"": 2,
                                ""HasSuggestion"": false,
                                ""Important"": false,
                                ""Description"": ""Đã tối ưu chuẩn SEO."",
                                ""AIRecommendation"": """"
                            }}
                        ]";

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
                        },
                        GenerationConfig = new GenerationConfig
                        {
                            //MaxOutputTokens = 8192,
                            Temperature = 0.2,      // Giữ nhiệt độ thấp để JSON chuẩn
                            ResponseMimeType = "application/json" // Bắt buộc Gemini trả về JSON chuẩn (không markdown)
                        }
                    };

                    var batchResult = await _rateLimitHelper.ExecuteWithRateLimitAsync<List<AiElementAnalysis>>(_url,
                        async (urlWithKey) =>
                        {
                            using HttpClient client = new HttpClient();
                            string json = JsonSerializer.Serialize(requestData);
                            var content = new StringContent(json, Encoding.UTF8, "application/json");

                            var response = await client.PostAsync(urlWithKey, content);

                            if (!response.IsSuccessStatusCode)
                            {
                                throw new HttpRequestException($"API Error: {response.ReasonPhrase}", null, response.StatusCode);
                            }

                            string result = await response.Content.ReadAsStringAsync();
                            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                            var geminiResponse = JsonSerializer.Deserialize<GeminiAIResponseModel>(result, options);

                            return DeserializeResponse<List<AiElementAnalysis>>(geminiResponse);
                        });

                    if (batchResult != null)
                    {
                        finalResults.AddRange(batchResult);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi xử lý batch: {ex.Message}");
                    // Có thể bỏ qua batch lỗi hoặc retry, nhưng không làm chết cả luồng
                }
            }

            return finalResults;
        }

        private T DeserializeResponse<T>(GeminiAIResponseModel geminiResponse)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            string dirtyJsonString = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrEmpty(dirtyJsonString))
            {
                throw new InvalidOperationException("Không thể trích xuất nội dung text từ phản hồi của Gemini. Cấu trúc response có thể đã thay đổi hoặc bị chặn.");
            }

            string cleanJsonString = dirtyJsonString
                    .Replace("```json", "")  // Xóa ```json ở đầu
                    .Replace("```", "")      // Xóa ``` ở cuối
                    .Trim();                // Xóa các khoảng trắng/xuống dòng thừa

            var result = JsonSerializer.Deserialize<T>(cleanJsonString, options);

            return result;
        }
    }
}