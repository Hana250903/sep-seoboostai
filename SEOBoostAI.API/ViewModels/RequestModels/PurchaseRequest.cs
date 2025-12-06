using System.ComponentModel.DataAnnotations;

namespace SEOBoostAI.API.ViewModels.RequestModels
{
	public class PurchaseRequest
	{
		[Required]
		public int FeatureId { get; set; }
		[Required]
        public int Quantity { get; set; }
	}
}
