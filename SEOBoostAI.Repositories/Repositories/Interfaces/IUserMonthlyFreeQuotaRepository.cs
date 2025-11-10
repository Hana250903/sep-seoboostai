using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories.Interfaces
{
	public interface IUserMonthlyFreeQuotaRepository : IGenericRepository<UserMonthlyFreeQuota>
	{
		Task<PaginationResult<List<UserMonthlyFreeQuota>>> GetUserMonthlyFreeQuotasWithPaginateAsync(int currentPage, int pageSize);
		Task<List<UserMonthlyFreeQuota>> CreateAsync(int userId);
		Task<List<UserMonthlyFreeQuota>> GetQuotasByUserId(int userId);
    }
}
