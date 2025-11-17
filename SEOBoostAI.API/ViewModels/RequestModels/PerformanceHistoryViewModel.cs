namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class PerformanceHistoryViewModel
    {
        public int UserId { get; set; }
        public string Url { get; set; }
        public string Strategy { get; set; }
    }

    public class PerformanceHistoryRequestModel
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int? UserId { get; set; }
    }

    public class PerformanceHistoryUpdateModel
    {
        public int PerformanceHistoryId { get; set; }
        public int UserId { get; set; }
    }
}
