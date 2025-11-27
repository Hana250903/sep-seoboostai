namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class QueryHistoryRequestModel
    {
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int? UserId { get; set; } // Để null nếu muốn lấy từ Token
    }
}