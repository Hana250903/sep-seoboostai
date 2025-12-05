using HtmlAgilityPack;
using SEOBoostAI.Repository.ModelExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Helpers
{
    public class CheckElementHelper
    {
        public static List<ElementFinding> CheckLCP(HtmlDocument htmlDoc)
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

        public static List<ElementFinding> CheckCLS(HtmlDocument htmlDoc)
        {
            var findings = new List<ElementFinding>();

            var allImages = htmlDoc.DocumentNode.SelectNodes("//img");
            if (allImages != null)
            {
                var imageClsCandidates = allImages.Where(img =>
                {
                    bool hasHtmlWidth = img.Attributes["width"] != null;
                    bool hasHtmlHeight = img.Attributes["height"] != null;
                    if (hasHtmlWidth && hasHtmlHeight) return false;//Okay nếu có cả 2 thuộc tính width và height

                    var styleAttribute = img.Attributes["style"];
                    if (styleAttribute != null)
                    {
                        string styleValue = styleAttribute.Value;
                        bool hasInlineWidth = styleValue.Contains("width:");
                        bool hasInlineHeight = styleValue.Contains("height:");
                        bool hasAspectRatio = styleValue.Contains("aspect-ratio:");
                        if ((hasInlineWidth && hasInlineHeight) || hasAspectRatio) return false;//Okay nếu có cả 2 thuộc tính width và height trong style hoặc có aspect-ratio
                    }
                    //Nếu không có thuộc tính width hoặc height nào, hoặc không có aspect-ratio, thì đây là ứng viên CLS
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

        public static List<ElementFinding> CheckMissingAltText(HtmlDocument htmlDoc)
        {
            var findings = new List<ElementFinding>();

            // Lấy tất cả ảnh không có alt hoặc alt rỗng
            var images = htmlDoc.DocumentNode.SelectNodes("//img[not(@alt) or @alt='']");

            if (images != null)
            {
                foreach (var img in images)
                {
                    // Bỏ qua icon trang trí (thường có role="presentation" hoặc kích thước nhỏ)
                    if (img.GetAttributeValue("role", "") == "presentation") continue;

                    findings.Add(new ElementFinding
                    {
                        TagName = "img",
                        OuterHtml = img.OuterHtml,
                        InnerHtml = "Missing Alt Text", // Đánh dấu lỗi
                    });
                }
            }
            return findings;
        }
        public static List<ElementFinding> CheckHeadingStructure(HtmlDocument htmlDoc)
        {
            var findings = new List<ElementFinding>();

            var h1 = htmlDoc.DocumentNode.SelectNodes("//h1");
            if (h1 == null || h1.Count == 0)
            {
                findings.Add(new ElementFinding
                {
                    TagName = "h1",
                    InnerHtml = "Missing H1 Tag",
                    OuterHtml = "<h1>N/A</h1>" // Gán giá trị giả để không bị null
                });
            }
            else if (h1.Count > 1)
            {
                // Gom tất cả các thẻ H1 lại thành một chuỗi string để lưu vào OuterHtml
                string combinedH1Html = string.Join("\n", h1.Select(node => node.OuterHtml));
                findings.Add(new ElementFinding
                {
                    TagName = "h1",
                    InnerHtml = "Multiple H1 Tags found (Should imply only one)",
                    OuterHtml = combinedH1Html
                });
            }

            // Kiểm tra các thẻ H rỗng (SEO spam hoặc lỗi code)
            var emptyHeadings = htmlDoc.DocumentNode.SelectNodes("//h1[not(normalize-space())] | //h2[not(normalize-space())] | //h3[not(normalize-space())]");
            if (emptyHeadings != null)
            {
                foreach (var h in emptyHeadings)
                {
                    findings.Add(new ElementFinding { TagName = h.Name, OuterHtml = h.OuterHtml, InnerHtml = "Empty Heading" });
                }
            }

            return findings;
        }

        public static List<ElementFinding> CheckFCP(HtmlDocument htmlDoc)
        {
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

        public static List<ElementFinding> FindThirdPartyScripts(HtmlDocument htmlDoc, string originalUrl)
        {
            var findings = new List<ElementFinding>();

            // Lấy tên miền "first-party"
            Uri baseUri;
            if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out baseUri))
            {
                return null;
            }

            string firstPartyHost = GetRootDomain(baseUri.Host);

            // Lấy tất cả các thẻ <script> có thuộc tính [src]
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

                // Xử lý các URL tương đối (ví dụ: /js/app.js)
                // Nếu không phải là URL tuyệt đối, nó là first-party
                if (!src.StartsWith("http://") && !src.StartsWith("https://") && !src.StartsWith("//"))
                {
                    // Đây là script first-party (ví dụ: /_framework/blazor.web.js)
                    continue;
                }

                // Phân tích tên miền của script
                Uri scriptUri;
                // Xử lý URL bắt đầu bằng // (ví dụ: //cdn.example.com)
                if (src.StartsWith("//"))
                {
                    src = "https:" + src;
                }

                if (Uri.TryCreate(src, UriKind.Absolute, out scriptUri))
                {
                    string scriptHost = GetRootDomain(scriptUri.Host);

                    // So sánh tên miền
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

        private static string GetRootDomain(string host)
        {
            if (host.StartsWith("www."))
            {
                host = host.Substring(4);
            }
            return host;
        }
    }
}
