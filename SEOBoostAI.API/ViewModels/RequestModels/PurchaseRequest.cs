namespace SEOBoostAI.API.ViewModels.RequestModels
{
	public class PurchaseRequest
	{
		public int UserId { get; set; }
		public int FeatureId { get; set; } // ID của tính năng muốn mua (VD: 1 = AI Content)
		public int Quantity { get; set; }  // Số lượng muốn mua (VD: 10 lượt)
	}
}
