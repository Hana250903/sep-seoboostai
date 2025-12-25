using PuppeteerSharp;
using Newtonsoft.Json;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    /// <summary>
    /// Service sử dụng Puppeteer để audit SEO/Performance của trang web
    /// Thay thế Selenium, lưu kết quả vào AnalysisCache và Element
    /// </summary>
    public class PuppeteerAuditService : IPuppeteerAuditService
    {
        private readonly IAnalysisCacheRepository _cacheRepository;
        private readonly IElementRepository _elementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _browserlessApiKey;

        public PuppeteerAuditService(
            IAnalysisCacheRepository cacheRepository,
            IElementRepository elementRepository,
            IUnitOfWork unitOfWork, ISystemConfigService systemConfigService)
        {
            _cacheRepository = cacheRepository;
            _elementRepository = elementRepository;
            _unitOfWork = unitOfWork;
            _browserlessApiKey = systemConfigService.GetValue<string>("BrowserlessApiKey", "");
        }

        /// <summary>
        /// Chạy audit và lưu kết quả vào database
        /// </summary>
        public async Task<List<Element>> RunAuditAsync(string url, string strategy = "desktop")
        {
            // 1. Khởi động Puppeteer
            await new BrowserFetcher().DownloadAsync();

            var connectOptions = new ConnectOptions
            {
                // Kết nối tới máy chủ trình duyệt bên ngoài
                BrowserWSEndpoint = $"wss://chrome.browserless.io?token={_browserlessApiKey}"
            };

            using var browser = await Puppeteer.ConnectAsync(connectOptions);
            await using var page = await browser.NewPageAsync();

            // Set Viewport theo strategy
            if (strategy == "mobile")
                await page.SetViewportAsync(new ViewPortOptions { Width = 375, Height = 812 });
            else
                await page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080 });

            // Load trang và đợi network idle
            await page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });

            // 3. BẮT ĐẦU CHECK - Sử dụng PageAuditChecks static class
            var elements = new List<Element>();

            var metaIssues = await PageAuditChecks.CheckMetadataAsync(page);
            elements.AddRange(metaIssues.Select(e => CreateElement(e)));

            var imageIssues = await PageAuditChecks.CheckImagesAsync(page);
            elements.AddRange(imageIssues.Select(e => CreateElement(e)));

            var scriptIssues = await PageAuditChecks.CheckScriptsAsync(page);
            elements.AddRange(scriptIssues.Select(e => CreateElement(e)));

            var seoIssues = await PageAuditChecks.CheckSeoAsync(page);
            elements.AddRange(seoIssues.Select(e => CreateElement(e)));

            var perfIssues = await PageAuditChecks.CheckPerformanceAsync(page);
            elements.AddRange(perfIssues.Select(e => CreateElement(e)));

            var a11yIssues = await PageAuditChecks.CheckAccessibilityAsync(page);
            elements.AddRange(a11yIssues.Select(e => CreateElement(e)));

            Console.WriteLine($"[PUPPETEER AUDIT] Completed: {url}, found {elements.Count} issues");
            return elements;
        }

        /// <summary>
        /// Debug Scan: Xem Puppeteer đang đọc được gì từ trang web (không lưu vào DB)
        /// </summary>
        public async Task<object> DebugScanAsync(string url)
        {
            await using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });

            await using var page = await browser.NewPageAsync();
            await page.GoToAsync(url, new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.Networkidle2 } });

            var rawJson = await page.EvaluateExpressionAsync<string>(@"
                JSON.stringify({
                    meta: {
                        title: document.title,
                        description: document.querySelector('meta[name=""description""]')?.content || null,
                        viewport: document.querySelector('meta[name=""viewport""]')?.content || null,
                        canonical: document.querySelector('link[rel=""canonical""]')?.href || null,
                        lang: document.documentElement.lang || null
                    },
                    h1Tags: Array.from(document.querySelectorAll('h1')).map(h => h.textContent),
                    ogTags: {
                        ogTitle: document.querySelector('meta[property=""og:title""]')?.content || null,
                        ogDesc: document.querySelector('meta[property=""og:description""]')?.content || null,
                        ogImage: document.querySelector('meta[property=""og:image""]')?.content || null
                    },
                    images: Array.from(document.querySelectorAll('img')).map(img => ({
                        src: img.src,
                        alt: img.alt,
                        loading: img.getAttribute('loading'),
                        width: img.width,
                        height: img.height,
                        snippet: img.outerHTML.substring(0, 150)
                    })),
                    domStats: {
                        totalNodes: document.querySelectorAll('*').length,
                        scripts: document.querySelectorAll('script').length,
                        styles: document.querySelectorAll('style').length,
                        links: document.querySelectorAll('link').length
                    }
                })
            ");

            var data = JsonConvert.DeserializeObject<dynamic>(rawJson);

            var analysis = new List<string>();
            if (data?.images != null)
            {
                foreach (var img in data.images)
                {
                    string alt = img?.alt?.ToString() ?? "";
                    string loading = img?.loading?.ToString() ?? "";
                    string src = img?.src?.ToString() ?? "";
                    string srcShort = src.Length > 50 ? src.Substring(0, 50) + "..." : src;

                    if (string.IsNullOrEmpty(alt))
                        analysis.Add($"❌ IMG thiếu ALT: {srcShort}");
                    else
                        analysis.Add($"✅ IMG có ALT: '{alt}'");

                    if (string.IsNullOrEmpty(loading) || loading != "lazy")
                        analysis.Add($"⚠️ IMG thiếu lazy: {srcShort}");
                }
            }

            return new
            {
                url,
                scanTime = DateTime.Now,
                rawData = data,
                quickAnalysis = analysis
            };
        }

        #region Helpers

        private Element CreateElement(AuditIssueDto issue)
        {
            return new Element
            {
                AuditId = issue.AuditId,
                Title = issue.Title,
                ExtractedEvidenceJson = issue.Evidence != null ? JsonConvert.SerializeObject(issue.Evidence) : "[]",
                Description = issue.Description,
                HasSuggestion = true,
                CreatedAt = DateTime.Now
            };
        }

        #endregion
    }
}
