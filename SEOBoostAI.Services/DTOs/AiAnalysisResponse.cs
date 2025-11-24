using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SEOBoostAI.Service.DTOs
{
    // 1. DTO cho từng dòng đánh giá quảng cáo
    public class AdsEvaluationItem
    {
        [JsonPropertyName("Keyword")]
        public string Keyword { get; set; }

        [JsonPropertyName("IsPotential")]
        public bool IsPotential { get; set; } // True = Nên đầu tư

        [JsonPropertyName("Message")]
        public string Message { get; set; }   // Lý do ngắn gọn
    }

    // 2. DTO tổng cho phản hồi của AI
    public class AiAnalysisResponse
    {
        [JsonPropertyName("Advice")]
        public string Advice { get; set; } // Bài văn tư vấn chiến lược

        [JsonPropertyName("AdsEvaluations")]
        public List<AdsEvaluationItem> AdsEvaluations { get; set; }
    }
}