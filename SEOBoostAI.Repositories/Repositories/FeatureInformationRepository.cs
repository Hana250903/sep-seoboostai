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
	public class FeatureInformationRepository : GenericRepository<FeatureInformation> ,IFeatureInformationRepository
	{
		public FeatureInformationRepository(SEP_SEOBoostAIContext context) : base(context) { }

		public async Task<List<FeatureInformation>> GetAllfeatureInformationsAsync()
		{
			return await _context.Set<FeatureInformation>().ToListAsync();
		}

		public async Task<List<FeatureInformation>> GetByFeatureIdAsync(int featureId)
		{
			return await _context.Set<FeatureInformation>()
								 .Where(x => x.FeatureID == featureId)
								 .OrderBy(x => x.CreatedAt)
								 .ToListAsync();
		}

		public async Task<PaginationResult<List<FeatureInformation>>> GetFeatureInformationsWithPaginateAsync(int currentPage, int pageSize)
		{
			var query = _context.Set<FeatureInformation>()
								.OrderBy(fi => fi.InformationID)
								.AsQueryable();
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
			var featureInformations = await query.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
			var result = new PaginationResult<List<FeatureInformation>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = featureInformations
			};
			return result;
		}
	}
}
