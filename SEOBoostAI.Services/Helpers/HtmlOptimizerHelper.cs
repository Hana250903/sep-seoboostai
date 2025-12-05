using HtmlAgilityPack;
using SEOBoostAI.Repository.ModelExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Helpers
{
    public class HtmlOptimizerHelper
    {
        public static ElementRequest OptimizeForAi(int id, string tagName, string outerHtml)
        {
            var request = new ElementRequest
            {
                ElementID = id,
                TagName = tagName,
                Attributes = new Dictionary<string, string>()
            };

            var doc = new HtmlDocument();
            doc.LoadHtml(outerHtml);
            var node = doc.DocumentNode.FirstChild;

            if (node == null) return request;

            // Lấy những thứ ảnh hưởng đến SEO và Performance
            var keyAttrs = new[] {
                "src", "href", "alt", "width", "height",
                "loading", "async", "defer", "rel", "aria-label",
                "style", "class", "id", "title", "role"
            };

            foreach (var attrName in keyAttrs)
            {
                var val = node.GetAttributeValue(attrName, null);
                if (val != null)
                {
                    // Nếu class quá dài (ví dụ TailwindCSS), cắt bớt để tiết kiệm token
                    if (attrName == "class" && val.Length > 100)
                        val = val.Substring(0, 100) + "...";

                    // Nếu là base64 quá dài, cắt bớt để tiết kiệm token
                    if (attrName == "src" && val.StartsWith("data:image") && val.Length > 50)
                        val = "[Base64 Image Truncated]";

                    request.Attributes.Add(attrName, val);
                }
            }

            string textContent = node.InnerText.Trim();
            if (!string.IsNullOrEmpty(textContent))
            {
                request.Context = textContent.Length > 50
                    ? textContent.Substring(0, 50) + "..."
                    : textContent;
            }
            else if (tagName == "img")
            {
                request.Context = "Image Element";
            }
            else if (tagName == "script")
            {
                request.Context = "Script Resource";
            }

            return request;
        }

        /// <summary>
        /// Extract toàn bộ metadata từ HTML document để phân tích SEO
        /// </summary>
        /// <param name="htmlContent">HTML content đầy đủ của trang</param>
        /// <returns>MetaDataInfo chứa tất cả metadata</returns>
        public static MetaDataInfo ExtractMetaData(string htmlContent)
        {
            var metaData = new MetaDataInfo();

            if (string.IsNullOrWhiteSpace(htmlContent))
                return metaData;

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            // 1. Extract Title Tag
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null)
            {
                metaData.Title = titleNode.InnerText?.Trim();
            }

            // 2. Extract Meta Tags
            var metaTags = doc.DocumentNode.SelectNodes("//meta");
            if (metaTags != null)
            {
                foreach (var metaTag in metaTags)
                {
                    var name = metaTag.GetAttributeValue("name", "").ToLower();
                    var property = metaTag.GetAttributeValue("property", "").ToLower();
                    var content = metaTag.GetAttributeValue("content", "");
                    var charset = metaTag.GetAttributeValue("charset", "");
                    var httpEquiv = metaTag.GetAttributeValue("http-equiv", "").ToLower();

                    // Basic Meta Tags
                    if (name == "description")
                        metaData.Description = content;
                    else if (name == "keywords")
                        metaData.Keywords = content;
                    else if (name == "viewport")
                        metaData.Viewport = content;
                    else if (name == "robots")
                        metaData.Robots = content;
                    else if (!string.IsNullOrEmpty(charset))
                        metaData.Charset = charset;
                    else if (httpEquiv == "content-type" && content.Contains("charset"))
                    {
                        // Extract charset from content-type meta tag
                        var charsetMatch = System.Text.RegularExpressions.Regex.Match(content, @"charset=([^;]+)");
                        if (charsetMatch.Success && string.IsNullOrEmpty(metaData.Charset))
                            metaData.Charset = charsetMatch.Groups[1].Value.Trim();
                    }
                    // Open Graph Tags (og:*)
                    else if (property.StartsWith("og:"))
                    {
                        metaData.OpenGraph[property] = content;
                    }
                    // Twitter Card Tags (twitter:*)
                    else if (name.StartsWith("twitter:"))
                    {
                        metaData.TwitterCard[name] = content;
                    }
                    // Other Important Meta Tags
                    else if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(content))
                    {
                        metaData.OtherMeta[name] = content;
                    }
                }
            }

            // 3. Extract Canonical URL
            var canonicalNode = doc.DocumentNode.SelectSingleNode("//link[@rel='canonical']");
            if (canonicalNode != null)
            {
                metaData.Canonical = canonicalNode.GetAttributeValue("href", "");
            }

            // 4. Extract Charset from HTML tag if not found yet
            if (string.IsNullOrEmpty(metaData.Charset))
            {
                var htmlNode = doc.DocumentNode.SelectSingleNode("//html");
                if (htmlNode != null)
                {
                    var lang = htmlNode.GetAttributeValue("lang", "");
                    if (!string.IsNullOrEmpty(lang))
                    {
                        metaData.OtherMeta["lang"] = lang;
                    }
                }
            }

            return metaData;
        }
    }
}
