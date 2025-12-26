using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class UserQuotaDto
	{
		public int FeatureId { get; set; }
		public string FeatureName { get; set; }

		// Thông tin gói Free (Hàng tháng)
		public int FreeUsage { get; set; }      // Đã dùng
		public int FreeLimit { get; set; }      // Tổng giới hạn
        public int FreeRemaining => Math.Max(0, FreeLimit - FreeUsage);

        // Thông tin gói Paid (Mua thêm)
        public int PaidRemaining { get; set; }  // Còn lại bao nhiêu lượt mua

		// Tổng cộng
		public int TotalRemaining => FreeRemaining + PaidRemaining;
	}
}
