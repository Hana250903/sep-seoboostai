using SEOBoostAI.Repository.ModelExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IAdminDashboardService
	{
		Task<DashboardOverviewDto> GetOverviewAsync();
		Task<List<RevenueChartDto>> GetRevenueChartAsync(string type);
	}
}
