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
}
