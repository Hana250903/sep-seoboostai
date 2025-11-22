using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class PaymentHistoryDto
	{
		public int TransactionId { get; set; }
		public decimal Amount { get; set; }
		public string Description { get; set; }
		public string Status { get; set; }
		public DateTime? PaymentDate { get; set; } // Lấy CompletedTime
		public string PaymentMethod { get; set; }
		public string GatewayTransactionId { get; set; } // Mã tham chiếu PayOS
	}
}
