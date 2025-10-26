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
    public class ElementRepository : GenericRepository<Element>, IElementRepository
    {
        public ElementRepository(SEP_SEOBoostAIContext context) : base(context) { }

        public async Task<PaginationResult<List<Element>>> GetElementWithPaginateAsync(int currentPage, int pageSize)
        {
            var query = _context.Set<Element>().AsQueryable();

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            var elements = await query.Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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
