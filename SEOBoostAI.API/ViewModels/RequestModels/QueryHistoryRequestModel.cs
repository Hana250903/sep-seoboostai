using System.ComponentModel.DataAnnotations;

namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class QueryHistoryRequestModel
    {
        [Required]
        public int CurrentPage { get; set; } = 1;
        [Required]
        public int PageSize { get; set; } = 10;
    }
}