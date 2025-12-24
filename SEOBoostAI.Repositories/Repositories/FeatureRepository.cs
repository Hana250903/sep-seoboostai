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
	public class FeatureRepository : GenericRepository<Feature>, IFeatureRepository
	{
		public FeatureRepository(SEP_SEOBoostAIContext context) : base(context) { }

		public async Task<PaginationResult<List<Feature>>> GetFeaturesWithPaginateAsync(int currentPage, int pageSize)
		{
			var query = _context.Set<Feature>().Include(f => f.FeatureInformations).AsQueryable();
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
			var features = await query.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
			var result = new PaginationResult<List<Feature>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = features
			};
			return result;
		}

		public async Task<Feature> GetFeatureByIdAsync(int id)
		{
			return await _context.Set<Feature>().FirstOrDefaultAsync(f => f.FeatureID == id);
		}

		public async Task<List<Feature>> GetAllFeaturesAsync()
		{
			return await _context.Set<Feature>()
				.Include(f => f.FeatureInformations) // Lấy kèm thông tin chi tiết
				.ToListAsync();
		}
	}
}
