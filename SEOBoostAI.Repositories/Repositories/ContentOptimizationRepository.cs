using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
	public class ContentOptimizationRepository : GenericRepository<ContentOptimization>, IContentOptimizationRepository
	{

		public ContentOptimizationRepository(SEP_SEOBoostAIContext context) : base(context) { }

		public async Task<PaginationResult<List<ContentOptimization>>> GetContentOptimizationWithPaginateAsync(SearchTransactionRequest searchRequest, int userId)
		{
			int currentPage = searchRequest.CurrentPage ?? 1;
			int pageSize = searchRequest.PageSize ?? 10;
			string keyword = searchRequest.Keyword;
			string createdAt = searchRequest.CreatedAt;

			var query = _context.Set<ContentOptimization>().Where(co => co.UserID == userId).AsQueryable();

			if (!string.IsNullOrEmpty(keyword))
			{
				query = query.Where(co => co.UserRequest.ToLower().Contains(keyword));
			}

			// 3. SỬA: Lọc theo Ngày tạo (Cách chính xác)
			if (!string.IsNullOrEmpty(createdAt) &&
				DateTime.TryParse(createdAt, out DateTime parsedDate))
			{
				// Yêu cầu CSDL chỉ so sánh phần NGÀY (bỏ qua giờ/phút/giây)
				query = query.Where(co =>
					// BƯỚC 1: Đảm bảo CreatedAt không phải là NULL
					co.CreatedAt.HasValue &&
					// BƯỚC 2: Dùng .Value.Date để so sánh
					co.CreatedAt.Value.Date == parsedDate.Date
				);
			}

			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

			var contents = await query
				.OrderByDescending(co => co.CreatedAt)
				.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();

			var result = new PaginationResult<List<ContentOptimization>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = contents
			};
			return result;
		}

		public async Task<List<ContentOptimization>> GetAllByUserIdAsync(int userId)
		{
			return await _context.Set<ContentOptimization>()
								 .Where(co => co.UserID == userId)
								 .OrderByDescending(co => co.CreatedAt)
								 .ToListAsync();
		}
	}
}
