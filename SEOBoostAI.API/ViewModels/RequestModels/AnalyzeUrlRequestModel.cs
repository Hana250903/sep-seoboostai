using System.ComponentModel.DataAnnotations;

namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class AnalyzeUrlRequestModel
    {
        [Required]
        public string Url { get; set; }
        [Required]
        public string Strategy { get; set; }
    }
}
