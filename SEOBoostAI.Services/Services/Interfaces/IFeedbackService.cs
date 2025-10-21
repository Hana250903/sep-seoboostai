using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IFeedbackService
	{
		Task<List<Repository.Models.Feedback>> GetFeedbacksAsync();
		Task<Repository.ModelExtensions.PaginationResult<List<Repository.Models.Feedback>>> GetFeedbacksWithPaginateAsync(int currentPage, int pageSize);
		Task<Repository.Models.Feedback> GetFeedbackByIdAsync(int id);
		Task<int> CreateAsync(Repository.Models.Feedback feedback);
		Task<int> UpdateAsync(Repository.Models.Feedback feedback);
		Task<bool> DeleteAsync(int id);
	}
}
