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

        public async Task CreateAsync(int userId)
		{
			var features = _context.Set<Feature>().ToList();
			var limitSetting = await _context.Set<SystemSetting>().FirstOrDefaultAsync(s => s.SettingKey == "QuotaMonlyLimit");
            var userMonthlyFreeQuotas = new List<UserMonthlyFreeQuota>();

            int defaultLimit = 0;
            if (limitSetting != null && int.TryParse(limitSetting.SettingValue, out int parsedLimit))
            {
                defaultLimit = parsedLimit;
            }

            foreach (var feature in features)
            {
                userMonthlyFreeQuotas.Add(new UserMonthlyFreeQuota
                {
                    UserID = userId,
                    FeatureID = feature.FeatureID,
                    MonthlyLimit = defaultLimit,
                    MonthYear = DateTime.Now.ToString("yyyy-MM"),
                    UsageCount = 0,
                    IsDeleted = false,
                });
            }

			await _context.Set<UserMonthlyFreeQuota>().AddRangeAsync(userMonthlyFreeQuotas);
        }

		public async Task<List<UserMonthlyFreeQuota>> GetQuotasByUserId(int userId)
		{
			var userMonthlyFreeQuota = _context.Set<UserMonthlyFreeQuota>().Where(u => u.UserID == userId).ToList();
			return userMonthlyFreeQuota;
		}

		public async Task<UserMonthlyFreeQuota> GetQuotaByUserIdAndFeatureId(int userId, int featureId)
		{
			var userMonthlyFreeQuota = await _context.Set<UserMonthlyFreeQuota>()
				.FirstOrDefaultAsync(u => u.UserID == userId && u.FeatureID == featureId);
			return userMonthlyFreeQuota;
        }

        public async Task UpdateMonthlyLimitBatchAsync(string monthYear, int newLimit)
        {
			var userMonthlyFreeQuotas = await _context.Set<UserMonthlyFreeQuota>().Where(q => q.MonthYear == monthYear).ToListAsync();

			var newUserMonthlyFreeQuotas = new List<UserMonthlyFreeQuota>();
            foreach (var quota in userMonthlyFreeQuotas)
			{
				newUserMonthlyFreeQuotas.Add(new UserMonthlyFreeQuota
				{
					UserMonthlyFreeQuotaID = quota.UserMonthlyFreeQuotaID,
					UserID = quota.UserID,
					FeatureID = quota.FeatureID,
					MonthYear = quota.MonthYear,
					MonthlyLimit = newLimit,
					UsageCount = quota.UsageCount,
					LastUsedAt = quota.LastUsedAt,
					IsDeleted = quota.IsDeleted
				});
            }
			_context.UpdateRange(newUserMonthlyFreeQuotas);

            // Sử dụng ExecuteUpdateAsync để update trực tiếp trên SQL (Bulk Update)
            // Không cần kéo dữ liệu về RAM -> Tránh treo server
            //        await _context.UserMonthlyFreeQuotas
            //        .Where(x => x.MonthYear == monthYear && !x.IsDeleted)
            //        .ExecuteUpdateAsync(s => s.SetProperty(p => p.MonthlyLimit, newLimit));
        }
	}
}
