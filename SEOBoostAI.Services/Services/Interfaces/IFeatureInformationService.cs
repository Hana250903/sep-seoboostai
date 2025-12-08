using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IFeatureInformationService
	{
		Task<List<FeatureInformationDto>> GetListByFeatureIdAsync(int featureId);
		Task<FeatureInformationDto> GetByIdAsync(int id);
		Task<FeatureInformationDto> CreateAsync(CreateFeatureInfoRequest request);
		Task UpdateAsync(int id, UpdateFeatureInfoRequest request);
		Task DeleteAsync(int id);
		Task<PaginationResult<List<FeatureInformation>>> GetFeatureInformationsWithPaginateAsync(int currentPage, int pageSize);
	}
}
