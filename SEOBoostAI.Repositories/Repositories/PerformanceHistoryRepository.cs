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
    public class PerformanceHistoryRepository : GenericRepository<PerformanceHistory>, IPerformanceHistoryRepository
    {
        public PerformanceHistoryRepository(SEP_SEOBoostAIContext context) : base(context)
        {
        }

        public async Task<PaginationResult<List<PerformanceHistory>>> GetPerformanceHistorysWithPagination(int currentPage, int pageSize, int? userId)
        {
            var query = _context.Set<PerformanceHistory>().AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(ph => ph.UserID == userId.Value);
            }

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query.Include(ph => ph.AnalysisCache).ThenInclude(ac => ac.Elements)
                .OrderByDescending(ph => ph.ScanTime)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PaginationResult<List<PerformanceHistory>>()
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                Items = items
            };

            return result;
        }

        public async Task<PerformanceHistory> GetByIdAsync(int performanceHistoryId)
        {
            return await _context.Set<PerformanceHistory>().Where(ph => ph.ScanHistoryID == performanceHistoryId).Include(ph => ph.AnalysisCache).ThenInclude(ac => ac.Elements).FirstOrDefaultAsync();
        }

        public async Task<bool> CheckUserHasUrl(int userId, string normalizedUrlToCheck, string strategy)
        {
            return await _context.Set<PerformanceHistory>()
                .AnyAsync(ph => ph.UserID == userId && ph.AnalysisCache.NormalizedUrl == normalizedUrlToCheck && ph.AnalysisCache.Strategy == strategy);
        }

        public async Task<PerformanceHistory> GetByUserIdAndUrlAsync(int userId, string normalizedUrlToCheck, string strategy)
        {
            return await _context.Set<PerformanceHistory>()
                .Include(ph => ph.AnalysisCache)
                .ThenInclude(ac => ac.Elements)
                .Where(ph => ph.UserID == userId && ph.AnalysisCache.NormalizedUrl == normalizedUrlToCheck && ph.AnalysisCache.Strategy == strategy)
                .FirstOrDefaultAsync();
        }

        public async Task UpdateScanTimeAsync(int performanceHistoryId)
        {
            // Update trực tiếp trong Database, KHÔNG load về RAM, KHÔNG bị lỗi tracking
            await _context.Set<PerformanceHistory>()
                .Where(ph => ph.ScanHistoryID == performanceHistoryId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(ph => ph.ScanTime, DateTime.UtcNow.AddHours(7))
                );
        }
    }
}
