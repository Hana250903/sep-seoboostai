using System.Collections.Generic;

namespace SEOBoostAI.Repository.ModelExtensions
{
    /// <summary>
    /// DTO để chứa kết quả phân tích từ AI
    /// </summary>
    public class MetaDataAnalysisResult
    {
        public string GeneralAssessment { get; set; }
        public List<MetaTagSuggestion> Suggestions { get; set; } = new List<MetaTagSuggestion>();
    }

    /// <summary>
    /// DTO cho từng suggestion về meta tag
    /// </summary>
    public class MetaTagSuggestion
    {
        public string TagName { get; set; }
        public string CurrentValue { get; set; }
        public string Issue { get; set; }
        public string Recommendation { get; set; }
        public bool IsImportant { get; set; }
    }
}
