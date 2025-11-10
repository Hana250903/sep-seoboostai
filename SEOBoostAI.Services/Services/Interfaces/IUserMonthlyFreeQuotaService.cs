using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IUserMonthlyFreeQuotaService
	{
		Task<List<UserMonthlyFreeQuota>> GetUserMonthlyFreeQuotasAsync();
		Task<PaginationResult<List<UserMonthlyFreeQuota>>> GetUserMonthlyFreeQuotasWithPaginateAsync(int currentPage, int pageSize);
		Task<UserMonthlyFreeQuota> GetUserMonthlyFreeQuotaByIdAsync(int id);
		Task CreateAsync(UserMonthlyFreeQuota userMonthlyFreeQuota);
		Task UpdateAsync(UserMonthlyFreeQuota userMonthlyFreeQuota);
		Task DeleteAsync(int id);
		Task<int> CreateQuotaAsync(int userId);
		Task<int> UpdateMonthQuotaAsync(int userId);
    }
}
