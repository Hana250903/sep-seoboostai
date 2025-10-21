using SEOBoostAI.Repository.ModelExtensions;
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
	public class ContentOptimizationService : IContentOptimizationService
	{
		private readonly ContentOptimizationRepository _contentOptimizationRepository;

		public ContentOptimizationService(ContentOptimizationRepository contentOptimizationRepository)
		{
			_contentOptimizationRepository = contentOptimizationRepository;
		}

		public async Task<int> CreateAsync(ContentOptimization content)
		{
			return await _contentOptimizationRepository.CreateAsync(content);
		}

		public async Task<bool> DeleteAsync(int id)
		{
			var content = await _contentOptimizationRepository.GetByIdAsync(id);
			return await _contentOptimizationRepository.RemoveAsync(content);
		}

		public async Task<ContentOptimization> GetContentOptimizationByIdAsync(int id)
		{
			return await _contentOptimizationRepository.GetByIdAsync(id);
		}

		public async Task<List<ContentOptimization>> GetContentOptimizationsAsync()
		{
			return await _contentOptimizationRepository.GetAllAsync();
		}

		public async Task<PaginationResult<List<ContentOptimization>>> GetContentOptimizationsWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _contentOptimizationRepository.GetContentOptimizationWithPaginateAsync(currentPage, pageSize);
		}

		public async Task<int> UpdateAsync(ContentOptimization contentOptimization)
		{
			return await _contentOptimizationRepository.UpdateAsync(contentOptimization);
		}
	}
}
