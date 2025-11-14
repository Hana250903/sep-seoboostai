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
	public class WalletService : IWalletService
	{
		private readonly IWalletRepository _walletRepositoriy;
		private readonly IUnitOfWork _unitOfWork;

		public WalletService(IWalletRepository walletRepositoriy, IUnitOfWork unitOfWork)
		{
			_walletRepositoriy = walletRepositoriy;
			_unitOfWork = unitOfWork;
		}

		public async Task<PaginationResult<List<Wallet>>> GetWalletsWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _walletRepositoriy.GetWalletsWithPaginateAsync(currentPage, pageSize);
		}

		public async Task<Wallet> GetWalletByIdAsync(int id)
		{
			return await _walletRepositoriy.GetByIdAsync(id);
		}

		public async Task<List<Wallet>> GetWalletsAsync()
		{
			return await _walletRepositoriy.GetAllAsync();
		}

		public async Task CreateAsync(Wallet wallet)
		{
			try
			{
				await _walletRepositoriy.CreateAsync(wallet);
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
				_walletRepositoriy.UpdateAsync(wallet);
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
				var wallet = await _walletRepositoriy.GetByIdAsync(id);
				await _walletRepositoriy.RemoveAsync(wallet);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		// HÀM MỚI (Lấy ví bằng UserID)
		public async Task<Wallet> GetWalletByUserIdAsync(int userId)
		{
			// LƯU Ý: Bạn cần thêm phương thức GetWalletByUserIdAsync
			// vào IWalletRepository và triển khai nó ở lớp Repository
			// (Nó sẽ dùng LINQ: _context.Wallets.FirstOrDefaultAsync(w => w.UserID == userId))
			var wallet = await _walletRepositoriy.GetByIdAsync(userId);
			if (wallet == null)
			{
				throw new Exception("Wallet not found for this user.");
			}
			return wallet;
		}

		// HÀM MỚI CHO PAYOS (Nạp tiền)
		public async Task<bool> TopUp(int walletId, decimal amount)
		{
			try
			{
				// 1. Lấy ví
				var wallet = await _walletRepositoriy.GetByIdAsync(walletId);
				if (wallet == null) return false;

				// 2. Cập nhật số dư
				wallet.Currency += amount;
				wallet.UpdatedAt = DateTime.UtcNow;

				// 3. Dùng hàm UpdateAsync đã có của bạn (vì nó tự SaveChanges)
				await UpdateAsync(wallet);
				return true;
			}
			catch (Exception ex)
			{
				// Ghi log lỗi ở đây
				throw;
			}
		}
	}
}
