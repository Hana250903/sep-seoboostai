using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.Enums;
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
	public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
	{
		public TransactionRepository(SEP_SEOBoostAIContext context) : base(context) { }

		public async Task<PaginationResult<List<Transaction>>> GetTransactionsWithPaginateAsync(int currentPage, int pageSize)
		{
			var query = _context.Set<Transaction>()
								.OrderByDescending(t => t.CompletedTime)
								.Where(t => t.Status == PaymentStatus.COMPLETED.ToString() && t.Type == PaymentType.DEPOSIT.ToString() || t.Type == PaymentType.PURCHASE.ToString())
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
			// 1. Tạo Query cơ bản
			var query = _context.Set<Transaction>()
				.Where(t => t.UserID == userId
							&& t.Status == PaymentStatus.COMPLETED.ToString()
							&& (t.Type == PaymentType.DEPOSIT.ToString() || t.Type == PaymentType.PURCHASE.ToString()));

			// 2. Đếm tổng số lượng
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
			return await _context.Set<Transaction>().OrderByDescending(t => t.RequestTime).FirstOrDefaultAsync(t => t.GatewayTransactionId == gatewayTransactionId);
		}

		public async Task<List<Transaction>> GetExpiredPendingTransactionsAsync(DateTime threshold)
		{
			return await _context.Set<Transaction>()
				.Where(t => t.Status == PaymentStatus.PENDING.ToString() && t.RequestTime < threshold)
				.OrderByDescending(t => t.RequestTime)
				.ToListAsync();
		}

		public async Task<List<Transaction>> GetTransactionsByIdAsync(int transactionId)
		{
			return await _context.Set<Transaction>()
				.Where(t => t.TransactionID == transactionId)
				.OrderByDescending(t => t.RequestTime)
				.ToListAsync();
		}

		//Hàm tính tổng doanh thu theo khoảng thời gian (dùng cho Overview)
		public async Task<decimal> GetTotalRevenueAsync(DateTime? fromDate, DateTime? toDate)
		{
			var query = _context.Set<Transaction>()
				.OrderByDescending(t => t.CompletedTime)
				.Where(t => t.Type == PaymentType.DEPOSIT.ToString() && t.Status == PaymentStatus.COMPLETED.ToString());

			if (fromDate.HasValue)
				query = query.Where(t => t.CompletedTime >= fromDate.Value);

			if (toDate.HasValue)
				query = query.Where(t => t.CompletedTime <= toDate.Value);

			return await query.SumAsync(t => t.Money);
		}

		//Hàm lấy dữ liệu biểu đồ theo ngày (trong khoảng ngày A đến ngày B)
		public async Task<List<RevenueChartDto>> GetRevenueChartDataAsync(DateTime fromDate, DateTime toDate)
		{
			// Lấy dữ liệu thô trước
			var transactions = await _context.Set<Transaction>()
				.Where(t => t.Type == PaymentType.DEPOSIT.ToString() &&
							t.Status == PaymentStatus.COMPLETED.ToString() &&
							t.CompletedTime >= fromDate &&
							t.CompletedTime <= toDate)
				.Select(t => new { t.CompletedTime, t.Money })
				.ToListAsync();

			// Xử lý GroupBy ở phía Client (C#) để tránh lỗi translation của EF Core với Date
			var result = transactions
				.GroupBy(t => t.CompletedTime.Value.Date) // Nhóm theo ngày
				.Select(g => new RevenueChartDto
				{
					Label = g.Key.ToString("dd/MM/yyyy"),
					Revenue = g.Sum(x => x.Money)
				})
				.OrderBy(x => x.Label)
				.ToList();

			return result;
		}

		public async Task<Transaction> GetTransactionDetailAsync(int transactionId)
		{
			return await _context.Set<Transaction>()
				.OrderByDescending(t => t.RequestTime)
				.Include(t => t.User)
				.FirstOrDefaultAsync(t => t.TransactionID == transactionId);
		}
	}
}
