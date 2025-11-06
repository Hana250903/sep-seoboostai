using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IFeatureService 
	{
		Task<List<Feature>> GetFeaturesAsync();
		Task<PaginationResult<List<Feature>>> GetFeaturesWithPaginateAsync(int currentPage, int pageSize);
		Task<Feature> GetFeatureByIdAsync(int id);
		Task CreateAsync(Feature feature);
		Task UpdateAsync(Feature feature);
		Task DeleteAsync(int id);
	}
}
