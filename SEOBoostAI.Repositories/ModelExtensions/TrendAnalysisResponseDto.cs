using System;

namespace SEOBoostAI.Repository.ModelExtensions
{
    public class TrendAnalysisResponseDto
    {
        public int Id { get; set; }                 // ID để FE gọi API xem chi tiết từ khóa
        public string OriginalQuestion { get; set; } // Câu hỏi gốc
        public string FinalAiResponse { get; set; }  // Bài văn tư vấn của AI
        public DateTime? CreatedAt { get; set; }     // Thời gian tạo
    }
}