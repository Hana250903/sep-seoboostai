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

        public async Task<List<UserMonthlyFreeQuota>> CreateAsync(int userId)
		{
			var features = _context.Set<Feature>().ToList();

            var userMonthlyFreeQuotas = new List<UserMonthlyFreeQuota>();

            foreach (var feature in features)
            {
                userMonthlyFreeQuotas.Add(new UserMonthlyFreeQuota
                {
                    UserID = userId,
                    FeatureID = feature.FeatureID,
                    MonthlyLimit = 3,
                    MonthYear = DateTime.Now.ToString("yyyy-MM"),
                    UsageCount = 0,
                    IsDeleted = false,
                });
            }

			return userMonthlyFreeQuotas;
        }

		public async Task<List<UserMonthlyFreeQuota>> GetQuotasByUserId(int userId)
		{
			var userMonthlyFreeQuota = _context.Set<UserMonthlyFreeQuota>().Where(u => u.UserID == userId).ToList();
			return userMonthlyFreeQuota;
		}

    }
}
