using Microsoft.Extensions.Configuration;
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
        private readonly string _apiKey;

        public PageSpeedService(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            // Lấy API key từ appsettings.json
            _apiKey = configuration["GooglePageSpeed:ApiKey"];
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("Chưa cấu hình GooglePageSpeed:ApiKey trong appsettings.json");
            }
        }

        public async Task<PageSpeedResponse> GetPageSpeedAsync(string url, string strategy = "desktop")
        {
            string endpoint = "https://www.googleapis.com/pagespeedonline/v5/runPagespeed";

            // Xây dựng URL với query parameters
            var queryParams = new Dictionary<string, string>
        {
            { "url", url },
            { "key", _apiKey },
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
