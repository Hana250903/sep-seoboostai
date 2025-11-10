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
	}
}