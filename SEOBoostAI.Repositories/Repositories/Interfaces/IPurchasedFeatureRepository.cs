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
	public interface IPurchasedFeatureRepository : IGenericRepository<PurchasedFeature>
	{
		Task<PaginationResult<List<PurchasedFeature>>> GetPurchasedFeaturesWithPaginateAsync(int currentPage, int pageSize);
	}
}
