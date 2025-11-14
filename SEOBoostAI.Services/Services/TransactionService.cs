using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
	public class TransactionService : ITransactionService
	{
		private readonly ITransactionRepository _transactionRepository;
		private readonly IUnitOfWork _unitOfWork;
		public TransactionService(ITransactionRepository transactionRepository, IUnitOfWork unitOfWork)
		{
			_transactionRepository = transactionRepository;
			_unitOfWork = unitOfWork;
		}
		public async Task<PaginationResult<List<Transaction>>> GetTransactionsWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _transactionRepository.GetTransactionsWithPaginateAsync(currentPage, pageSize);
		}
		public async Task<Transaction> GetTransactionByIdAsync(int id)
		{
			return await _transactionRepository.GetByIdAsync(id);
		}
		public async Task<List<Transaction>> GetTransactionsAsync()
		{
			return await _transactionRepository.GetAllAsync();
		}
		public async Task CreateAsync(Transaction transaction)
		{
			try
			{
				await _transactionRepository.CreateAsync(transaction);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}
		public async Task UpdateAsync(Transaction transaction)
		{
			try
			{
				_transactionRepository.UpdateAsync(transaction);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}
		public async Task DeleteAsync(int id)
		{
			try
			{
				var transaction = await _transactionRepository.GetByIdAsync(id);
				if (transaction != null)
				{
					_transactionRepository.RemoveAsync(transaction);
					await _unitOfWork.SaveChangesAsync();
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		// HÀM MỚI CHO PAYOS
		public async Task<Transaction> CreatePendingDeposit(int walletId, decimal amount, string paymentMethod)
		{
			var newTransaction = new Transaction
			{
				WalletID = walletId,
				Money = amount,
				PaymentMethod = paymentMethod,
				Type = "DEPOSIT",
				Description = "Nạp tiền vào ví qua PayOS",
				Status = "PENDING", // Trạng thái quan trọng
				RequestTime = DateTime.UtcNow,
				IsDeleted = false
				// GatewayTransactionId, BankTransId, CompletedTime sẽ được cập nhật bởi Webhook
			};

			// Dùng hàm CreateAsync đã có của bạn (vì nó tự SaveChanges)
			// Điều này tốt vì chúng ta cần ID ngay lập tức
			await CreateAsync(newTransaction);

			// Sau khi CreateAsync, newTransaction đã có TransactionID từ CSDL
			return newTransaction;
		}
	}
}