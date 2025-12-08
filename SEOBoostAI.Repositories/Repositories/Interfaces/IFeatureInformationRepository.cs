using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories.Interfaces
{
	public interface IFeatureInformationRepository : IGenericRepository<FeatureInformation>
	{
		Task<List<FeatureInformation>> GetAllfeatureInformationsAsync();
		Task<List<FeatureInformation>> GetByFeatureIdAsync(int featureId);
		Task<PaginationResult<List<FeatureInformation>>> GetFeatureInformationsWithPaginateAsync(int currentPage, int pageSize);
	}
}
