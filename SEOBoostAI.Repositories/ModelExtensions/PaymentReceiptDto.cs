using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class PaymentReceiptDto
	{
		// 1. Header Info
		public string TransactionCode { get; set; } // Mã GD
		public string Status { get; set; }          // COMPLETED / PENDING
		public DateTime PaymentDate { get; set; }   // Ngày thanh toán

		// 2. Payer Info (Người mua)
		public string PayerName { get; set; }
		public string PayerEmail { get; set; }

		// 3. Payment Method Info
		public string PaymentMethod { get; set; }   // VD: "PayOS (QR Code)"
		public string BankName { get; set; }        // VD: "MB Bank" (Nếu có lưu)

		// 4. Line Items (Chi tiết đơn hàng)
		public string ServiceName { get; set; }     // Tên gói hoặc "Nạp tiền"
		public string Description { get; set; }     // Mô tả phụ
		public decimal Amount { get; set; }         // Số tiền gốc

		// 5. Totals
		public decimal VatRate { get; set; }        // Thuế suất (%)
		public decimal VatAmount { get; set; }      // Tiền thuế
		public decimal TotalAmount { get; set; }    // Tổng cộng
	}
}
