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
            var keyAttrs = new[] { "src", "href", "alt", "width", "height", "loading", "async", "defer", "rel", "aria-label" };

            foreach (var attrName in keyAttrs)
            {
                var val = node.GetAttributeValue(attrName, null);
                if (val != null)
                {
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
    }
}
