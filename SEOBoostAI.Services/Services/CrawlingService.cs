using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class CrawlingService : ICrawlingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CrawlingService> _logger;

        public CrawlingService(IHttpClientFactory httpClientFactory, ILogger<CrawlingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<HtmlDocument> GetHtmlDocumentAsync(string url)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                // Giả lập user agent của trình duyệt để tránh bị chặn
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/100.0.0.0 Safari/537.36");

                _logger.LogInformation("Bắt đầu tải HTML từ: {Url}", url);
                string htmlString = await httpClient.GetStringAsync(url);

                if (string.IsNullOrWhiteSpace(htmlString))
                {
                    _logger.LogWarning("HTML trả về rỗng từ: {Url}", url);
                    return null;
                }

                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlString);

                _logger.LogInformation("Tải và parse HTML thành công từ: {Url}", url);
                return htmlDoc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tải hoặc parse HTML từ: {Url}", url);
                return null; // Trả về null nếu có lỗi
            }
        }
    }
}
