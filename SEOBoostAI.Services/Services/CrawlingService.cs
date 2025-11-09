using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium.BiDi.Script;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
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
        private readonly IElementRepository _elementRepository;

        public CrawlingService(IHttpClientFactory httpClientFactory, ILogger<CrawlingService> logger, IElementRepository elementRepository)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _elementRepository = elementRepository;
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
                return null;
            }
        }

        public List<ElementFinding> CheckLCP(HtmlDocument htmlDoc)
        {
            var findings = new List<ElementFinding>();

            var lcpCandidates = htmlDoc.DocumentNode.SelectNodes(
                "//img | //video[@poster] | //h1 | //p[string-length() > 150]"
            );

            if (lcpCandidates == null)
            {
                return findings;
            }

            foreach (var node in lcpCandidates)
            {
                findings.Add(new ElementFinding
                {
                    TagName = node.Name,
                    InnerHtml = node.InnerHtml,
                    OuterHtml = node.OuterHtml,
                });
            }

            return findings;

        }

        public List<ElementFinding> CheckCLS(HtmlDocument htmlDoc)
        {
            var findings = new List<ElementFinding>();

            var allImages = htmlDoc.DocumentNode.SelectNodes("//img");
            if (allImages != null)
            {
                var imageClsCandidates = allImages.Where(img =>
                {
                    bool hasHtmlWidth = img.Attributes["width"] != null;
                    bool hasHtmlHeight = img.Attributes["height"] != null;
                    if (hasHtmlWidth && hasHtmlHeight) return false;

                    var styleAttribute = img.Attributes["style"];
                    if (styleAttribute != null)
                    {
                        string styleValue = styleAttribute.Value;
                        bool hasInlineWidth = styleValue.Contains("width:");
                        bool hasInlineHeight = styleValue.Contains("height:");
                        if (hasInlineWidth && hasInlineHeight) return false;
                    }
                    return true;
                }).ToList();

                foreach (var item in imageClsCandidates)
                {
                    findings.Add(new ElementFinding
                    {
                        TagName = item.Name,
                        InnerHtml = item.InnerHtml,
                        OuterHtml = item.OuterHtml,
                    });
                }
            }

            var iframes = htmlDoc.DocumentNode.SelectNodes("//iframe[not(@width) or not(@height)]");
            if (iframes != null)
            {
                foreach (var item in iframes)
                {
                    findings.Add(new ElementFinding
                    {
                        TagName = item.Name,
                        InnerHtml = item.InnerHtml,
                        OuterHtml = item.OuterHtml,
                    });
                }
            }

            return findings;
        }

        public List<ElementFinding> CheckFCP(HtmlDocument htmlDoc)
        {
            //var blockingStylesheets = htmlDoc.DocumentNode.SelectNodes("//head/link[@rel='stylesheet']");
            //var blockingScripts = htmlDoc.DocumentNode.SelectNodes("//head/script[not(@async) and not(@defer) and @src]");

            var findings = new List<ElementFinding>();

            var allBlockingResources = htmlDoc.DocumentNode.SelectNodes(
                "//head/link[@rel='stylesheet'] | //head/script[@src and not(@async) and not(@defer)]"
            );

            if (allBlockingResources == null)
            {
                return findings;
            }

            if (allBlockingResources != null)
            {
                foreach (var resource in allBlockingResources)
                {
                    //if (resource.Name == "link")
                    //{
                    //    Console.WriteLine($"CSS: {resource.GetAttributeValue("href", "N/A")}");
                    //}
                    //else if (resource.Name == "script")
                    //{
                    //    Console.WriteLine($"Script: {resource.GetAttributeValue("src", "N/A")}");
                    //}

                    findings.Add(new ElementFinding
                    {
                        TagName = resource.Name,
                        InnerHtml = resource.InnerHtml,
                        OuterHtml = resource.OuterHtml,
                    });
                }
            }

            return findings;
        }

        public List<ElementFinding> FindThirdPartyScripts(HtmlDocument htmlDoc, string originalUrl)
        {
            var findings = new List<ElementFinding>();

            // 1. Lấy tên miền "first-party" (chủ nhà)
            Uri baseUri;
            if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out baseUri))
            {
                return null;
            }

            // Chuẩn hóa tên miền gốc, ví dụ "www.blazorise.com" -> "blazorise.com"
            string firstPartyHost = GetRootDomain(baseUri.Host);

            // 2. Lấy tất cả các thẻ <script> có thuộc tính [src]
            var scriptNodes = htmlDoc.DocumentNode.SelectNodes("//script[@src]");

            if (scriptNodes == null)
            {
                return findings;
            }

            //Phân tích Script
            foreach (var scriptNode in scriptNodes)
            {
                string src = scriptNode.GetAttributeValue("src", string.Empty);

                // Bỏ qua nếu src rỗng
                if (string.IsNullOrWhiteSpace(src)) continue;

                // 3. Xử lý các URL tương đối (ví dụ: /js/app.js)
                // Nếu không phải là URL tuyệt đối, nó là first-party
                if (!src.StartsWith("http://") && !src.StartsWith("https://") && !src.StartsWith("//"))
                {
                    // Đây là script first-party (ví dụ: /_framework/blazor.web.js)
                    continue;
                }

                // 4. Phân tích tên miền của script
                Uri scriptUri;
                // Xử lý URL bắt đầu bằng // (ví dụ: //cdn.example.com)
                if (src.StartsWith("//"))
                {
                    src = "https:" + src;
                }

                if (Uri.TryCreate(src, UriKind.Absolute, out scriptUri))
                {
                    string scriptHost = GetRootDomain(scriptUri.Host);

                    // 5. So sánh tên miền
                    if (scriptHost != firstPartyHost)
                    {
                        findings.Add(new ElementFinding
                        {
                            TagName = scriptNode.Name,
                            OuterHtml = scriptNode.OuterHtml,
                            InnerHtml = scriptNode.InnerHtml,
                        });
                    }
                }
            }
            return findings;
        }

        private string GetRootDomain(string host)
        {
            if (host.StartsWith("www."))
            {
                host = host.Substring(4);
            }
            return host;
        }
    }
}
