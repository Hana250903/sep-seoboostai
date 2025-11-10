using Microsoft.Extensions.Logging;
using SEOBoostAI.Service.DTOs; // Sửa lại using cho đúng
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class SerpApiService : ISerpApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISystemConfigService _systemConfigService;
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly ILogger<SerpApiService> _logger;

        public SerpApiService(IHttpClientFactory httpClientFactory,
                              ISystemConfigService systemConfigService,
                              ILogger<SerpApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _systemConfigService = systemConfigService;
            _logger = logger;

            _apiKey = _systemConfigService.GetValue<string>("serpapigia", "");
            _endpoint = _systemConfigService.GetValue<string>("serpapiurlgia", "https://serpapi.com/search.json");
        }

        // --- Hàm Helper (Không thay đổi) ---
        private async Task<T> GetTrendDataAsync<T>(TrendParameters parameters, string dataType)
        {
            var httpClient = _httpClientFactory.CreateClient();
            var queryParams = new Dictionary<string, string>
            {
                { "engine", "google_trends" },
                { "q", parameters.Query },
                { "geo", parameters.Geolocation },
                { "hl", parameters.Language },
                { "date", parameters.Timeframe },
                { "api_key", _apiKey },
                { "data_type", dataType }
            };
            var fullUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(_endpoint, queryParams);

            _logger.LogInformation("Đang gọi SerpApi: {url}", fullUrl);

            try
            {
                var response = await httpClient.GetFromJsonAsync<T>(fullUrl);
                return response;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Lỗi khi gọi SerpApi cho {dataType}", dataType);
                throw new Exception($"Lỗi khi gọi SerpApi ({dataType}): {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Lỗi khi deserialize JSON từ SerpApi cho {dataType}", dataType);
                throw new Exception($"Lỗi khi đọc kết quả từ SerpApi ({dataType}): {ex.Message}", ex);
            }
        }

        // --- Implement các phương thức ---

        // Hàm đã test (Ảnh 2 & 4)
        public async Task<SerpInterestOverTimeResponse> GetInterestOverTimeAsync(TrendParameters parameters)
        {
            return await GetTrendDataAsync<SerpInterestOverTimeResponse>(parameters, "TIMESERIES");
        }

        // --- 4 HÀM MỚI ---

        // Hàm cho Ảnh 3
        public async Task<SerpInterestByRegionResponse> GetInterestByRegionAsync(TrendParameters parameters)
        {
            return await GetTrendDataAsync<SerpInterestByRegionResponse>(parameters, "GEO_MAP_0");
        }

        // Hàm cho Ảnh 1
        public async Task<SerpRegionComparisonResponse> GetComparedBreakdownByRegionAsync(TrendParameters parameters)
        {
            return await GetTrendDataAsync<SerpRegionComparisonResponse>(parameters, "GEO_MAP");
        }

        // Hàm cho Ảnh 5
        public async Task<SerpRelatedTopicsResponse> GetRelatedTopicsAsync(TrendParameters parameters)
        {
            return await GetTrendDataAsync<SerpRelatedTopicsResponse>(parameters, "RELATED_TOPICS");
        }

        // Hàm cho Ảnh 6
        public async Task<SerpRelatedQueriesResponse> GetRelatedQueriesAsync(TrendParameters parameters)
        {
            return await GetTrendDataAsync<SerpRelatedQueriesResponse>(parameters, "RELATED_QUERIES");
        }
    }
}