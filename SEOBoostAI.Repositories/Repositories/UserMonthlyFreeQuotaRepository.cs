using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
	public class UserMonthlyFreeQuotaRepository : GenericRepository<UserMonthlyFreeQuota>, IUserMonthlyFreeQuotaRepository
	{
		public UserMonthlyFreeQuotaRepository(SEP_SEOBoostAIContext context) : base(context) { }
		public async Task<PaginationResult<List<UserMonthlyFreeQuota>>> GetUserMonthlyFreeQuotasWithPaginateAsync(int currentPage, int pageSize)
		{
			var query = _context.Set<UserMonthlyFreeQuota>().AsQueryable();
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
			var userMonthlyFreeQuotas = await query.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
			var result = new PaginationResult<List<UserMonthlyFreeQuota>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = userMonthlyFreeQuotas
			};
			return result;
		}
	}
}
