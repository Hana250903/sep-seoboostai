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
	}
}
