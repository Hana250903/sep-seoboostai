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
    public class ElementRepository : GenericRepository<Element>
    {
        public ElementRepository() { }

        public async Task<PaginationResult<List<Element>>> GetElementWithPaginateAsync(int currentPage, int pageSize)
        {
            var elements = await GetAllAsync();

            var totalItems = elements.Count;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            elements = elements.Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var result = new PaginationResult<List<Element>>
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = currentPage,
                PageSize = pageSize,
                Items = elements
            };
            return result;
        }
    }
}
