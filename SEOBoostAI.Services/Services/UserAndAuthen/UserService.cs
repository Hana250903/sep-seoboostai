using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Repository.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.UserAndAuthen
{
	using SEOBoostAI.Repository.Enums;
	public class UserService : IUserService
	{
		private readonly IUserRepository _userRepository;
		private readonly ITransactionRepository _transactionRepository;
		private readonly ISystemConfigService _systemConfigService;
		private readonly IUnitOfWork _unitOfWork;
		private readonly string _VAT_RATE;


		public UserService(IUserRepository userRepository, ITransactionRepository transactionRepository, ISystemConfigService systemConfigService, IUnitOfWork unitOfWork)
		{
			_userRepository = userRepository;
			_transactionRepository = transactionRepository;
			_systemConfigService = systemConfigService;
			_unitOfWork = unitOfWork;
			_VAT_RATE = _systemConfigService.GetValue<string>("VAT", "");
		}

		public async Task CreateAsync(User user)
		{
			try
			{
				await _userRepository.CreateAsync(user);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task<List<User>> GetUsersAsync()
		{
			return await _userRepository.GetAllAsync();
		}

		public async Task UpdateAsync(User user)
		{
			try
			{
				await _userRepository.UpdateAsync(user);
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
				var user = await _userRepository.GetByIdAsync(id);
				await _userRepository.RemoveAsync(user);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task<User> GetUserByIdAsync(int id)
		{
			return await _userRepository.GetByIdAsync(id);
		}

		public async Task<PaginationResult<List<User>>> GetUsersWithPaginateAsync(int currentPage, int pageSize, string? role, bool? isBanned, bool? isDeleted)
		{
			return await _userRepository.GetUserWithPaginateAsync(currentPage, pageSize, role, isBanned, isDeleted);
		}

		public async Task<User> UpdateUserToStaff(int userId)
		{
			var user = await _userRepository.GetByIdAsync(userId);
			if (user == null)
			{
				throw new Exception("User not found");
			}
			user.Role = UserRole.Staff.ToString();
			user.UpdatedAt = DateTime.UtcNow;
			await _userRepository.UpdateAsync(user);
			await _unitOfWork.SaveChangesAsync();
			return user;
		}

		public async Task<List<User>> BanAndUnbanUser(List<int> listUserId)
		{
			var listUser = await _userRepository.GetUsersByIdsAsync(listUserId);
			if (listUser == null)
			{
				throw new Exception("User not found");
			}

			List<User> users = new List<User>();
			foreach (var user in listUser)
			{
				user.IsBanned = !user.IsBanned;
				user.UpdatedAt = DateTime.UtcNow;
				users.Add(user);
			}
			await _userRepository.UpdateRangeAsync(users);
			await _unitOfWork.SaveChangesAsync();
			return listUser;
		}

		public async Task TopUpAsync(int userId, decimal amount, int transactionId)
		{
			decimal taxAmount = amount * (decimal.Parse(_VAT_RATE) / 100m);
			decimal netAmountToWallet = amount - taxAmount;

			var user = await _userRepository.GetByIdAsync(userId);
			if (user == null) throw new Exception("User không tồn tại");

			//Cộng tiền trực tiếp vào User
			user.Currency += netAmountToWallet;
			await _userRepository.UpdateAsync(user);

			//Cập nhật số dư cuối cùng vào Transaction
			var transaction = await _transactionRepository.GetByIdAsync(transactionId);
			if (transaction != null)
			{
				transaction.BalanceAfter = user.Currency;
				transaction.Description = $"{transaction.Description} (VAT {_VAT_RATE}%: {taxAmount:N0}đ)";
				await _transactionRepository.UpdateAsync(transaction);
			}

			await _unitOfWork.SaveChangesAsync();
		}

		public async Task<bool> DeductBalanceAsync(int userId, decimal amount)
		{
			try
			{
				//Tìm User
				var user = await _userRepository.GetByIdAsync(userId);
				if (user == null) return false;

				//Kiểm tra số dư
				if (user.Currency < amount)
				{
					return false; // Không đủ tiền
				}

				//Trừ tiền
				user.Currency -= amount;

				await _userRepository.UpdateAsync(user);
				await _unitOfWork.SaveChangesAsync();

				return true; // Trừ tiền thành công
			}
			catch (Exception)
			{
				return false; // Lỗi hệ thống
			}
		}
	}
}
