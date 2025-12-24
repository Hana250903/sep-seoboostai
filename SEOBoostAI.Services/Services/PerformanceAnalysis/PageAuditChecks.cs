using Newtonsoft.Json;
using PuppeteerSharp;
using SEOBoostAI.Repository.ModelExtensions;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    /// <summary>
    /// Các phương thức check SEO, Performance, Accessibility cho Puppeteer Audit
    /// </summary>
    public static class PageAuditChecks
    {
        #region Metadata Checks

        public static async Task<List<AuditIssueDto>> CheckMetadataAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();

            var rawJson = await page.EvaluateExpressionAsync<string>(@"
                JSON.stringify({
                    title: document.title || '',
                    description: document.querySelector('meta[name=""description""]')?.content || '',
                    viewport: document.querySelector('meta[name=""viewport""]')?.content || ''
                })
            ");

            var metaData = JsonConvert.DeserializeObject<dynamic>(rawJson);
            string title = metaData?.title?.ToString() ?? "";
            string description = metaData?.description?.ToString() ?? "";
            string viewport = metaData?.viewport?.ToString() ?? "";

            if (string.IsNullOrEmpty(title))
            {
                issues.Add(new AuditIssueDto("meta-missing-title", "Thiếu thẻ Title", "Trang web không có tiêu đề.", null));
            }

            if (string.IsNullOrEmpty(description))
            {
                issues.Add(new AuditIssueDto("meta-missing-desc", "Thiếu Meta Description", "Chưa có mô tả trang.", null));
            }

            if (string.IsNullOrEmpty(viewport) || !viewport.Contains("width=device-width"))
            {
                issues.Add(new AuditIssueDto("meta-viewport-invalid", "Cấu hình Viewport chưa chuẩn", "Thẻ viewport chưa tối ưu cho mobile.", null));
            }

            return issues;
        }

        #endregion

        #region Image Checks

        public static async Task<List<AuditIssueDto>> CheckImagesAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();

            var rawJson = await page.EvaluateExpressionAsync<string>(@"
                JSON.stringify(
                    Array.from(document.querySelectorAll('img')).map(img => ({
                        src: img.src,
                        alt: img.alt,
                        loading: img.getAttribute('loading'),
                        snippet: img.outerHTML.substring(0, 200)
                    }))
                )
            ");

            var images = JsonConvert.DeserializeObject<List<dynamic>>(rawJson) ?? new List<dynamic>();

            var missingAltEvidence = new List<string>();
            var missingLazyEvidence = new List<string>();

            foreach (var img in images)
            {
                string alt = img?.alt?.ToString() ?? "";
                string loading = img?.loading?.ToString() ?? "";
                string snippet = img?.snippet?.ToString() ?? "";

                if (string.IsNullOrEmpty(alt))
                    missingAltEvidence.Add(snippet);

                if (string.IsNullOrEmpty(loading) || loading != "lazy")
                    missingLazyEvidence.Add(snippet);
            }

            if (missingAltEvidence.Count > 0)
                issues.Add(new AuditIssueDto("img-missing-alt", "Hình ảnh thiếu thẻ Alt", 
                    $"{missingAltEvidence.Count} hình ảnh thiếu alt.", missingAltEvidence));

            if (missingLazyEvidence.Count > 0)
                issues.Add(new AuditIssueDto("img-missing-lazy", "Hình ảnh thiếu Lazy Load", 
                    $"{missingLazyEvidence.Count} hình ảnh thiếu loading=\"lazy\".", missingLazyEvidence));

            return issues;
        }

        #endregion

        #region Script Checks

        public static async Task<List<AuditIssueDto>> CheckScriptsAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();

            var rawJson = await page.EvaluateExpressionAsync<string>(@"
                JSON.stringify(
                    Array.from(document.querySelectorAll('head script[src]'))
                        .filter(s => !s.hasAttribute('async') && !s.hasAttribute('defer'))
                        .map(s => s.outerHTML)
                )
            ");

            var blockingScripts = JsonConvert.DeserializeObject<List<string>>(rawJson) ?? new List<string>();

            if (blockingScripts.Count > 0)
            {
                issues.Add(new AuditIssueDto("js-render-blocking", "JavaScript chặn hiển thị",
                    $"{blockingScripts.Count} script đang chặn render.", blockingScripts));
            }

            return issues;
        }

        #endregion

        #region SEO Checks

        public static async Task<List<AuditIssueDto>> CheckSeoAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();

            var rawJson = await page.EvaluateExpressionAsync<string>(@"
                JSON.stringify({
                    h1Count: document.querySelectorAll('h1').length,
                    h1Texts: Array.from(document.querySelectorAll('h1')).map(h => h.textContent.trim()),
                    hasOgTitle: !!document.querySelector('meta[property=""og:title""]'),
                    hasOgDesc: !!document.querySelector('meta[property=""og:description""]'),
                    hasOgImage: !!document.querySelector('meta[property=""og:image""]'),
                    hasCanonical: !!document.querySelector('link[rel=""canonical""]'),
                    htmlLang: document.documentElement.lang || ''
                })
            ");

            var seoData = JsonConvert.DeserializeObject<dynamic>(rawJson);
            int h1Count = (int)(seoData?.h1Count ?? 0);
            var h1Texts = seoData?.h1Texts?.ToObject<List<string>>() ?? new List<string>();
            bool hasOgTitle = (bool)(seoData?.hasOgTitle ?? false);
            bool hasOgDesc = (bool)(seoData?.hasOgDesc ?? false);
            bool hasOgImage = (bool)(seoData?.hasOgImage ?? false);
            bool hasCanonical = (bool)(seoData?.hasCanonical ?? false);
            string htmlLang = seoData?.htmlLang?.ToString() ?? "";

            if (h1Count == 0)
                issues.Add(new AuditIssueDto("seo-missing-h1", "Thiếu thẻ H1", "Trang web cần có 1 thẻ H1.", null));
            else if (h1Count > 1)
                issues.Add(new AuditIssueDto("seo-multiple-h1", $"Có {h1Count} thẻ H1", "Trang chỉ nên có 1 H1.", h1Texts));

            if (!hasOgTitle || !hasOgDesc || !hasOgImage)
            {
                var missing = new List<string>();
                if (!hasOgTitle) missing.Add("og:title");
                if (!hasOgDesc) missing.Add("og:description");
                if (!hasOgImage) missing.Add("og:image");
                issues.Add(new AuditIssueDto("seo-missing-og-tags", "Thiếu Open Graph tags", 
                    $"Thiếu: {string.Join(", ", missing)}.", missing));
            }

            if (!hasCanonical)
                issues.Add(new AuditIssueDto("seo-missing-canonical", "Thiếu Canonical URL", "Cần thêm link canonical.", null));

            if (string.IsNullOrEmpty(htmlLang))
                issues.Add(new AuditIssueDto("seo-missing-lang", "Thẻ HTML thiếu lang", "Thêm lang=\"vi\" hoặc \"en\".", null));

            return issues;
        }

        #endregion

        #region Performance Checks

        public static async Task<List<AuditIssueDto>> CheckPerformanceAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();

            var rawJson = await page.EvaluateExpressionAsync<string>(@"
                JSON.stringify({
                    domNodeCount: document.querySelectorAll('*').length,
                    inlineCssBytes: Array.from(document.querySelectorAll('style')).reduce((sum, s) => sum + s.textContent.length, 0),
                    googleFontCount: document.querySelectorAll('link[href*=""fonts.googleapis.com""]').length,
                    hasPreconnect: document.querySelectorAll('link[rel=""preconnect""]').length > 0,
                    blockingCssSnippets: Array.from(document.querySelectorAll('head link[rel=""stylesheet""]'))
                        .filter(l => !l.media || l.media === 'all')
                        .map(l => l.outerHTML)
                })
            ");

            var perfData = JsonConvert.DeserializeObject<dynamic>(rawJson);
            int domNodeCount = (int)(perfData?.domNodeCount ?? 0);
            int inlineCssBytes = (int)(perfData?.inlineCssBytes ?? 0);
            int googleFontCount = (int)(perfData?.googleFontCount ?? 0);
            bool hasPreconnect = (bool)(perfData?.hasPreconnect ?? false);
            var blockingCss = perfData?.blockingCssSnippets?.ToObject<List<string>>() ?? new List<string>();

            if (domNodeCount > 1500)
                issues.Add(new AuditIssueDto("perf-large-dom", $"DOM quá lớn ({domNodeCount} nodes)", "Nên < 1500 nodes.", null));

            if (inlineCssBytes > 10000)
                issues.Add(new AuditIssueDto("perf-inline-css", $"Inline CSS quá nhiều ({inlineCssBytes / 1000}KB)", "Nên tách ra file.", null));

            if (googleFontCount > 2)
                issues.Add(new AuditIssueDto("perf-external-fonts", $"Quá nhiều Google Fonts ({googleFontCount})", "Dùng tối đa 2 fonts.", null));

            if (!hasPreconnect && googleFontCount > 0)
                issues.Add(new AuditIssueDto("perf-no-preconnect", "Thiếu preconnect", "Thêm preconnect cho Google Fonts.", null));

            if (blockingCss.Count > 0)
                issues.Add(new AuditIssueDto("perf-css-render-blocking", "CSS chặn render", 
                    $"{blockingCss.Count} CSS đang chặn.", blockingCss));

            return issues;
        }

        #endregion

        #region Accessibility Checks

        public static async Task<List<AuditIssueDto>> CheckAccessibilityAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();

            var rawJson = await page.EvaluateExpressionAsync<string>(@"
                JSON.stringify({
                    emptyButtons: Array.from(document.querySelectorAll('button'))
                        .filter(b => !b.textContent.trim() && !b.getAttribute('aria-label'))
                        .map(b => b.outerHTML),
                    inputsWithoutLabels: Array.from(document.querySelectorAll('input:not([type=""hidden""]):not([type=""submit""]):not([type=""button""])'))
                        .filter(input => {
                            const id = input.id;
                            const hasLabel = id && document.querySelector('label[for=""' + id + '""]');
                            return !hasLabel && !input.getAttribute('aria-label') && !input.placeholder;
                        })
                        .map(i => i.outerHTML),
                    emptyLinks: Array.from(document.querySelectorAll('a'))
                        .filter(a => !a.textContent.trim() && !a.getAttribute('aria-label') && !a.querySelector('img[alt]'))
                        .map(a => a.outerHTML)
                })
            ");

            var a11yData = JsonConvert.DeserializeObject<dynamic>(rawJson);
            var emptyButtons = a11yData?.emptyButtons?.ToObject<List<string>>() ?? new List<string>();
            var inputsWithoutLabels = a11yData?.inputsWithoutLabels?.ToObject<List<string>>() ?? new List<string>();
            var emptyLinks = a11yData?.emptyLinks?.ToObject<List<string>>() ?? new List<string>();

            if (emptyButtons.Count > 0)
                issues.Add(new AuditIssueDto("a11y-missing-button-text", $"Button thiếu text ({emptyButtons.Count})", 
                    "Button cần text hoặc aria-label.", emptyButtons));

            if (inputsWithoutLabels.Count > 0)
                issues.Add(new AuditIssueDto("a11y-missing-form-labels", $"Input thiếu label ({inputsWithoutLabels.Count})", 
                    "Input cần label hoặc aria-label.", inputsWithoutLabels));

            if (emptyLinks.Count > 0)
                issues.Add(new AuditIssueDto("a11y-empty-links", $"Link thiếu text ({emptyLinks.Count})", 
                    "Link cần text mô tả.", emptyLinks));

            return issues;
        }

        #endregion
    }
}
