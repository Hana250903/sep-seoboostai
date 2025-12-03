using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions.GeminiAIModel
{
    public class GeminiAIResponseModel
    {
        [JsonPropertyName("candidates")]
        public Candidate[] Candidates { get; set; }
        [JsonPropertyName("usageMetadata")]
        public UsageMetadata UsageMetadata { get; set; }
    }

    public class Candidate
    {
        [JsonPropertyName("content")]
        public ContentResponse Content { get; set; }
    }

    public class UsageMetadata
    {
        [JsonPropertyName("promptTokenCount")]
        public int PromptTokenCount { get; set; }
        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }
        [JsonPropertyName("totalTokenCount")]
        public int TotalTokenCount { get; set; }
    }

    public class ContentResponse
    {
        [JsonPropertyName("parts")]
        public PartResponse[] Parts { get; set; }
    }

    public class PartResponse
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

	public class AiOptimizationResponse
	{
		[JsonPropertyName("comparison")]
		public ComparisonData Comparison { get; set; }

		[JsonPropertyName("optimized_content")]
		public string OptimizedContent { get; set; }

		[JsonPropertyName("summary")]
		public string Summary { get; set; }
	}

	public class ComparisonData
	{
		[JsonPropertyName("original")]
		public ScoreData Original { get; set; }

		[JsonPropertyName("optimized")]
		public ScoreData Optimized { get; set; }
	}

	public class ScoreData
	{
		[JsonPropertyName("seo_score")]
		public int SeoScore { get; set; }

		[JsonPropertyName("seo_justification")]
		public string SeoJustification { get; set; }

		[JsonPropertyName("readability_score")]
		public int ReadabilityScore { get; set; }

		[JsonPropertyName("readability_justification")]
		public string ReadabilityJustification { get; set; }

		[JsonPropertyName("engagement_score")]
		public int EngagementScore { get; set; }

		[JsonPropertyName("engagement_justification")]
		public string EngagementJustification { get; set; }
	}
}
