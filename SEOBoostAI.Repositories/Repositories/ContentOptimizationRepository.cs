using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
	public class ContentOptimizationRepository : GenericRepository<ContentOptimization>
	{
		public ContentOptimizationRepository() { }

		public async Task<PaginationResult<List<ContentOptimization>>> GetContentOptimizationWithPaginateAsync(int currentPage, int pageSize)
		{
			var contents = await GetAllAsync();

			var totalItems = contents.Count;
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

			contents = contents.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToList();

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
