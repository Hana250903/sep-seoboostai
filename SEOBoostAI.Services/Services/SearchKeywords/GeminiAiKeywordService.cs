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
            // === PROMPT ĐÃ ĐƯỢC CẬP NHẬT (THÔNG MINH HƠN) ===
            string promptTemplate = $@"

            {_promptExtractKeywordsFromQuestion}

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
