using System.Text.Json.Serialization;

namespace SEOBoostAI.Service.DTOs
{
    // === CÁC LỚP CHUNG ===
    public class SerpApiSearchMetadata
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("status")]
        public string Status { get; set; }
    }

    public class SerpApiSearchParameters
    {
        [JsonPropertyName("q")]
        public string Query { get; set; }
        [JsonPropertyName("geo")]
        public string Geo { get; set; }
        [JsonPropertyName("hl")]
        public string Hl { get; set; }
        [JsonPropertyName("date")]
        public string Date { get; set; }
        [JsonPropertyName("data_type")]
        public string DataType { get; set; }
    }

    // === 1. DTO cho InterestOverTime (Ảnh 2 & 4) ===
    public class SerpTimeValue
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }
        [JsonPropertyName("extracted_value")]
        public int ExtractedValue { get; set; }
    }

    public class SerpTimelineData
    {
        [JsonPropertyName("date")]
        public string Date { get; set; }
        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; }
        [JsonPropertyName("values")]
        public List<SerpTimeValue> Values { get; set; }
    }

    public class SerpInterestOverTimeData
    {
        [JsonPropertyName("timeline_data")]
        public List<SerpTimelineData> TimelineData { get; set; }
    }

    public class SerpInterestOverTimeResponse
    {
        [JsonPropertyName("search_metadata")]
        public SerpApiSearchMetadata SearchMetadata { get; set; }
        [JsonPropertyName("interest_over_time")]
        public SerpInterestOverTimeData InterestOverTime { get; set; }
    }

    // === 2. DTO cho InterestByRegion (Ảnh 3) ===
    public class SerpRegionItem
    {
        [JsonPropertyName("location")]
        public string Location { get; set; }
        [JsonPropertyName("extracted_value")]
        public int ExtractedValue { get; set; }
    }

    public class SerpInterestByRegionResponse
    {
        [JsonPropertyName("search_metadata")]
        public SerpApiSearchMetadata SearchMetadata { get; set; }
        [JsonPropertyName("interest_by_region")]
        public List<SerpRegionItem> InterestByRegion { get; set; }
    }

    // === 3. DTO cho RegionComparison (Ảnh 1) ===
    public class SerpComparisonValue
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }
        [JsonPropertyName("value")]
        public string Value { get; set; } // "52%"
        [JsonPropertyName("extracted_value")]
        public int ExtractedValue { get; set; } // 52
    }

    public class SerpComparisonItem
    {
        [JsonPropertyName("location")]
        public string Location { get; set; }
        [JsonPropertyName("values")]
        public List<SerpComparisonValue> Values { get; set; }
    }

    public class SerpRegionComparisonResponse
    {
        [JsonPropertyName("search_metadata")]
        public SerpApiSearchMetadata SearchMetadata { get; set; }
        [JsonPropertyName("compared_breakdown_by_region")]
        public List<SerpComparisonItem> ComparedBreakdownByRegion { get; set; }
    }

    // === 4. DTO cho RelatedTopics (Ảnh 5) ===
    public class SerpTopicDetails
    {
        [JsonPropertyName("title")]
        public string Title { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }

    public class SerpTopicItem
    {
        [JsonPropertyName("topic")]
        public SerpTopicDetails Topic { get; set; }

        [JsonPropertyName("value")] // <-- THÊM DÒNG NÀY
        public string ValueString { get; set; } // <-- THÊM DÒNG NÀY

        [JsonPropertyName("extracted_value")]
        public int ExtractedValue { get; set; }
    }

    public class SerpRelatedTopicsData
    {
        [JsonPropertyName("rising")]
        public List<SerpTopicItem> Rising { get; set; }
        [JsonPropertyName("top")]
        public List<SerpTopicItem> Top { get; set; }
    }

    public class SerpRelatedTopicsResponse
    {
        [JsonPropertyName("search_metadata")]
        public SerpApiSearchMetadata SearchMetadata { get; set; }
        [JsonPropertyName("related_topics")]
        public SerpRelatedTopicsData RelatedTopics { get; set; }
    }

    // === 5. DTO cho RelatedQueries (Ảnh 6) ===
    public class SerpQueryItem
    {
        [JsonPropertyName("query")]
        public string Query { get; set; }
        [JsonPropertyName("extracted_value")]
        public int ExtractedValue { get; set; }
    }

    public class SerpRelatedQueriesData
    {
        [JsonPropertyName("rising")]
        public List<SerpQueryItem> Rising { get; set; }
        [JsonPropertyName("top")]
        public List<SerpQueryItem> Top { get; set; }
    }

    public class SerpRelatedQueriesResponse
    {
        [JsonPropertyName("search_metadata")]
        public SerpApiSearchMetadata SearchMetadata { get; set; }
        [JsonPropertyName("related_queries")]
        public SerpRelatedQueriesData RelatedQueries { get; set; }
    }
}