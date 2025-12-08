using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
	public class DashboardOverviewDto
	{
		// Tổng doanh thu toàn thời gian
		public decimal TotalRevenue { get; set; }
		// Doanh thu hôm nay
		public decimal TodayRevenue { get; set; }
		// Doanh thu tháng này
		public decimal ThisMonthRevenue { get; set; }
	}

	public class RevenueChartDto
	{
		// Nhãn (Ví dụ: "01/12", "Tuần 1", "Tháng 11")
		public string Label { get; set; }
		// Số tiền
		public decimal Revenue { get; set; }
	}
}
