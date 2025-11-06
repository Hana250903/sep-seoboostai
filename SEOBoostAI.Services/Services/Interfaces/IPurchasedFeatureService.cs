using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IPurchasedFeatureService
	{
		Task<List<PurchasedFeature>> GetPurchasedFeaturesAsync();
		Task<PaginationResult<List<PurchasedFeature>>> GetPurchasedFeaturesWithPaginateAsync(int currentPage, int pageSize);
		Task<PurchasedFeature> GetPurchasedFeatureByIdAsync(int id);
		Task CreateAsync(PurchasedFeature purchasedFeature);
		Task UpdateAsync(PurchasedFeature purchasedFeature);
		Task DeleteAsync(int id);
	}
}
