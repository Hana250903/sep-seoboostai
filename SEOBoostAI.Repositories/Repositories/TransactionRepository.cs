using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
	public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
	{
		public TransactionRepository(SEP_SEOBoostAIContext context) : base(context) { }

		public async Task<PaginationResult<List<Transaction>>> GetTransactionsWithPaginateAsync(int currentPage, int pageSize)
		{
			var query = _context.Set<Transaction>()
								.OrderByDescending(t => t.CompletedTime)
								.Where(t => t.Status == "COMPLETED" && t.Type == "DEPOSIT")
								.AsQueryable();
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
			var transactions = await query.Skip((currentPage - 1) * pageSize)
				.Take(pageSize)
				.ToListAsync();
			var result = new PaginationResult<List<Transaction>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = transactions
			};
			return result;
		}

		public async Task<PaginationResult<List<Transaction>>> GetSuccessfulDepositsByUserIdAsync(int userId, int currentPage, int pageSize)
		{
			// 1. Tạo Query cơ bản (Chưa chạy lệnh SQL)
			var query = _context.Set<Transaction>() // Nhớ dùng .Set<Transaction>()
				.Where(t => t.UserID == userId
							&& t.Status == "COMPLETED"  // Chỉ lấy thành công
							&& t.Type == "DEPOSIT");   // Chỉ lấy nạp tiền

			// 2. Đếm tổng số lượng (để tính số trang)
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

			// 3. Thực hiện phân trang và lấy dữ liệu
			var transactions = await query
				.OrderByDescending(t => t.CompletedTime) // Mới nhất lên đầu
				.Skip((currentPage - 1) * pageSize)      // Bỏ qua các trang trước
				.Take(pageSize)                          // Lấy số lượng cần thiết
				.ToListAsync();

			// 4. Đóng gói kết quả
			return new PaginationResult<List<Transaction>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = transactions
			};
		}

		public async Task<Transaction> GetByGatewayTransactionIdAsync(string gatewayTransactionId)
		{
			return await _context.Set<Transaction>()
								 .FirstOrDefaultAsync(t => t.GatewayTransactionId == gatewayTransactionId);
		}

		public async Task<List<Transaction>> GetExpiredPendingTransactionsAsync(DateTime threshold)
		{
			return await _context.Set<Transaction>()
				.Where(t => t.Status == "PENDING" && t.RequestTime < threshold)
				.ToListAsync();
		}

		public async Task<List<Transaction>> GetTransactionsByIdAsync(int transactionId)
		{
			return await _context.Set<Transaction>()
				.Where(t => t.TransactionID == transactionId)
				.ToListAsync();
		}
	}
}
