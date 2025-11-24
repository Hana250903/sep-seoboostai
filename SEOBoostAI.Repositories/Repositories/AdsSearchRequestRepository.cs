using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
    public class AdsSearchRequestRepository : GenericRepository<AdsSearchRequest>, IAdsSearchRequestRepository
    {
        public AdsSearchRequestRepository(SEP_SEOBoostAIContext content) : base(content) { }

        public async Task<AdsSearchRequest?> GetValidCacheAsync(string queryList)
        {
            var cacheExpiryDate = DateTime.UtcNow.AddDays(-30);

            // SỬA LỖI Ở ĐÂY:
            // Thay vì dùng "_dbSet", ta dùng "_context.Set<AdsSearchRequest>()"
            // Cách này luôn hoạt động mà không cần lo về quyền truy cập của lớp cha.
            
            return await _context.Set<AdsSearchRequest>()
                .Include(x => x.AdsKeywordData) 
                .FirstOrDefaultAsync(x => x.QueryList == queryList && x.CreatedAt >= cacheExpiryDate);
        }
    }
}