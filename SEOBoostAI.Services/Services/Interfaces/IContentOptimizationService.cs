using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IContentOptimizationService
	{
		Task<List<ContentOptimization>> GetContentOptimizationsAsync();
		Task<PaginationResult<List<ContentOptimization>>> GetContentOptimizationsWithPaginateAsync(int currentPage, int pageSize);
		Task<ContentOptimization> GetContentOptimizationByIdAsync(int id);
		Task CreateAsync(ContentOptimization contentOptimization);
		Task UpdateAsync(ContentOptimization contentOptimization);
		Task DeleteAsync(int id);
		Task<ContentOptimization> OptimizeAndCreateAsync(OptimizeRequestDto request);
	}
}
