using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
	public class ContentOptimizationRepository : GenericRepository<ContentOptimization>, IContentOptimizationRepository
	{

        public ContentOptimizationRepository(SEP_SEOBoostAIContext context): base(context) { }

        public async Task<PaginationResult<List<ContentOptimization>>> GetContentOptimizationWithPaginateAsync(int currentPage, int pageSize)
		{
			var query = _context.Set<ContentOptimization>().AsQueryable();

            var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

			var contents = await query.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var result = new PaginationResult<List<ContentOptimization>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = contents
			};
			return result;
		}
	}
}
