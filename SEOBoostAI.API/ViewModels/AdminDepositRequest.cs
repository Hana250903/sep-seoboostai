namespace SEOBoostAI.API.ViewModels
{
	public class AdminDepositRequest
	{
		public int UserId { get; set; }
		public decimal Money { get; set; }
		public string Description { get; set; }
	}
}
