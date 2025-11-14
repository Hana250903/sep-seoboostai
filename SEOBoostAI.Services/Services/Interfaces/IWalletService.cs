using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IWalletService
	{
		Task<List<Wallet>> GetWalletsAsync();
		Task<PaginationResult<List<Wallet>>> GetWalletsWithPaginateAsync(int currentPage, int pageSize);
		Task<Wallet> GetWalletByIdAsync(int id);
		Task CreateAsync(Wallet wallet);
		Task UpdateAsync(Wallet wallet);
		Task DeleteAsync(int id);

		// HÀM MỚI CHO PAYOS
		Task<Wallet> GetWalletByUserIdAsync(int userId);
		Task<bool> TopUp(int walletId, decimal amount);
	}
}
