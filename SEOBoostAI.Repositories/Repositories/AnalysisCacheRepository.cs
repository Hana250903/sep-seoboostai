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
    public class AnalysisCacheRepository : GenericRepository<AnalysisCache>, IAnalysisCacheRepository
    {
        public AnalysisCacheRepository(SEP_SEOBoostAIContext context) : base(context) { }

        public async Task<PaginationResult<List<AnalysisCache>>> GetAnalysisCachesWithPaginateAsync(int currentPage, int pageSize)
        {
            var query = _context.Set<AnalysisCache>().AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var analysisCaches = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PaginationResult<List<AnalysisCache>>
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                Items = analysisCaches
            };
            return result;
        }

        public async Task<AnalysisCache> GetAnalysisCacheAsync(int id)
        {
            return await _context.Set<AnalysisCache>().Include(p => p.Elements).FirstOrDefaultAsync(p => p.AnalysisCacheID == id);
        }
    }
}
