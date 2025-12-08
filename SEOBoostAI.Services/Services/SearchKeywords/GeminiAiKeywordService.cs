using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Service.Helpers;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.SearchKeywords
{
    public class GeminiAiKeywordService : IGeminiAiKeywordService
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly GeminiRateLimitHelper _geminiRateLimitHelper;
        private readonly string _url;

        private readonly string _promptExtractKeywordsFromQuestion;


        public GeminiAiKeywordService(ISystemConfigService systemConfigService, GeminiRateLimitHelper geminiRateLimitHelper)
        {
            _systemConfigService = systemConfigService;
            _geminiRateLimitHelper = geminiRateLimitHelper;
            // Dùng chung key và url với service Gemini cũ
            _url = _systemConfigService.GetValue<string>("GeminiUrl", "");

            _promptExtractKeywordsFromQuestion = _systemConfigService.GetValue<string>("GeminiPromptExtractKeywordsFromQuestion", "");

        }

        // --- Thực thi Phân tích Lần 1 ---
        public async Task<TrendParameters> ExtractKeywordsFromQuestionAsync(string originalQuestion)
        {
            string ExtractKeywordsFromQuestionPrompt = _promptExtractKeywordsFromQuestion;
            // === PROMPT ĐÃ ĐƯỢC CẬP NHẬT (THÔNG MINH HƠN) ===
            string promptTemplate = $@"

            {ExtractKeywordsFromQuestionPrompt}

            Câu hỏi của người dùng:
            {originalQuestion}";

            var requestData = new GeminiAIRequestModel
            {
                Contents = new[] { new ContentRequest { Parts = new[] { new PartRequest { Text = promptTemplate } } } }
            };

            int estimatedTokens = _geminiRateLimitHelper.EstimateTokens(promptTemplate);
            int actualTokens = estimatedTokens;

            var (keywordResult, keyId, initialEstimate) = await _geminiRateLimitHelper.ExecuteWithRateLimitAsync<TrendParameters>(
                _url,
                async (urlWithKey) =>
                {
                    using HttpClient client = new HttpClient();
                    string json = JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(urlWithKey, content);
                    string result = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var geminiResponseModel = JsonSerializer.Deserialize<GeminiAIResponseModel>(result, options);
                    
                    // LẤY ACTUAL TOKENS TỪ RESPONSE
                    actualTokens = geminiResponseModel?.UsageMetadata?.PromptTokenCount ?? estimatedTokens;
                    
                    return DeserializeResponse<TrendParameters>(geminiResponseModel);
                },
                estimatedTokens
                );
            
            // UPDATE ACTUAL TOKENS
            if (actualTokens > 0)
            {
                await _geminiRateLimitHelper.RateLimitManager.UpdateActualTokensAsync(keyId, actualTokens, estimatedTokens);
            }
            
            return keywordResult;
        }

        // --- Hàm Private dọn dẹp JSON (Giống service cũ) ---
        // --- Hàm Private dọn dẹp JSON (Đã nâng cấp để sửa lỗi) ---
        // Thay thế hàm DeserializeResponse cũ trong file GeminiAiKeywordService.cs bằng hàm này:

        private T DeserializeResponse<T>(GeminiAIResponseModel geminiResponse)
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            // 1. Lấy text gốc
            string jsonString = geminiResponse.Candidates.First().Content.Parts.First().Text;

            // 2. Xóa các ký tự markdown cơ bản trước
            jsonString = jsonString.Replace("```json", "").Replace("```", "").Trim();

            // 3. Tìm vị trí bắt đầu và kết thúc của JSON
            int firstBrace = jsonString.IndexOf('{');
            int lastBrace = jsonString.LastIndexOf('}');

            if (firstBrace >= 0 && lastBrace > firstBrace)
            {
                // Cắt lấy phần chuỗi nằm trong ngoặc xa nhất
                jsonString = jsonString.Substring(firstBrace, lastBrace - firstBrace + 1);
            }
            else
            {
                throw new Exception($"Gemini response invalid. Raw: {jsonString}");
            }

            // --- KHẮC PHỤC LỖI CỦA BẠN TẠI ĐÂY ---
            // Kiểm tra nếu chuỗi vẫn còn bị bọc bởi 2 lớp ngoặc {{ ... }} thì gỡ bỏ lớp ngoài cùng
            // Dùng vòng lặp while để xử lý trường hợp Gemini bị điên trả về {{{ ... }}}
            while (jsonString.StartsWith("{{") && jsonString.EndsWith("}}"))
            {
                jsonString = jsonString.Substring(1, jsonString.Length - 2).Trim();
            }

            // Debug: In ra console để kiểm tra nếu còn lỗi
            // Console.WriteLine($"[Final Cleaned JSON]: {jsonString}");

            try
            {
                var result = JsonSerializer.Deserialize<T>(jsonString, options);
                return result;
            }
            catch (JsonException ex)
            {
                throw new Exception($"JSON Parsing Error: {ex.Message}. Cleaned JSON: {jsonString}", ex);
            }
        }
    }
}
