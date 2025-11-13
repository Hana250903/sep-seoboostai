using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Ultils
{
    public interface ICompareUrlString
    {
        public string NormalizeUrlForComparison(string url);
    }

    public class CompareUrlString : ICompareUrlString
    {
        public string NormalizeUrlForComparison(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return string.Empty;

            // Đảm bảo URI là tuyệt đối (fallback về http)
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (!Uri.TryCreate("http://" + url, UriKind.Absolute, out uri))
                    return url.Trim().ToLowerInvariant(); // Fallback nếu không thể phân tích
            }

            // Scheme và host chuẩn hóa về chữ thường
            var scheme = uri.Scheme.ToLowerInvariant();
            var host = uri.Host.ToLowerInvariant();

            // Bỏ qua port mặc định
            string portPart = "";
            if (!uri.IsDefaultPort)
            {
                // Giữ lại port không mặc định
                portPart = $":{uri.Port}";
            }

            // Chuẩn hóa path: giải mã (decode) rồi cắt bỏ dấu / ở cuối (nhưng giữ lại / nếu là gốc)
            var path = Uri.UnescapeDataString(uri.AbsolutePath ?? string.Empty).TrimEnd('/');
            if (string.IsNullOrEmpty(path))
                path = "/";

            // Chuẩn hóa query: phân tích, giải mã, sắp xếp theo key rồi đến value
            string canonicalQuery = "";
            var rawQuery = uri.Query?.TrimStart('?') ?? "";
            if (!string.IsNullOrEmpty(rawQuery))
            {
                var pairs = rawQuery
                    .Split(new[] { '&' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p =>
                    {
                        var idx = p.IndexOf('=');
                        if (idx >= 0)
                        {
                            var k = Uri.UnescapeDataString(p.Substring(0, idx)).Trim().ToLowerInvariant();
                            var v = Uri.UnescapeDataString(p.Substring(idx + 1)).Trim();
                            return (k, v);
                        }
                        else
                        {
                            var k = Uri.UnescapeDataString(p).Trim().ToLowerInvariant();
                            return (k: k, v: "");
                        }
                    })
                    .OrderBy(kv => kv.k)
                    .ThenBy(kv => kv.v)
                    .ToList();

                canonicalQuery = string.Join("&", pairs.Select(kv =>
                    kv.v == ""
                        ? Uri.EscapeDataString(kv.k)
                        : $"{Uri.EscapeDataString(kv.k)}={Uri.EscapeDataString(kv.v)}"
                ));
            }

            var canonical = new StringBuilder();
            canonical.Append($"{scheme}://{host}{portPart}{path}");
            if (!string.IsNullOrEmpty(canonicalQuery))
                canonical.Append($"?{canonicalQuery}");

            return canonical.ToString();
        }
    }
}