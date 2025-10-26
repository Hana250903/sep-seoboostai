using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
	public class FeedbackService : IFeedbackService
	{
		private readonly IFeedbackRepository _feedbackRepository;

		public FeedbackService(IFeedbackRepository feedbackRepository)
		{
			_feedbackRepository = feedbackRepository;
		}
		public async Task CreateAsync(Feedback feedback)
		{
			throw new NotImplementedException();
		}
		public async Task DeleteAsync(int id)
		{
			var feedback = await _feedbackRepository.GetByIdAsync(id);
            throw new NotImplementedException();
        }
		public async Task<Feedback> GetFeedbackByIdAsync(int id)
		{
			return await _feedbackRepository.GetByIdAsync(id);
		}
		public async Task<List<Feedback>> GetFeedbacksAsync()
		{
			return await _feedbackRepository.GetAllAsync();
		}
		public async Task<Repository.ModelExtensions.PaginationResult<List<Feedback>>> GetFeedbacksWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _feedbackRepository.GetFeedbackWithPaginateAsync(currentPage, pageSize);
		}
		public async Task UpdateAsync(Feedback feedback)
		{
            throw new NotImplementedException();
        }
	}
}
