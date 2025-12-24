using System.ComponentModel.DataAnnotations;

namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class UpdateSettingRequest
    {
        [Required]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        public int? FeatureID { get; set; }
    }
}
