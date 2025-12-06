using System.ComponentModel.DataAnnotations;

namespace SEOBoostAI.API.ViewModels.RequestModels
{
	public class PaymentLinkRequest
	{
		[Required]
		public int Amount { get; set; }
	}
}
