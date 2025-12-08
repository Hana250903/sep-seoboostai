using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Service.Helpers;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.Service.Services.ContentOptimizations
{
	public class GeminiContentOptimizer : IGeminiContentOptimizer
	{
		private readonly ISystemConfigService _systemConfigService;
        private readonly GeminiRateLimitHelper _geminiRateLimitHelper;
		private readonly string _url;
		private readonly string _sensitiveWordsRaw;
		private readonly string _promtGeminiContentOptimization;

		// Constructor để nhận các dependencies
		public GeminiContentOptimizer(ISystemConfigService systemConfigService, GeminiRateLimitHelper geminiRateLimitHelper)
		{
			_systemConfigService = systemConfigService;
            _geminiRateLimitHelper = geminiRateLimitHelper;
            _url = _systemConfigService.GetValue<string>("GeminiUrl", "");
			_sensitiveWordsRaw = _systemConfigService.GetValue<string>("SensitiveWordsBlacklist", "");
			_promtGeminiContentOptimization = _systemConfigService.GetValue<string>("GeminiContentOptimizationPrompt", "");
		}

		public async Task<AiOptimizationResponse> OptimizeContentAsync(OptimizeRequestDto request)
		{
			// --- 1. LỌC TỪ CẤM (BLACKLIST) ---
			string sensitiveWordsRaw = _sensitiveWordsRaw;

			if (!string.IsNullOrEmpty(sensitiveWordsRaw))
			{
				var blackList = sensitiveWordsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries)
												 .Select(w => w.Trim().ToLower())
												 .ToList();

				string userContentLower = request.Content.ToLower();

				foreach (var word in blackList)
				{
					if (userContentLower.Contains(word))
					{
						// CHẶN NGAY LẬP TỨC VÀ NÉM RA LỖI (ĐỂ KHÔNG LƯU DB)
						throw new ArgumentException($"Yêu cầu bị từ chối: Nội dung chứa từ khóa nhạy cảm hoặc vi phạm chính sách ('{word}'). Vui lòng loại bỏ và thử lại.");
					}
				}
			}

			string rawPrompt = _promtGeminiContentOptimization;

			string promptTemplate = rawPrompt
				.Replace("[[KEYWORD]]", request.Keyword)
				.Replace("[[CONTENT]]", request.Content)
				.Replace("[[LENGTH]]", request.ContentLength)
				.Replace("[[LEVEL]]", request.OptimizationLevel.ToString())
				.Replace("[[READABILITY]]", request.ReadabilityLevel);

			var requestData = new GeminiAIRequestModel
			{
				Contents = new[]
				{
					new ContentRequest
					{
						Parts = new[]
						{
							new PartRequest { Text = promptTemplate }
						}
					}
				},
				GenerationConfig = new GenerationConfig { ResponseMimeType = "application/json" },
				SafetySettings = new List<SafetySetting>
				{
					new SafetySetting { Category = "HARM_CATEGORY_HATE_SPEECH", Threshold = "BLOCK_LOW_AND_ABOVE" },
					new SafetySetting { Category = "HARM_CATEGORY_HARASSMENT", Threshold = "BLOCK_LOW_AND_ABOVE" },
					new SafetySetting { Category = "HARM_CATEGORY_SEXUALLY_EXPLICIT", Threshold = "BLOCK_LOW_AND_ABOVE" },
					new SafetySetting { Category = "HARM_CATEGORY_DANGEROUS_CONTENT", Threshold = "BLOCK_LOW_AND_ABOVE" }
				}
			};

			int estimatedTokens = _geminiRateLimitHelper.EstimateTokens(promptTemplate);
			int actualTokens = estimatedTokens;

            var (geminiResponse, keyId, initialEstimate) = await _geminiRateLimitHelper.ExecuteWithRateLimitAsync<AiOptimizationResponse>(_url,
				async (urlWithKey) =>
				{
					using HttpClient client = new HttpClient();
                    string json = JsonSerializer.Serialize(requestData);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
					var response = await client.PostAsync(urlWithKey, content);
					string result = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"Lỗi từ Gemini API: {response.StatusCode}. Chi tiết: {result}");
                    }
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
					var geminiResponseModel = JsonSerializer.Deserialize<GeminiAIResponseModel>(result, options);
					
					// LẤY ACTUAL TOKENS TỪ RESPONSE
					actualTokens = geminiResponseModel?.UsageMetadata?.PromptTokenCount ?? estimatedTokens;
					
					return DeserializeResponse<AiOptimizationResponse>(geminiResponseModel);
                },
				estimatedTokens: estimatedTokens
                );

			// UPDATE ACTUAL TOKENS
			if (actualTokens > 0)
			{
				await _geminiRateLimitHelper.RateLimitManager.UpdateActualTokensAsync(keyId, actualTokens, estimatedTokens);
			}

			return geminiResponse;
		}

		// Hàm helper này nên để private trong class này hoặc static trong utils
		private T DeserializeResponse<T>(GeminiAIResponseModel geminiResponse)
		{
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			string dirtyJsonString = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

			if (string.IsNullOrEmpty(dirtyJsonString))
			{
				// Nếu không có text, có thể do bị chặn bởi SafetyFilter của Google
				throw new InvalidOperationException("Gemini từ chối trả lời (Có thể do vi phạm nội dung an toàn).");
			}

			string cleanJsonString = dirtyJsonString
					.Replace("```json", "")
					.Replace("```", "")
					.Trim();

			return JsonSerializer.Deserialize<T>(cleanJsonString, options);
		}
	}
}