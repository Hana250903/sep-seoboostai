using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
    public class PerformanceRepository: GenericRepository<Performance>
    {
        public PerformanceRepository() { }

        public async Task<PaginationResult<List<Performance>>> GetPerformancesWithPaginateAsync(int currentPage, int pageSize)
        {
            var performances = await GetAllAsync();

            var totalItems = performances.Count();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            performances = performances
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PaginationResult<List<Performance>>
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                Items = performances
            };
            return result;
        }
    }
}
