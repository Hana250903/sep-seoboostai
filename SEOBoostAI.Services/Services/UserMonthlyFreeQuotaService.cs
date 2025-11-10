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
	public class UserMonthlyFreeQuotaService : IUserMonthlyFreeQuotaService
	{
		private readonly IUserMonthlyFreeQuotaRepository _userMonthlyFreeQuotaRepository;
		private readonly IUnitOfWork _unitOfWork;

		public UserMonthlyFreeQuotaService(IUserMonthlyFreeQuotaRepository userMonthlyFreeQuotaRepository, IUnitOfWork unitOfWork)
		{
			_userMonthlyFreeQuotaRepository = userMonthlyFreeQuotaRepository;
			_unitOfWork = unitOfWork;
		}

		public async Task<PaginationResult<List<UserMonthlyFreeQuota>>> GetUserMonthlyFreeQuotasWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _userMonthlyFreeQuotaRepository.GetUserMonthlyFreeQuotasWithPaginateAsync(currentPage, pageSize);
		}

		public async Task<UserMonthlyFreeQuota> GetUserMonthlyFreeQuotaByIdAsync(int id)
		{
			return await _userMonthlyFreeQuotaRepository.GetByIdAsync(id);
		}

		public async Task<List<UserMonthlyFreeQuota>> GetUserMonthlyFreeQuotasAsync()
		{
			return await _userMonthlyFreeQuotaRepository.GetAllAsync();
		}

		public async Task CreateAsync(UserMonthlyFreeQuota userMonthlyFreeQuota)
		{
			try
			{
				await _userMonthlyFreeQuotaRepository.CreateAsync(userMonthlyFreeQuota);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task UpdateAsync(UserMonthlyFreeQuota userMonthlyFreeQuota)
		{
			try
			{
				_userMonthlyFreeQuotaRepository.UpdateAsync(userMonthlyFreeQuota);
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
				var userMonthlyFreeQuota = await _userMonthlyFreeQuotaRepository.GetByIdAsync(id);
				await _userMonthlyFreeQuotaRepository.RemoveAsync(userMonthlyFreeQuota);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}
	}
}
