using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IContentOptimizationService
	{
		Task<List<ContentOptimizationDto>> GetContentOptimizationsAsync();
		Task<PaginationResult<List<ContentOptimizationDto>>> GetContentOptimizationsWithPaginateAsync(int currentPage, int pageSize);
		Task<ContentOptimizationDto> GetContentOptimizationByIdAsync(int id);
		Task CreateAsync(ContentOptimization contentOptimization);
		Task UpdateAsync(ContentOptimization contentOptimization);
		Task DeleteAsync(int id);
		Task<ContentOptimizationDto> OptimizeAndCreateAsync(OptimizeRequestDto request);
	}
}
