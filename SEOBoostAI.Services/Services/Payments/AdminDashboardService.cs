using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Payments
{
	public class AdminDashboardService : IAdminDashboardService
	{
		private readonly ITransactionRepository _transactionRepository;
		private readonly IUserRepository _userRepository; // Để đếm User

		public AdminDashboardService(ITransactionRepository transactionRepository, IUserRepository userRepository)
		{
			_transactionRepository = transactionRepository;
			_userRepository = userRepository;
		}

		public async Task<DashboardOverviewDto> GetOverviewAsync()
		{
			var now = DateTime.UtcNow;
			var startOfToday = now.Date;
			var startOfMonth = new DateTime(now.Year, now.Month, 1);

			return new DashboardOverviewDto
			{
				TotalRevenue = await _transactionRepository.GetTotalRevenueAsync(null, null),
				TodayRevenue = await _transactionRepository.GetTotalRevenueAsync(startOfToday, now),
				ThisMonthRevenue = await _transactionRepository.GetTotalRevenueAsync(startOfMonth, now)
			};
		}

		public async Task<List<RevenueChartDto>> GetRevenueChartAsync(string type)
		{
			var now = DateTime.UtcNow;
			DateTime fromDate, toDate = now;
			List<RevenueChartDto> data;

			// Xử lý logic lọc thời gian
			switch (type.ToLower())
			{
				case "week": // 7 ngày gần nhất
					fromDate = now.AddDays(-6).Date;
					data = await _transactionRepository.GetRevenueChartDataAsync(fromDate, toDate);
					break;

				case "month": // Từ đầu tháng đến nay
					fromDate = new DateTime(now.Year, now.Month, 1);
					data = await _transactionRepository.GetRevenueChartDataAsync(fromDate, toDate);
					break;

				case "year": // 12 tháng (Logic GroupBy sẽ khác, cần xử lý riêng nếu muốn)
							 // Demo đơn giản cho week/month trước
					fromDate = now.AddMonths(-1);
					data = await _transactionRepository.GetRevenueChartDataAsync(fromDate, toDate);
					break;

				default: // Mặc định 7 ngày
					fromDate = now.AddDays(-6).Date;
					data = await _transactionRepository.GetRevenueChartDataAsync(fromDate, toDate);
					break;
			}

			// LẤP ĐẦY DỮ LIỆU TRỐNG (Rất quan trọng cho biểu đồ đẹp)
			// Nếu ngày đó không có doanh thu, API vẫn phải trả về 0đ chứ không được thiếu ngày.
			var fullData = new List<RevenueChartDto>();
			for (var day = fromDate; day <= toDate.Date; day = day.AddDays(1))
			{
				var dateLabel = day.ToString("dd/MM/yyyy");
				var existData = data.FirstOrDefault(x => x.Label == dateLabel);

				fullData.Add(new RevenueChartDto
				{
					Label = dateLabel,
					Revenue = existData?.Revenue ?? 0 // Nếu không có thì là 0
				});
			}

			return fullData;
		}
	}
}
