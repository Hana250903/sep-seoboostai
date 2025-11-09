using System.ComponentModel.DataAnnotations;

namespace SEOBoostAI.API.ViewModels.RequestModels
{
    public class UpdateSettingRequest
    {
        [Required]
        [StringLength(100)]
        public string Key { get; set; }

        // Cho phép giá trị rỗng, nhưng không cho phép null
        [Required]
        public string Value { get; set; }
    }
}
