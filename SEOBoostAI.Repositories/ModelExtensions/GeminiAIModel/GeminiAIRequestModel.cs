using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions.GeminiAIModel
{
    public class GeminiAIRequestModel
    {
        [JsonPropertyName("contents")]
        public ContentRequest[] Contents { get; set; }

		[JsonPropertyName("generationConfig")]
		public GenerationConfig GenerationConfig { get; set; }

		[JsonPropertyName("safetySettings")]
		public List<SafetySetting> SafetySettings { get; set; }
	}

	public class GenerationConfig
	{
		[JsonPropertyName("response_mime_type")]
		public string ResponseMimeType { get; set; }
	}

	public class ContentRequest
    {
        [JsonPropertyName("parts")]
        public PartRequest[] Parts { get; set; }
    }

    public class PartRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }

	public class SafetySetting
	{
		[JsonPropertyName("category")]
		public string Category { get; set; }

		[JsonPropertyName("threshold")]
		public string Threshold { get; set; }
	}
}
