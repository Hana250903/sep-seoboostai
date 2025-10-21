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
	public class FeedbackRepository : GenericRepository<Feedback>
	{
		public FeedbackRepository() { }

		public async Task<PaginationResult<List<Feedback>>> GetFeedbackWithPaginateAsync(int currentPage, int pageSize)
		{
			var feedbacks = await GetAllAsync();
			var totalItems = feedbacks.Count;
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
			feedbacks = feedbacks.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToList();
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
