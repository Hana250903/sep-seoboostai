using Newtonsoft.Json;
using PuppeteerSharp;
using SEOBoostAI.Repository.ModelExtensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    public static class PageAuditChecks
    {
        // Check an toàn chung để tránh lặp code
        private static bool IsPageInvalid(IPage page) => page == null || page.IsClosed;

        #region Metadata Checks

        public static async Task<List<AuditIssueDto>> CheckMetadataAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();
            if (IsPageInvalid(page)) return issues;

            try
            {
                // Sử dụng nháy đơn (') trong JS để code C# sạch hơn, không cần escape ""
                var rawJson = await page.EvaluateFunctionAsync<string>(@"() => {
                    return JSON.stringify({
                        title: document.title || '',
                        description: document.querySelector(""meta[name='description']"")?.content || '',
                        viewport: document.querySelector(""meta[name='viewport']"")?.content || ''
                    });
                }");

                var metaData = JsonConvert.DeserializeObject<dynamic>(rawJson);
                string title = (string)metaData?.title ?? "";
                string description = (string)metaData?.description ?? "";
                string viewport = (string)metaData?.viewport ?? "";

                if (string.IsNullOrEmpty(title))
                    issues.Add(new AuditIssueDto("meta-missing-title", "Thiếu thẻ Title", "Trang web không có tiêu đề.", null));

                if (string.IsNullOrEmpty(description))
                    issues.Add(new AuditIssueDto("meta-missing-desc", "Thiếu Meta Description", "Chưa có mô tả trang.", null));

                if (string.IsNullOrEmpty(viewport) || !viewport.Contains("width=device-width"))
                    issues.Add(new AuditIssueDto("meta-viewport-invalid", "Cấu hình Viewport chưa chuẩn", "Thẻ viewport chưa tối ưu cho mobile.", null));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CheckMetadata] Error: {ex.Message}");
            }

            return issues;
        }

        #endregion

        #region Image Checks

        private const int MAX_EVIDENCE_COUNT = 10;

        public static async Task<List<AuditIssueDto>> CheckImagesAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();
            if (IsPageInvalid(page)) return issues;

            try
            {
                // Timeout ngắn hơn để không block luồng lâu nếu không có ảnh
                try { await page.WaitForSelectorAsync("img", new WaitForSelectorOptions { Timeout = 2000 }); } catch { }

                var rawJson = await page.EvaluateFunctionAsync<string>(@"() => {
                    const images = Array.from(document.querySelectorAll('img'));
                    // Lấy tối đa 50 ảnh để check
                    return JSON.stringify(
                        images.slice(0, 50).map(img => ({
                            alt: img.alt || '',
                            loading: img.getAttribute('loading') || '',
                            snippet: img.outerHTML.substring(0, 250)
                        }))
                    );
                }");

                var images = JsonConvert.DeserializeObject<List<dynamic>>(rawJson) ?? new List<dynamic>();
                var missingAltEvidence = new List<string>();
                var missingLazyEvidence = new List<string>();

                foreach (var img in images)
                {
                    string alt = (string)img?.alt ?? "";
                    string loading = (string)img?.loading ?? "";
                    string snippet = (string)img?.snippet ?? "";

                    if (string.IsNullOrEmpty(alt))
                        if (missingAltEvidence.Count < MAX_EVIDENCE_COUNT) missingAltEvidence.Add(snippet);

                    if (loading != "lazy")
                        if (missingLazyEvidence.Count < MAX_EVIDENCE_COUNT) missingLazyEvidence.Add(snippet);
                }

                if (missingAltEvidence.Count > 0)
                    issues.Add(new AuditIssueDto("img-missing-alt", "Hình ảnh thiếu thẻ Alt",
                        $"Tìm thấy {missingAltEvidence.Count}+ hình ảnh thiếu alt.", missingAltEvidence));

                // Logic cũ check lazy load cho TẤT CẢ ảnh là chưa chuẩn (ảnh above-the-fold không nên lazy).
                // Tuy nhiên giữ nguyên theo logic bạn yêu cầu.
                if (missingLazyEvidence.Count > 0)
                    issues.Add(new AuditIssueDto("img-missing-lazy", "Hình ảnh thiếu Lazy Load",
                        $"Tìm thấy {missingLazyEvidence.Count}+ hình ảnh thiếu loading=\"lazy\".", missingLazyEvidence));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CheckImages] Error: {ex.Message}");
            }

            return issues;
        }

        #endregion

        #region Script Checks

        public static async Task<List<AuditIssueDto>> CheckScriptsAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();
            if (IsPageInvalid(page)) return issues;

            try
            {
                var rawJson = await page.EvaluateFunctionAsync<string>(@"() => {
                    return JSON.stringify(
                        Array.from(document.querySelectorAll('head script[src]'))
                            .filter(s => !s.hasAttribute('async') && !s.hasAttribute('defer'))
                            .map(s => s.outerHTML)
                    );
                }");

                var blockingScripts = JsonConvert.DeserializeObject<List<string>>(rawJson) ?? new List<string>();

                if (blockingScripts.Count > 0)
                {
                    issues.Add(new AuditIssueDto("js-render-blocking", "JavaScript chặn hiển thị",
                        $"{blockingScripts.Count} script trong <head> đang chặn render.", blockingScripts));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CheckScripts] Error: {ex.Message}");
            }

            return issues;
        }

        #endregion

        #region SEO Checks

        public static async Task<List<AuditIssueDto>> CheckSeoAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();
            if (IsPageInvalid(page)) return issues;

            try
            {
                var rawJson = await page.EvaluateFunctionAsync<string>(@"() => {
                    return JSON.stringify({
                        h1Count: document.querySelectorAll('h1').length,
                        h1Texts: Array.from(document.querySelectorAll('h1')).map(h => h.textContent.trim()),
                        hasOgTitle: !!document.querySelector(""meta[property='og:title']""),
                        hasOgDesc: !!document.querySelector(""meta[property='og:description']""),
                        hasOgImage: !!document.querySelector(""meta[property='og:image']""),
                        hasCanonical: !!document.querySelector(""link[rel='canonical']""),
                        htmlLang: document.documentElement.lang || ''
                    });
                }");

                var seoData = JsonConvert.DeserializeObject<dynamic>(rawJson);
                int h1Count = (int)(seoData?.h1Count ?? 0);
                var h1Texts = seoData?.h1Texts?.ToObject<List<string>>() ?? new List<string>();
                bool hasOgTitle = (bool)(seoData?.hasOgTitle ?? false);
                bool hasOgDesc = (bool)(seoData?.hasOgDesc ?? false);
                bool hasOgImage = (bool)(seoData?.hasOgImage ?? false);
                bool hasCanonical = (bool)(seoData?.hasCanonical ?? false);
                string htmlLang = (string)seoData?.htmlLang ?? "";

                if (h1Count == 0)
                    issues.Add(new AuditIssueDto("seo-missing-h1", "Thiếu thẻ H1", "Trang web cần có 1 thẻ H1.", null));
                else if (h1Count > 1)
                    issues.Add(new AuditIssueDto("seo-multiple-h1", $"Có {h1Count} thẻ H1", "Trang chỉ nên có 1 H1 duy nhất.", h1Texts));

                List<string> missingOg = new List<string>();
                if (!hasOgTitle) missingOg.Add("og:title");
                if (!hasOgDesc) missingOg.Add("og:description");
                if (!hasOgImage) missingOg.Add("og:image");

                if (missingOg.Count > 0)
                    issues.Add(new AuditIssueDto("seo-missing-og-tags", "Thiếu Open Graph tags", $"Thiếu: {string.Join(", ", missingOg)}.", missingOg));

                if (!hasCanonical)
                    issues.Add(new AuditIssueDto("seo-missing-canonical", "Thiếu Canonical URL", "Cần thêm link canonical để tránh trùng lặp nội dung.", null));

                if (string.IsNullOrEmpty(htmlLang))
                    issues.Add(new AuditIssueDto("seo-missing-lang", "Thẻ HTML thiếu lang", "Thêm lang=\"vi\" hoặc \"en\" vào thẻ <html>.", null));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CheckSeo] Error: {ex.Message}");
            }

            return issues;
        }

        #endregion

        #region Performance Checks

        public static async Task<List<AuditIssueDto>> CheckPerformanceAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();
            if (IsPageInvalid(page)) return issues;

            try
            {
                // ĐÃ FIX: Xóa phần tính toán heavyCssElements gây lag
                // ĐÃ FIX: Dùng single quote (') cho selector JS để tránh lỗi escape
                var rawJson = await page.EvaluateFunctionAsync<string>(@"() => {
                    const allImages = Array.from(document.querySelectorAll('img'));
                    
                    // Tìm large images (ảnh hưởng LCP)
                    const largeImages = allImages
                        .filter(img => {
                            const rect = img.getBoundingClientRect();
                            return rect.width > 500 || rect.height > 300;
                        })
                        .map(img => ({
                            src: img.src,
                            width: img.naturalWidth,
                            height: img.naturalHeight,
                            hasExplicitDimensions: img.hasAttribute('width') && img.hasAttribute('height'),
                            snippet: img.outerHTML.substring(0, 250)
                        }));

                    // Tìm images không có explicit dimensions (gây CLS)
                    const imagesWithoutDimensions = allImages
                        .filter(img => !img.hasAttribute('width') || !img.hasAttribute('height'))
                        .map(img => img.outerHTML.substring(0, 250));

                    // Check preload (Fix syntax lỗi cũ)
                    const preloadedImages = Array.from(document.querySelectorAll(""link[rel='preload'][as='image']""))
                        .map(l => l.href);
                    
                    const unpreloadedLargeImages = largeImages
                        .filter(img => !preloadedImages.includes(img.src))
                        .map(img => img.snippet);

                    // Check inline scripts
                    const inlineScripts = Array.from(document.querySelectorAll('head script:not([src])'))
                        .filter(s => s.textContent.length > 200) // Tăng ngưỡng lên 200 ký tự
                        .map(s => s.outerHTML.substring(0, 200));

                    // Check oversized images
                    const oversizedImages = allImages
                        .filter(img => {
                            const estimatedSizeKB = (img.naturalWidth * img.naturalHeight * 0.5) / 1024; 
                            return estimatedSizeKB > 500;
                        })
                        .map(img => img.outerHTML.substring(0, 250));

                    // Check fonts
                    const fontLinks = Array.from(document.querySelectorAll(""link[href*='fonts.googleapis.com']""));
                    let totalFontWeights = 0;
                    fontLinks.forEach(link => {
                        const matches = link.href.match(/wght@[\d;]+/g);
                        if (matches) {
                            matches.forEach(m => {
                                totalFontWeights += m.split(';').length;
                            });
                        }
                    });

                    return JSON.stringify({
                        domNodeCount: document.querySelectorAll('*').length,
                        inlineCssBytes: Array.from(document.querySelectorAll('style')).reduce((sum, s) => sum + s.textContent.length, 0),
                        googleFontCount: fontLinks.length,
                        hasPreconnect: document.querySelectorAll(""link[rel='preconnect']"").length > 0,
                        largeImages: largeImages,
                        imagesWithoutDimensions: imagesWithoutDimensions.slice(0, 10),
                        totalImages: allImages.length,
                        unpreloadedLargeImages: unpreloadedLargeImages.slice(0, 5),
                        inlineScripts: inlineScripts.slice(0, 5),
                        oversizedImages: oversizedImages.slice(0, 5),
                        totalFontWeights: totalFontWeights
                    });
                }");

                var perfData = JsonConvert.DeserializeObject<dynamic>(rawJson);

                int domNodeCount = (int)(perfData?.domNodeCount ?? 0);
                int inlineCssBytes = (int)(perfData?.inlineCssBytes ?? 0);
                int googleFontCount = (int)(perfData?.googleFontCount ?? 0);
                bool hasPreconnect = (bool)(perfData?.hasPreconnect ?? false);
                int totalImages = (int)(perfData?.totalImages ?? 0);
                int totalFontWeights = (int)(perfData?.totalFontWeights ?? 0);

                var largeImages = perfData?.largeImages?.ToObject<List<dynamic>>() ?? new List<dynamic>();
                var imagesWithoutDimensions = perfData?.imagesWithoutDimensions?.ToObject<List<string>>() ?? new List<string>();
                var unpreloadedLargeImages = perfData?.unpreloadedLargeImages?.ToObject<List<string>>() ?? new List<string>();
                var inlineScripts = perfData?.inlineScripts?.ToObject<List<string>>() ?? new List<string>();
                var oversizedImages = perfData?.oversizedImages?.ToObject<List<string>>() ?? new List<string>();

                var lcpEvidence = new List<string>();
                var lcpLazyEvidence = new List<string>();

                foreach (var img in largeImages)
                {
                    bool hasExplicit = (bool)(img?.hasExplicitDimensions ?? true);
                    string snippet = (string)img?.snippet ?? "";

                    if (!hasExplicit && lcpEvidence.Count < 5)
                        lcpEvidence.Add(snippet);

                    if ((snippet.Contains("loading=\"lazy\"") || snippet.Contains("loading='lazy'")) && lcpLazyEvidence.Count < 5)
                        lcpLazyEvidence.Add(snippet);
                }

                if (lcpEvidence.Count > 0)
                    issues.Add(new AuditIssueDto("perf-lcp-large-images", $"Ảnh lớn thiếu kích thước cụ thể ({lcpEvidence.Count})", "Thêm width/height để tối ưu LCP.", lcpEvidence));

                if (lcpLazyEvidence.Count > 0)
                    issues.Add(new AuditIssueDto("perf-lcp-lazy-loading", $"Ảnh LCP có lazy loading ({lcpLazyEvidence.Count})", "Xóa loading='lazy' ở ảnh đầu trang.", lcpLazyEvidence));

                if (imagesWithoutDimensions.Count > 0)
                    issues.Add(new AuditIssueDto("perf-cls-images-no-dimensions", $"Ảnh thiếu width/height ({imagesWithoutDimensions.Count})", "Thêm kích thước để tránh CLS.", imagesWithoutDimensions));

                if (domNodeCount > 1500)
                    issues.Add(new AuditIssueDto("perf-large-dom", $"DOM quá lớn ({domNodeCount} nodes)", "Nên giữ dưới 1500 nodes.", null));

                if (inlineCssBytes > 10000)
                    issues.Add(new AuditIssueDto("perf-inline-css", $"Inline CSS quá nhiều ({inlineCssBytes / 1024}KB)", "Nên tách ra file CSS riêng.", null));

                if (googleFontCount > 2)
                    issues.Add(new AuditIssueDto("perf-external-fonts", $"Quá nhiều Google Fonts ({googleFontCount})", "Chỉ nên dùng tối đa 2 fonts.", null));

                if (!hasPreconnect && googleFontCount > 0)
                    issues.Add(new AuditIssueDto("perf-no-preconnect", "Thiếu preconnect", "Thêm <link rel='preconnect'> cho Google Fonts.", null));

                if (totalImages > 20) // Nâng ngưỡng lên 20, 3 ảnh là quá ít với web hiện đại
                    issues.Add(new AuditIssueDto("perf-too-many-images", $"Nhiều ảnh trên trang ({totalImages})", "Hãy chắc chắn đã bật lazy load.", null));

                if (unpreloadedLargeImages.Count > 0)
                    issues.Add(new AuditIssueDto("perf-lcp-no-preload", "Ảnh LCP chưa được preload", "Thêm <link rel='preload' as='image'>.", unpreloadedLargeImages));

                if (inlineScripts.Count > 0)
                    issues.Add(new AuditIssueDto("perf-inline-scripts", "Inline scripts lớn trong head", "Di chuyển ra file ngoài để giảm TBT.", inlineScripts));

                if (oversizedImages.Count > 0)
                    issues.Add(new AuditIssueDto("perf-oversized-images", "Ảnh kích thước quá lớn", "Nén ảnh hoặc dùng WebP/AVIF.", oversizedImages));

                if (totalFontWeights > 6)
                    issues.Add(new AuditIssueDto("perf-too-many-font-weights", $"Quá nhiều font weights ({totalFontWeights})", "Chỉ load các weight thực sự cần thiết.", null));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CheckPerformance] Error: {ex.Message}");
            }

            return issues;
        }

        #endregion

        #region Accessibility Checks

        public static async Task<List<AuditIssueDto>> CheckAccessibilityAsync(IPage page)
        {
            var issues = new List<AuditIssueDto>();
            if (IsPageInvalid(page)) return issues;

            try
            {
                var rawJson = await page.EvaluateFunctionAsync<string>(@"() => {
                    return JSON.stringify({
                        emptyButtons: Array.from(document.querySelectorAll('button'))
                            .filter(b => !b.textContent.trim() && !b.getAttribute('aria-label'))
                            .map(b => b.outerHTML),
                        
                        // ĐÃ FIX: Thêm check input.closest('label')
                        inputsWithoutLabels: Array.from(document.querySelectorAll('input:not([type=""hidden""]):not([type=""submit""]):not([type=""button""])'))
                            .filter(input => {
                                const id = input.id;
                                const hasExplicitLabel = id && document.querySelector(""label[for='"" + id + ""']"");
                                const hasImplicitLabel = input.closest('label');
                                return !hasExplicitLabel && !hasImplicitLabel && !input.getAttribute('aria-label') && !input.placeholder;
                            })
                            .map(i => i.outerHTML),

                        emptyLinks: Array.from(document.querySelectorAll('a'))
                            .filter(a => !a.textContent.trim() && !a.getAttribute('aria-label') && !a.querySelector('img[alt]'))
                            .map(a => a.outerHTML)
                    });
                }");

                var a11yData = JsonConvert.DeserializeObject<dynamic>(rawJson);
                var emptyButtons = a11yData?.emptyButtons?.ToObject<List<string>>() ?? new List<string>();
                var inputsWithoutLabels = a11yData?.inputsWithoutLabels?.ToObject<List<string>>() ?? new List<string>();
                var emptyLinks = a11yData?.emptyLinks?.ToObject<List<string>>() ?? new List<string>();

                if (emptyButtons.Count > 0)
                    issues.Add(new AuditIssueDto("a11y-missing-button-text", $"Button thiếu text ({emptyButtons.Count})", "Button cần text hoặc aria-label.", emptyButtons));

                if (inputsWithoutLabels.Count > 0)
                    issues.Add(new AuditIssueDto("a11y-missing-form-labels", $"Input thiếu label ({inputsWithoutLabels.Count})", "Input cần label (for hoặc bao ngoài) hoặc aria-label.", inputsWithoutLabels));

                if (emptyLinks.Count > 0)
                    issues.Add(new AuditIssueDto("a11y-empty-links", $"Link thiếu text ({emptyLinks.Count})", "Link cần text mô tả đích đến.", emptyLinks));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CheckAccessibility] Error: {ex.Message}");
            }

            return issues;
        }

        #endregion
    }
}