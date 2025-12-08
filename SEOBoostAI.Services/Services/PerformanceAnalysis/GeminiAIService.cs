using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Service.Helpers;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    public class GeminiAIService : IGeminiAIService
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly GeminiRateLimitHelper _rateLimitHelper;
        private readonly string _url;
        private readonly string _promptSuggestionAnalysisTaskNewMetrics;
        private readonly string _promptSuggestionAnalysisTaskOldMetrics;
        private readonly string _promptSuggestionElement;
        private readonly string _promptAnalysisMetadata;
        private readonly double _temperatureSuggestionAnalysis;
        private readonly double _temperatureSuggestionElement;
        private readonly double _temperatureAnalysisMetadata;


        public GeminiAIService(
            ISystemConfigService systemConfigService,
            GeminiRateLimitHelper rateLimitHelper)
        {
            _systemConfigService = systemConfigService;
            _rateLimitHelper = rateLimitHelper;
            _url = _systemConfigService.GetValue<string>("GeminiUrl", "");
            _promptSuggestionAnalysisTaskNewMetrics = _systemConfigService.GetValue<string>("GeminiPromptSuggestionAnalysisTaskNewMetrics", "");
            _promptSuggestionAnalysisTaskOldMetrics = _systemConfigService.GetValue<string>("GeminiPromptSuggestionAnalysisTaskOldMetrics", "");

            _promptSuggestionElement = _systemConfigService.GetValue<string>("GeminiPromptSuggestionElement", "");

            _promptAnalysisMetadata = _systemConfigService.GetValue<string>("GeminiPromptAnalysisMetadata", "");

            _temperatureSuggestionAnalysis = _systemConfigService.GetValue<double>("GeminiTemperatureSuggestion", 0.2);
            _temperatureSuggestionElement = _systemConfigService.GetValue<double>("GeminiTemperatureElement", 0.2);
            _temperatureAnalysisMetadata = _systemConfigService.GetValue<double>("GeminiTemperatureMetadata", 0.2);
        }

        public async Task<AiAssessment> SuggestionAnalysisPerformance(string newMetrics, string oldMetrics)
        {
            string dataInputSection;
            string taskInstruction;

            var temperature = _temperatureSuggestionAnalysis;

            if (string.IsNullOrEmpty(oldMetrics))
            {
                // TRƯỜNG HỢP 1: CHỈ CÓ DỮ LIỆU MỚI (Phân tích thông thường)
                /*taskInstruction = @"Bạn là một chuyên gia phân tích và tối ưu hiệu suất website (Core Web Vitals). 
    
                Nhiệm vụ của bạn là:
                    1. Phân tích các chỉ số này và viết một **đánh giá chung** (GeneralAssessment) về tình trạng hiệu suất hiện tại (ví dụ: Tốt, Cần cải thiện, Chậm).
                    2. Đưa ra các **gợi ý/đề xuất** (Suggestion) để cải thiện các chỉ số yếu kém nhất.

                Bạn **PHẢI** trả về kết quả **CHỈ** bằng một đối tượng JSON hợp lệ, không có bất kỳ văn bản giải thích nào khác, không dùng markdown code block (```json ... ```). Nội dung bên trong JSON phải bằng tiếng Việt.";*/
                taskInstruction = _promptSuggestionAnalysisTaskNewMetrics;

                dataInputSection = $@"
                    Dữ liệu PageSpeed:
                    {newMetrics}";
            }
            else
            {
                // TRƯỜNG HỢP 2: CÓ DỮ LIỆU CŨ (So sánh sự thay đổi)
                /*taskInstruction = @"
                    Bạn là một chuyên gia phân tích và tối ưu hiệu suất website (Core Web Vitals). 
    
                    Nhiệm vụ của bạn là:
                        1. **So sánh** dữ liệu 'MỚI' so với 'CŨ'. Trong phần **GeneralAssessment**, bạn PHẢI nhận xét xem hiệu suất đã **TĂNG** hay **GIẢM**, chỉ ra cụ thể chỉ số nào thay đổi đáng kể (ví dụ: 'Điểm hiệu suất tăng từ 50 lên 70, LCP cải thiện 0.5s').
                        2. Trong phần **Suggestion**, đưa ra lời khuyên dựa trên sự thay đổi. Nếu hiệu suất giảm, hãy cảnh báo. Nếu tăng nhưng chưa tối ưu, hãy gợi ý bước tiếp theo.

                    Bạn **PHẢI** trả về kết quả **CHỈ** bằng một đối tượng JSON hợp lệ, không có bất kỳ văn bản giải thích nào khác, không dùng markdown code block (```json ... ```). Nội dung bên trong JSON phải bằng tiếng Việt.";*/

                taskInstruction = _promptSuggestionAnalysisTaskOldMetrics;

                dataInputSection = $@"
                    Dữ liệu CŨ (Lần trước):
                    {oldMetrics}

                    Dữ liệu MỚI (Lần này - Cần đánh giá):
                    {newMetrics}";
            }

            // 2. Ghép vào Prompt Template chính
            string promptTemplate = $@"
                {taskInstruction}

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
                    Temperature = temperature,      // Giữ nhiệt độ thấp để JSON chuẩn
                    ResponseMimeType = "application/json" // Bắt buộc Gemini trả về JSON chuẩn (không markdown)
                }
            };

            int estimatedTokens = _rateLimitHelper.EstimateTokens(promptTemplate);
            int actualTokens = estimatedTokens;

            // GỌI QUA HELPER ĐỂ XỬ LÝ RATE LIMIT & AUTO SWITCH KEY
            // Hàm này sẽ tự động: Lấy key -> Gọi -> Nếu 429 -> Lấy key khác -> Gọi lại
            var (assessmentResult, keyId, initialEstimate) = await _rateLimitHelper.ExecuteWithRateLimitAsync<AiAssessment>(_url,
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
                    
                    // LẤY ACTUAL TOKENS TỪ RESPONSE
                    actualTokens = geminiResponse?.UsageMetadata?.PromptTokenCount ?? estimatedTokens;
                    
                    return DeserializeResponse<AiAssessment>(geminiResponse);
                },
                estimatedTokens: estimatedTokens
                );

            // UPDATE ACTUAL TOKENS
            if (actualTokens > 0)
            {
                await _rateLimitHelper.RateLimitManager.UpdateActualTokensAsync(keyId, actualTokens, estimatedTokens);
            }

            return assessmentResult;
        }

        public async Task<List<AiElementAnalysis>> SuggestionElement(List<ElementRequest> elements)
        {
            // Dùng ConcurrentBag để thread-safe khi add kết quả từ nhiều luồng
            var finalResults = new ConcurrentBag<AiElementAnalysis>();


            /*Bạn là chuyên gia Audit SEO &Core Web Vitals(LCP, CLS, INP).
                        Nhiệm vụ: Phân tích danh sách các elements HTML được cung cấp dưới dạng JSON.
                        Yêu cầu bắt buộc:
                        1.Ngôn ngữ: TRẢ VỀ 100 % TIẾNG VIỆT.
                        2.Output format: Chỉ trả về JSON Array hợp lệ.
                        3.Xử lý logic cho từng loại thẻ:
                           - `img`: Kiểm tra `alt`, `width`, `height` (tránh CLS), `loading = 'lazy'`.
                           - `a`: Kiểm tra `href` có hợp lệ, có `aria - label` hoặc text mô tả không.
                           - `link`: 
                             +Nếu là CSS/ Font(`rel = 'stylesheet'`, `fonts.googleapis`...): Kiểm tra xem có gây chặn hiển thị(Render blocking) không.Đề xuất `preload` hoặc `preconnect`.
                             +Kiểm tra tính bảo mật(https).
                           - `script`: Kiểm tra `async` hoặc `defer` để tránh chặn main-thread.
                        4.Quy định về nội dung trả về:
                           -Nếu phát hiện lỗi / thiếu sót: Set `HasSuggestion` = true, `Important` = true(nếu lỗi nghiêm trọng như CLS / LCP), viết `Description` và `AIRecommendation`.
                           -Nếu thẻ ĐÃ TỐI ƯU(Không lỗi): Set `HasSuggestion` = false.TRONG TRƯỜNG HỢP NÀY, `Description` phải ghi là ""Đã tối ưu chuẩn SEO / Performance""(KHÔNG ĐƯỢC ĐỂ RỖNG HOẶC NULL).*/
            var promptTemplateBase = _promptSuggestionElement;
            var temperature = _temperatureSuggestionElement;

            var batches = elements.Chunk(50).ToList();

            // Cấu hình song song
            int maxConcurrency = 5; // Chạy tối đa 5 batch cùng lúc (tương đương 250 elements)
            using var semaphore = new SemaphoreSlim(maxConcurrency);
            var tasks = new List<Task>();

            foreach (var batch in batches)
            {
                // Tạo Task cho mỗi batch
                tasks.Add(Task.Run(async () =>
                {
                    // Chờ đến lượt (nếu đang có 5 luồng chạy thì luồng thứ 6 phải đợi)
                    await semaphore.WaitAsync();
                    try
                    {
                        string jsonRequest = JsonSerializer.Serialize(batch);

                        string promptTemplate = $@"
                        {promptTemplateBase}

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
                                Temperature = temperature,      // Giữ nhiệt độ thấp để JSON chuẩn
                                ResponseMimeType = "application/json" // Bắt buộc Gemini trả về JSON chuẩn (không markdown)
                            }
                        };

                        int estimatedTokens = _rateLimitHelper.EstimateTokens(promptTemplate);
                        int actualTokens = estimatedTokens;

                        var (batchResult, keyId, initialEstimate) = await _rateLimitHelper.ExecuteWithRateLimitAsync<List<AiElementAnalysis>>(_url,
                            async (urlWithKey) =>
                            {
                                using HttpClient client = new HttpClient();
                                client.Timeout = TimeSpan.FromMinutes(5); // Tăng timeout lên 120 giây cho các request lớn

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

                                // LẤY ACTUAL TOKENS TỪ RESPONSE
                                actualTokens = geminiResponse?.UsageMetadata?.PromptTokenCount ?? estimatedTokens;

                                return DeserializeResponse<List<AiElementAnalysis>>(geminiResponse);
                            },
                            estimatedTokens: estimatedTokens
                            );
                        
                        // UPDATE ACTUAL TOKENS
                        if (actualTokens > 0)
                        {
                            await _rateLimitHelper.RateLimitManager.UpdateActualTokensAsync(keyId, actualTokens, estimatedTokens);
                        }
                        if (batchResult != null)
                        {
                            foreach (var item in batchResult)
                            {
                                finalResults.Add(item);
                            }
                        }
                    }
                    finally
                    {
                        // Giải phóng semaphore để luồng khác có thể chạy
                        semaphore.Release();
                    }

                }));
            }
            // Đợi tất cả các batch chạy xong
            await Task.WhenAll(tasks);

            return finalResults.ToList();
        }

        /// <summary>
        /// Phân tích metadata SEO của trang web sử dụng Gemini AI
        /// </summary>
        /// <param name="metaData">Metadata đã được extract từ HTML</param>
        /// <returns>Kết quả phân tích với suggestions</returns>
        public async Task<MetaDataAnalysisResult> AnalyzeMetaDataSEO(MetaDataAnalysis metaData)
        {
            var jsonReadOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonWriteOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Để hiển thị tiếng Việt đẹp trong prompt
            };

            var openGraphDict = !string.IsNullOrEmpty(metaData.OpenGraphData)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(metaData.OpenGraphData, jsonReadOptions)
                : new Dictionary<string, string>();

            var twitterCardDict = !string.IsNullOrEmpty(metaData.TwitterCardData)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(metaData.TwitterCardData, jsonReadOptions)
                : new Dictionary<string, string>();

            var otherMetaDict = !string.IsNullOrEmpty(metaData.OtherMetaData)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(metaData.OtherMetaData, jsonReadOptions)
                : new Dictionary<string, string>();

            /*Bạn là chuyên gia SEO và phân tích metadata cho website.

                **Nhiệm vụ:** Phân tích metadata sau đây và đưa ra đánh giá + gợi ý tối ưu SEO.

                **Tiêu chí phân tích:**
                1. **Title Tag:**
                - Độ dài tối ưu: 50-60 ký tự
                - Có chứa từ khóa chính không?
                - Có hấp dẫn và mô tả đúng nội dung không?

                2. **Meta Description:**
                - Độ dài tối ưu: 150-160 ký tự
                - Có call-to-action không?
                - Có chứa từ khóa relevant không?

                3. **Meta Keywords:**
                - Tag này đã deprecated, nhưng nếu có thì kiểm tra có spam không

                4. **Charset & Viewport:**
                - Charset nên là UTF-8
                - Viewport phải có cho responsive design

                5. **Canonical URL:**
                - Có được set đúng không?
                - Tránh duplicate content

                6. **Robots Meta Tag:**
                - Có đang block indexing không?
                - index/noindex, follow/nofollow có đúng không?

                7. **Open Graph Tags (og:*):**
                - Kiểm tra có đủ: og:title, og:description, og:image, og:url, og:type không
                - Giá trị có phù hợp không?

                8. **Twitter Cards:**
                - Có twitter:card, twitter:title, twitter:description, twitter:image không?
                - Loại card có phù hợp không? (summary, summary_large_image)

                **Lưu ý quan trọng:**
                - Chỉ trả về JSON, không có text khác
                - Không dùng markdown code block (```json)
                - Nội dung phải 100% tiếng Việt
                - Chỉ đưa ra suggestions cho các tags có vấn đề hoặc thiếu
                - Nếu metadata đã tối ưu hoàn toàn, Suggestions có thể rỗng []
             */
            var promptTemplateBase = _promptAnalysisMetadata;
            var temperature = _temperatureAnalysisMetadata;

            // Serialize metadata thành JSON để đưa vào prompt
            var metaDataJson = JsonSerializer.Serialize(new
            {
                metaData.Title,
                metaData.Description,
                metaData.Keywords,
                metaData.Charset,
                metaData.Viewport,
                metaData.Canonical,
                metaData.Robots,
                OpenGraph = openGraphDict,
                TwitterCard = twitterCardDict,
                OtherMeta = otherMetaDict
            }, jsonWriteOptions);

            string promptTemplate = $@"
                {promptTemplateBase}

                **Dữ liệu Metadata:**
                {metaDataJson}

                **Format Output:**
                Bạn PHẢI trả về JSON với cấu trúc sau (100% tiếng Việt):

                {{
                    ""GeneralAssessment"": ""Đánh giá tổng quan về metadata SEO (tốt/trung bình/cần cải thiện)"",
                    ""Suggestions"": [
                        {{
                            ""TagName"": ""Tên tag (ví dụ: 'title', 'meta description', 'og:image')"",
                            ""CurrentValue"": ""Giá trị hiện tại (hoặc 'Không có' nếu thiếu)"",
                            ""Issue"": ""Vấn đề cụ thể (ví dụ: 'Quá dài', 'Thiếu tag', 'Không tối ưu')"",
                            ""Recommendation"": ""Gợi ý cải thiện cụ thể"",
                            ""IsImportant"": true/false
                        }}
                    ]
                }}";

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
                    Temperature = temperature,      // Nhiệt độ thấp để JSON chuẩn
                    ResponseMimeType = "application/json"
                }
            };

            int estimatedTokens = _rateLimitHelper.EstimateTokens(promptTemplate);
            int actualTokens = estimatedTokens;

            var (analysisResult, keyId, initialEstimate) = await _rateLimitHelper.ExecuteWithRateLimitAsync<MetaDataAnalysisResult>(_url,
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
                    
                    // LẤY ACTUAL TOKENS TỪ RESPONSE
                    actualTokens = geminiResponse?.UsageMetadata?.PromptTokenCount ?? estimatedTokens;
                    
                    return DeserializeResponse<MetaDataAnalysisResult>(geminiResponse);
                },
                estimatedTokens: estimatedTokens
                );

            // UPDATE ACTUAL TOKENS
            if (actualTokens > 0)
            {
                await _rateLimitHelper.RateLimitManager.UpdateActualTokensAsync(keyId, actualTokens, estimatedTokens);
            }

            return analysisResult;
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