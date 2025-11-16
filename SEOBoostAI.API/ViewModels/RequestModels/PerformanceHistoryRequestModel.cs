namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class PerformanceHistoryRequestModel
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int? UserId { get; set; }
    }
}
