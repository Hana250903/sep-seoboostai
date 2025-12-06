using System.ComponentModel.DataAnnotations;

namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class PerformanceHistoryRequestModel
    {
        [Required]
        public string Url { get; set; }
        [Required]
        public string Strategy { get; set; }
        [Required]
        public int FeatureId { get; set; }
    }

    public class PerformanceHistoryGetAllRequestModel
    {
        [Required]
        public int CurrentPage { get; set; }
        [Required]
        public int PageSize { get; set; }
    }

    public class PerformanceHistoryUpdateModel
    {
        [Required]
        public int PerformanceHistoryId { get; set; }
        [Required]
        public int FeatureId { get; set; }
    }
}
