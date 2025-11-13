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

        public async Task<PaginationResult<List<PerformanceHistory>>> GetPerformanceHistorysWithPagination(int currentPage, int pageSize)
        {
            var query = _context.Set<PerformanceHistory>().AsQueryable();

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
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
    }
}
