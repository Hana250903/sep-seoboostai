using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.ViewModels
{
	public class AiOptimizationResponse
	{
		[JsonPropertyName("optimized_content")]
		public string OptimizedContent { get; set; }

		[JsonPropertyName("seo_score")]
		public int SeoScore { get; set; }

		[JsonPropertyName("readability")]
		public int Readability { get; set; }

		[JsonPropertyName("engagement")]
		public int Engagement { get; set; }

		[JsonPropertyName("originality")]
		public int Originality { get; set; }
	}
}
