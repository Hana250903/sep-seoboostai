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
    public class AnalysisSnapshotRepository : GenericRepository<AnalysisSnapshot>, IAnalysisSnapshotRepository
    {
        public AnalysisSnapshotRepository(SEP_SEOBoostAIContext context) : base(context)
        {
        }

        public async Task<AnalysisSnapshot> GetAnalysisSnapshotByAnalysisCacheIdAsync(int analysisCacheId)
        {
            return await _context.Set<AnalysisSnapshot>().Where(s => s.AnalysisCacheID == analysisCacheId)
                .OrderByDescending(s => s.AnalyzedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<PaginationResult<List<AnalysisSnapshot>>> GetAnalysisSnapshotsWithPagination(int currentPage,int pageSize)
        {
            var query = _context.Set<AnalysisSnapshot>().AsQueryable();
            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            var items = await query
                .OrderByDescending(s => s.AnalyzedAt)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PaginationResult<List<AnalysisSnapshot>>
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                Items = items
            };
        }


    }
}
