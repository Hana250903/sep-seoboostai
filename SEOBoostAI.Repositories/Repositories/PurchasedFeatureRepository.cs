using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
	public class PurchasedFeatureRepository : GenericRepository<PurchasedFeature>, IPurchasedFeatureRepository
	{
		public PurchasedFeatureRepository(SEP_SEOBoostAIContext context) : base(context) { }
		public async Task<PaginationResult<List<PurchasedFeature>>> GetPurchasedFeaturesWithPaginateAsync(int currentPage, int pageSize)
		{
			var query = _context.Set<PurchasedFeature>().AsQueryable();
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
			var purchasedFeatures = await query.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
			var result = new PaginationResult<List<PurchasedFeature>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = purchasedFeatures
			};
			return result;
		}

		public async Task<PurchasedFeature?> GetAvailablePackAsync(int userId, int featureId)
		{
			// Tìm gói mua cũ nhất (FIFO) mà còn lượt sử dụng (RemainingQuantity > 0)
			return await _context.Set<PurchasedFeature>()
				.Include(p => p.Transaction)
				.ThenInclude(t => t.Wallet)
				.Where(p => p.Transaction.Wallet.UserID == userId
							&& p.FeatureID == featureId
							&& p.RemainingQuantity > 0)
				.OrderBy(p => p.PurchaseDate) // Dùng gói cũ trước
				.FirstOrDefaultAsync();
		}
	}
}
