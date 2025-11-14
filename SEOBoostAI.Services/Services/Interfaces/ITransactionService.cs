using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface ITransactionService
	{
		Task<List<Transaction>> GetTransactionsAsync();
		Task<PaginationResult<List<Transaction>>> GetTransactionsWithPaginateAsync(int currentPage, int pageSize);
		Task<Transaction> GetTransactionByIdAsync(int id);
		Task CreateAsync(Transaction transaction);
		Task UpdateAsync(Transaction transaction);
		Task DeleteAsync(int id);

		// HÀM MỚI CHO PAYOS
		Task<Transaction> CreatePendingDeposit(int walletId, decimal amount, string paymentMethod);
	}
}
