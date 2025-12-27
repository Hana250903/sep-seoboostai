using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Service.Helpers;
using SEOBoostAI.Service.Services.Interfaces;
using System.Text;
using System.Text.Json;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    /// <summary>
    /// Service gọi Gemini AI để fix code SEO/Performance
    /// </summary>
    public class GeminiFixService : IGeminiFixService
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly GeminiRateLimitHelper _rateLimitHelper;
        private readonly string _url;

        public GeminiFixService(
            ISystemConfigService systemConfigService,
            GeminiRateLimitHelper rateLimitHelper)
        {
            _systemConfigService = systemConfigService;
            _rateLimitHelper = rateLimitHelper;
            _url = _systemConfigService.GetValue<string>("GeminiUrl", "");
        }

        public async Task<string> FixCodeAsync(string code, string errorTitle, string issueKey)
        {
            // 1. Tạo instruction theo loại lỗi (tiếng Việt)
            /*
             * Hướng dẫn mặc định: Sửa lỗi trong đoạn code bên dưới.
             * Nếu lỗi là MISSING (thiếu element): Thêm tag hoặc attribute bị thiếu vào đúng vị trí 
             * (ví dụ: <title> trong <head>, 'alt' trong <img>).
             */
            string instruction = "Sửa lỗi trong đoạn code bên dưới.";
            if (issueKey.Contains("missing"))
            {
                instruction = "Lỗi cho thấy đang THIẾU element. Hãy THÊM tag hoặc attribute bị thiếu vào đúng vị trí parent chuẩn (ví dụ: <title> trong <head>, 'alt' trong <img>).";
            }

            // 2. Tạo prompt (tiếng Việt)
            /*
             * Vai trò: Chuyên gia Web Developer & SEO cao cấp.
             * Nhiệm vụ: Sửa lỗi theo hướng dẫn.
             * 
             * Quy tắc nghiêm ngặt:
             * 1. CHỈ trả về code đã sửa hoàn chỉnh, nguyên bản.
             * 2. KHÔNG dùng markdown formatting (không ```html, không ```).
             * 3. KHÔNG giải thích hay viết text khác.
             */
            var prompt = $@"
                Vai trò: Chuyên gia Web Developer & SEO cao cấp.

                Nhiệm vụ: Sửa lỗi sau trong code và trả về code đã sửa hoàn chỉnh.

                Thông tin lỗi:
                - Mã lỗi: {issueKey}
                - Mô tả: {errorTitle}
                - Hướng dẫn sửa: {instruction}

                Quy tắc BẮT BUỘC:
                1. Trả về TOÀN BỘ code đã sửa, không cắt xén
                2. KHÔNG dùng markdown (không ```, không giải thích)
                3. CHỈ trả về code, không có text nào khác
                4. Giữ nguyên cấu trúc và format của code gốc
                5. Chỉ thay đổi phần liên quan đến lỗi

                Code cần sửa:
                {code}";

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
                                Text = prompt,
                            }
                        }
                    }
                },
                GenerationConfig = new GenerationConfig
                {
                    Temperature = 0.2 // Giữ nhiệt độ thấp để output chuẩn
                }
            };

            int estimatedTokens = _rateLimitHelper.EstimateTokens(prompt);
            int actualTokens = estimatedTokens;

            // 3. Gọi qua Helper để xử lý rate limit & auto switch key
            var (fixedCode, keyId, initialEstimate) = await _rateLimitHelper.ExecuteWithRateLimitAsync<string>(_url,
                async (urlWithKey) =>
                {
                    using HttpClient client = new HttpClient();
                    client.Timeout = TimeSpan.FromMinutes(2);

                    string json = JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(urlWithKey, content);

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Gemini API Error: {response.ReasonPhrase}", null, response.StatusCode);
                    }

                    string result = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var geminiResponse = JsonSerializer.Deserialize<GeminiAIResponseModel>(result, options);

                    // Lấy actual tokens từ response
                    actualTokens = geminiResponse?.UsageMetadata?.PromptTokenCount ?? estimatedTokens;

                    // Trích xuất code từ response
                    string rawCode = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;
                    
                    if (string.IsNullOrEmpty(rawCode))
                    {
                        Console.WriteLine("[GEMINI FIX] AI không trả về code, giữ nguyên code gốc.");
                        return code;
                    }

                    // Clean code - loại bỏ markdown formatting
                    return rawCode
                        .Replace("```html", "")
                        .Replace("```javascript", "")
                        .Replace("```css", "")
                        .Replace("```xml", "")
                        .Replace("```jsx", "")
                        .Replace("```tsx", "")
                        .Replace("```", "")
                        .Trim();
                },
                estimatedTokens: estimatedTokens
            );

            // 4. Update actual tokens
            if (actualTokens > 0)
            {
                await _rateLimitHelper.RateLimitManager.UpdateActualTokensAsync(keyId, actualTokens, estimatedTokens);
            }

            return fixedCode ?? code;
        }
    }
}
