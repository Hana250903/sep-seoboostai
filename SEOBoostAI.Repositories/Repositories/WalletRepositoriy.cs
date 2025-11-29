using Microsoft.EntityFrameworkCore;
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
	public class WalletRepositoriy : GenericRepository<Wallet>, IWalletRepository
	{
		public WalletRepositoriy(SEP_SEOBoostAIContext context) : base(context) { }
		public async Task<PaginationResult<List<Wallet>>> GetWalletsWithPaginateAsync(int currentPage, int pageSize)
		{
			var query = _context.Set<Wallet>().AsQueryable();
			var totalItems = await query.CountAsync();
			var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
			var wallets = await query
			.Skip((currentPage - 1) * pageSize)
			.Take(pageSize)
			.Select(w => new Wallet
			{
				WalletID = w.WalletID,
				UserID = w.UserID,
				Currency = w.Currency,
				CreatedAt = w.CreatedAt,
				UpdatedAt = w.UpdatedAt,
				IsDeleted = w.IsDeleted,

				Transactions = w.Transactions,

				User = new User
				{
					FullName = w.User.FullName,
					Email = w.User.Email,
					Role = w.User.Role
				}
			})
			.ToListAsync();

			var result = new PaginationResult<List<Wallet>>
			{
				TotalItems = totalItems,
				TotalPages = totalPages,
				CurrentPage = currentPage,
				PageSize = pageSize,
				Items = wallets
			};
			return result;
		}

		public async Task<Wallet> GetWalletByUserIdAsync(int userId)
		{
			return await _context.Set<Wallet>()
				.Where(w => w.UserID == userId)
				.Select(w => new Wallet
				{
					WalletID = w.WalletID,
					UserID = w.UserID,
					Currency = w.Currency,
					CreatedAt = w.CreatedAt,
					UpdatedAt = w.UpdatedAt,
					IsDeleted = w.IsDeleted,

					Transactions = w.Transactions,

					User = new User
					{
						FullName = w.User.FullName,
						Email = w.User.Email,
						Role = w.User.Role
					}
				})
				.FirstOrDefaultAsync();
		}
	}
}
