using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
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
		private readonly FeedbackRepository _feedbackRepository;

		public FeedbackService(FeedbackRepository feedbackRepository)
		{
			_feedbackRepository = feedbackRepository;
		}
		public async Task<int> CreateAsync(Feedback feedback)
		{
			return await _feedbackRepository.CreateAsync(feedback);
		}
		public async Task<bool> DeleteAsync(int id)
		{
			var feedback = await _feedbackRepository.GetByIdAsync(id);
			return await _feedbackRepository.RemoveAsync(feedback);
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
		public async Task<int> UpdateAsync(Feedback feedback)
		{
			return await _feedbackRepository.UpdateAsync(feedback);
		}
	}
}
