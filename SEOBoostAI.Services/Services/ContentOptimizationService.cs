using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
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
	public class ContentOptimizationService : IContentOptimizationService
	{
		private readonly IContentOptimizationRepository _contentOptimizationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ContentOptimizationService(IContentOptimizationRepository contentOptimizationRepository, IUnitOfWork unitOfWork)
		{
			_contentOptimizationRepository = contentOptimizationRepository;
            _unitOfWork = unitOfWork;
        }

		public async Task CreateAsync(ContentOptimization content)
		{
			try
			{
				await _contentOptimizationRepository.CreateAsync(content);
				await _unitOfWork.SaveChangesAsync();
            }
			catch (Exception ex)
			{
				throw;
			}

		}

		public async Task DeleteAsync(int id)
		{
			var content = await _contentOptimizationRepository.GetByIdAsync(id);
			//return await _contentOptimizationRepository.RemoveAsync(content);
			throw new NotImplementedException();
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

		public async Task UpdateAsync(ContentOptimization contentOptimization)
		{
			//return await _contentOptimizationRepository.UpdateAsync(contentOptimization);
            throw new NotImplementedException();
        }
	}
}
