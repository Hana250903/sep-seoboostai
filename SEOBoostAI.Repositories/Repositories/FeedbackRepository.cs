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
	public class FeedbackRepository : GenericRepository<Feedback>, IFeedbackRepository
	{
		public FeedbackRepository(SEP_SEOBoostAIContext context) : base(context) { }

		public async Task<PaginationResult<List<Feedback>>> GetFeedbackWithPaginateAsync(int currentPage, int pageSize)
		{
			var query = _context.Set<Feedback>().AsQueryable();
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
			var feedbacks = await query.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
			var result = new PaginationResult<List<Feedback>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = feedbacks
			};
			return result;
		}
	}
}
