using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Net;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Helpers
{
    public class GeminiRateLimitHelper
    {
        private readonly IGeminiRateLimitManager _rateLimitManager;

        public GeminiRateLimitHelper(IGeminiRateLimitManager rateLimitManager)
        {
            _rateLimitManager = rateLimitManager;
        }

        /// <summary>
        /// Ước tính số tokens từ text (1 token ≈ 4 characters)
        /// </summary>
        public int EstimateTokens(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            return (int)Math.Ceiling(text.Length / 4.0);
        }

        /// <summary>
        /// Execute Gemini API request với rate limit handling và auto retry
        /// </summary>
        public async Task<T> ExecuteWithRateLimitAsync<T>(
            string baseUrl,
            Func<string, Task<T>> apiCallFunc,
            int maxRetries = 3)
        {
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                GeminiKey availableKey = null;
                try
                {
                    // Lấy key khả dụng từ rate limit manager
                    availableKey = await _rateLimitManager.GetAvailableKeyAsync();
                    string fullUrl = $"{baseUrl}?key={availableKey.ApiKey}";

                    // Thực thi API call
                    var result = await apiCallFunc(fullUrl);

                    // Nếu thành công, ghi nhận usage (estimate basic 100 tokens)
                    await _rateLimitManager.RecordUsageAsync(availableKey.Id, 100);

                    return result;
                }
                catch (HttpRequestException ex) when (availableKey != null &&
                    (ex.StatusCode == (HttpStatusCode)429 || ex.StatusCode == (HttpStatusCode)428))
                {
                    // Đánh dấu key bị rate limited
                    await _rateLimitManager.MarkKeyRateLimitedAsync(availableKey.Id);

                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        throw new InvalidOperationException(
                            $"Đã retry {maxRetries} lần nhưng vẫn gặp rate limit. Vui lòng thử lại sau.", ex);
                    }

                    // Chờ một chút trước khi retry
                    await Task.Delay(500);
                }
                catch
                {
                    // Các exception khác, không retry
                    throw;
                }
            }

            throw new InvalidOperationException("Không thể thực hiện request sau nhiều lần retry.");
        }

        /// <summary>
        /// Lấy API key khả dụng và tạo full URL
        /// </summary>
        public async Task<(GeminiKey key, string fullUrl)> GetAvailableKeyAndUrlAsync(string baseUrl)
        {
            var key = await _rateLimitManager.GetAvailableKeyAsync();
            var fullUrl = $"{baseUrl}?key={key.ApiKey}";
            return (key, fullUrl);
        }

        /// <summary>
        /// Ghi nhận usage sau khi request thành công
        /// </summary>
        public async Task RecordSuccessAsync(int keyId, int estimatedTokens = 100)
        {
            await _rateLimitManager.RecordUsageAsync(keyId, estimatedTokens);
        }

        /// <summary>
        /// Đánh dấu key bị rate limited
        /// </summary>
        public async Task MarkKeyRateLimitedAsync(int keyId)
        {
            await _rateLimitManager.MarkKeyRateLimitedAsync(keyId);
        }
    }
}
