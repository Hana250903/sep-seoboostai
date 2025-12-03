using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Payments
{
	public class WalletService : IWalletService
	{
		private readonly IWalletRepository _walletRepository;
		private readonly ITransactionRepository _transactionRepository;
		private readonly IUnitOfWork _unitOfWork;

		public WalletService(IWalletRepository walletRepositoriy, IUnitOfWork unitOfWork, ITransactionRepository transactionRepository)
		{
			_walletRepository = walletRepositoriy;
			_unitOfWork = unitOfWork;
			_transactionRepository = transactionRepository;
		}

		public async Task<PaginationResult<List<Wallet>>> GetWalletsWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _walletRepository.GetWalletsWithPaginateAsync(currentPage, pageSize);
		}

		public async Task<List<Wallet>> GetWalletsAsync()
		{
			return await _walletRepository.GetAllAsync();
		}

		public async Task CreateAsync(Wallet wallet)
		{
			try
			{
				await _walletRepository.CreateAsync(wallet);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task UpdateAsync(Wallet wallet)
		{
			try
			{
				_walletRepository.UpdateAsync(wallet);
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
				var wallet = await _walletRepository.GetByIdAsync(id);
				await _walletRepository.RemoveAsync(wallet);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task<Wallet> GetWalletByUserIdAsync(int userId)
		{
			var wallet = await _walletRepository.GetWalletByUserIdAsync(userId);
			if (wallet == null)
			{
				throw new Exception("Wallet not found for this user.");
			}
			return wallet;
		}

		// HÀM MỚI CHO PAYOS (Nạp tiền)
		public async Task<bool> TopUp(int walletId, decimal amount, int transactionId)
		{
			try
			{
				// 1. Lấy ví
				var wallet = await _walletRepository.GetByIdAsync(walletId);
				if (wallet == null) throw new Exception("Ví không tồn tại");

				// 2. Cập nhật số dư
				wallet.Currency += amount;
				wallet.UpdatedAt = DateTime.UtcNow.AddHours(7);
				await UpdateAsync(wallet);

				var transaction = await _transactionRepository.GetByIdAsync(transactionId);
				if (transaction != null)
				{
					// LƯU SỐ DƯ SAU KHI NẠP VÀO LỊCH SỬ
					transaction.BalanceAfter = wallet.Currency;
					await _transactionRepository.UpdateAsync(transaction);
				}

				// 3. Lưu tất cả thay đổi
				await _unitOfWork.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{
				// Ghi log lỗi ở đây
				throw;
			}
		}

		public async Task DepositManualAsync(int userId, decimal amount)
		{
			try
			{
				// 1. Tìm ví của User
				var wallet = await _walletRepository.GetWalletByUserIdAsync(userId);
				if (wallet == null)
				{
					throw new Exception($"Không tìm thấy ví cho UserID: {userId}");
				}

				// 2. Cộng dồn tiền (Logic chính)
				wallet.Currency += amount;
				wallet.UpdatedAt = DateTime.UtcNow.AddHours(7);

				// 3. Cập nhật Ví
				_walletRepository.UpdateAsync(wallet);

				// 4. (QUAN TRỌNG) Tạo lịch sử giao dịch để đối soát
				// Để biết tại sao tiền lại tăng (không phải do PayOS nạp, mà do Admin nạp)
				var transaction = new Transaction
				{
					WalletID = wallet.WalletID,
					Money = amount,
					Type = "DEPOSIT",
					PaymentMethod = "MANUAL_ADMIN", // Đánh dấu là nạp tay
					Status = "COMPLETED",
					Description = $"Admin nạp tiền thủ công (+{amount:N0})",
					RequestTime = DateTime.UtcNow.AddHours(7),
					CompletedTime = DateTime.UtcNow.AddHours(7),
					GatewayTransactionId = "MANUAL_" + Guid.NewGuid().ToString("N"), // Mã giả
					IsDeleted = false
				};

				await _transactionRepository.CreateAsync(transaction);

				// 5. Lưu tất cả thay đổi (Ví + Transaction)
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}
	}
}
