namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class UserRequestModel
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public string? Role { get; set; }
        public bool? IsBanned { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
