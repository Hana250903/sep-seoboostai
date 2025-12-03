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
	public interface ITransactionRepository : IGenericRepository<Transaction>
	{
		Task<PaginationResult<List<Transaction>>> GetTransactionsWithPaginateAsync(int currentPage, int pageSize);
		Task<PaginationResult<List<Transaction>>> GetSuccessfulDepositsByUserIdAsync(int userId, int currentPage, int pageSize);
		Task<Transaction> GetByGatewayTransactionIdAsync(string gatewayTransactionId);
		Task<List<Transaction>> GetExpiredPendingTransactionsAsync(DateTime threshold);
		Task<List<Transaction>> GetTransactionsByIdAsync(int transactionId);
	}
}
