using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
    public class PageSpeedResponse
    {
        [JsonPropertyName("lighthouseResult")]
        public LighthouseResult? LighthouseResult { get; set; }
    }

    public class LighthouseResult
    {
        [JsonPropertyName("categories")]
        public Categories? Categories { get; set; }

        [JsonPropertyName("audits")]
        public Audits? Audits { get; set; }
    }

    public class Categories
    {
        [JsonPropertyName("performance")]
        public PerformanceCategory? Performance { get; set; }
    }

    public class PerformanceCategory
    {
        // Score là một số thập phân từ 0 đến 1 (ví dụ: 0.95)
        [JsonPropertyName("score")]
        public double? Score { get; set; }
    }

    public class Audits
    {
        [JsonPropertyName("first-contentful-paint")]
        public AuditItem? Fcp { get; set; }

        [JsonPropertyName("largest-contentful-paint")]
        public AuditItem? Lcp { get; set; }

        [JsonPropertyName("cumulative-layout-shift")]
        public AuditItem? Cls { get; set; }

        [JsonPropertyName("total-blocking-time")]
        public AuditItem? Tbt { get; set; }

        [JsonPropertyName("speed-index")]
        public AuditItem? Si { get; set; }

        [JsonPropertyName("interactive")]
        public AuditItem? Tti { get; set; }
    }

    public class AuditItem
    {
        [JsonPropertyName("displayValue")]
        public string? DisplayValue { get; set; }

        [JsonPropertyName("numericValue")]
        public double? NumericValue { get; set; }
    }

    public record PageSpeedMetrics(
        int PerformanceScore,
        double? FCP,
        double? LCP,
        double? CLS,
        double? TBT,
        double? SpeedIndex,
        double? TimeToInteractive
    );
}
