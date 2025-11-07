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
}
