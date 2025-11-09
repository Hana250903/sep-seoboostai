using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class PageSpeedService : IPageSpeedService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISystemConfigService _systemConfigService;

        public PageSpeedService(IHttpClientFactory httpClientFactory, ISystemConfigService systemConfigService)
        {
            _httpClientFactory = httpClientFactory;
            _systemConfigService = systemConfigService;
        }

        public async Task<PageSpeedResponse> GetPageSpeedAsync(string url, string strategy = "desktop")
        {
            string endpoint = _systemConfigService.GetValue<string>("PageSpeedAPI", "link");
            var apiKey = _systemConfigService.GetValue<string>("ApiKey", "api");

            // Xây dựng URL với query parameters
            var queryParams = new Dictionary<string, string>
            {
                { "url", url },
                { "key", apiKey },
                { "strategy", strategy }
            };
            var fullUrl = Microsoft.AspNetCore.WebUtilities.QueryHelpers.AddQueryString(endpoint, queryParams);

            var httpClient = _httpClientFactory.CreateClient();

            try
            {
                // Gửi request và tự động deserialize JSON
                var response = await httpClient.GetFromJsonAsync<PageSpeedResponse>(fullUrl);
                return response;
            }
            catch (HttpRequestException ex)
            {
                // Xử lý lỗi (ví dụ: API key sai, URL không hợp lệ)
                throw new Exception($"Lỗi khi gọi Google PageSpeed API: {ex.Message}", ex);
            }
        }
    }
}
