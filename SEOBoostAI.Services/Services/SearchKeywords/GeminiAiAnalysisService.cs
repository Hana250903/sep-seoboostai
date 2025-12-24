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
    public class GeminiAiAnalysisService : IGeminiAiAnalysisService
    {
        private readonly ISystemConfigService _systemConfigService;
        private readonly GeminiRateLimitHelper _geminiRateLimitHelper;
        private readonly string _url;

        private readonly string _promptSuggestionTrendAnalysis;


        public GeminiAiAnalysisService(ISystemConfigService systemConfigService, GeminiRateLimitHelper geminiRateLimitHelper)
        {
            _systemConfigService = systemConfigService;
            _geminiRateLimitHelper = geminiRateLimitHelper;
            _url = _systemConfigService.GetValue<string>("GeminiUrl", "");

            _promptSuggestionTrendAnalysis = _systemConfigService.GetValue<string>("GeminiPromptSuggestionTrendAnalysis", "");

        }

        // --- Thực thi Phân tích Lần 2 ---
        public async Task<string> GetTrendAnalysisSuggestionAsync(string originalQuestion, string trendDataJson)
        {
            string promptSuggestionTrendAnalysis = _promptSuggestionTrendAnalysis;
            // === PROMPT HOÀN CHỈNH (CÂN BẰNG GIỮA BẢO MẬT & VĂN PHONG) ===
            string promptTemplate = $@"

            {promptSuggestionTrendAnalysis}
            **DỮ LIỆU ĐẦU VÀO:**
            1.  **Câu hỏi của người dùng (chỉ để lấy bối cảnh):** ""{originalQuestion}""
            2.  **Dữ liệu Phân tích Xu hướng (JSON):** {trendDataJson}
            ";
            // === KẾT THÚC PROMPT HOÀN CHỈNH ===

            var requestData = new GeminiAIRequestModel
            {
                Contents = new[] { new ContentRequest { Parts = new[] { new PartRequest { Text = promptTemplate } } } }
            };

            int estimatedTokens = _geminiRateLimitHelper.EstimateTokens(promptTemplate);
            int actualTokens = estimatedTokens;

            var (analysisResult, keyId, initialEstimate) = await _geminiRateLimitHelper.ExecuteWithRateLimitAsync<string>(
                _url,
                async (urlWithKey) =>
                {
                    using HttpClient client = new HttpClient();
                    string json = JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(urlWithKey, content);
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Lỗi gọi Gemini API: {response.StatusCode}");
                    }
                    string result = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    try
                    {
                        var geminiResponseModel = JsonSerializer.Deserialize<GeminiAIResponseModel>(result, options);

                        // LẤY ACTUAL TOKENS Từ RESPONSE
                        actualTokens = geminiResponseModel?.UsageMetadata?.PromptTokenCount ?? estimatedTokens;

                        if (geminiResponseModel?.Candidates == null || !geminiResponseModel.Candidates.Any())
                        {
                            return "Hiện tại hệ thống đang bận, vui lòng thử lại sau."; // Trả về thông báo thay vì crash
                        }

                        // Lấy văn bản thô
                        string finalAnswer = geminiResponseModel.Candidates.First().Content.Parts.First().Text;
                        return finalAnswer.Trim();
                    }
                    catch (Exception ex)
                    {
                        // Log lỗi nếu cần
                        return "Đã xảy ra lỗi khi xử lý phản hồi từ AI.";
                    }
                },
                estimatedTokens: estimatedTokens
                );

            // UPDATE ACTUAL TOKENS
            if (actualTokens > 0)
            {
                await _geminiRateLimitHelper.RateLimitManager.UpdateActualTokensAsync(keyId, actualTokens, estimatedTokens);
            }

            return analysisResult;
        }
    }
}
