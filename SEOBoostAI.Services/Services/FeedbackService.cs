using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
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
        private readonly IUnitOfWork _unitOfWork;

        public FeedbackService(IFeedbackRepository feedbackRepository, IUnitOfWork unitOfWork)
		{
			_feedbackRepository = feedbackRepository;
            _unitOfWork = unitOfWork;

        }
		public async Task CreateAsync(Feedback feedback)
		{
			try
			{
                await _feedbackRepository.CreateAsync(feedback);
				await _unitOfWork.SaveChangesAsync();
            }
			catch
			{
				throw;
            }
        }
		public async Task DeleteAsync(int id)
		{
			try
			{
                var feedback = await _feedbackRepository.GetByIdAsync(id);
                await _feedbackRepository.RemoveAsync(feedback);
            }
			catch
			{
				throw;
            }
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
			try
			{
                await _feedbackRepository.UpdateAsync(feedback);
                await _unitOfWork.SaveChangesAsync();
            }
			catch
			{
				throw;
			}
        }
	}
}
