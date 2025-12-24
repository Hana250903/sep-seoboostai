using System.ComponentModel.DataAnnotations;

namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class UserRequestModel
    {
        [Required]
        public int CurrentPage { get; set; }
        [Required]
        public int PageSize { get; set; }
        public string? Role { get; set; }
        public bool? IsBanned { get; set; }
        public bool? IsDeleted { get; set; }
    }
}
