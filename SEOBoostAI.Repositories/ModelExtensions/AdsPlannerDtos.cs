using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{

    public class AdsPlannerItemDto
    {
        [JsonPropertyName("Keyword")]
        public string Keyword { get; set; }

        [JsonPropertyName("Avg_Search_Volume")]
        public string AvgSearchVolume { get; set; }

        [JsonPropertyName("Competition")]
        public string Competition { get; set; }

        [JsonPropertyName("Low_Bid")]
        public string? LowBid { get; set; }

        [JsonPropertyName("High_Bid")]
        public string? HighBid { get; set; }

        [JsonPropertyName("AiSuggestion")]
        public bool AiSuggestion { get; set; }

        [JsonPropertyName("AiMessage")]
        public string AiMessage { get; set; }
    }

    public class AdsPlannerResponseDto
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        public List<AdsPlannerItemDto> Data { get; set; }
    }


}
