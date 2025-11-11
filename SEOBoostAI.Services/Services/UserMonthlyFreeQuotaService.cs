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

namespace SEOBoostAI.Service.Services
{
	public class UserMonthlyFreeQuotaService : IUserMonthlyFreeQuotaService
	{
		private readonly IUserMonthlyFreeQuotaRepository _userMonthlyFreeQuotaRepository;
		private readonly IUnitOfWork _unitOfWork;
        private readonly IFeatureRepository _featureRepository;

        public UserMonthlyFreeQuotaService(IUserMonthlyFreeQuotaRepository userMonthlyFreeQuotaRepository, IUnitOfWork unitOfWork, IFeatureRepository featureRepository)
		{
			_userMonthlyFreeQuotaRepository = userMonthlyFreeQuotaRepository;
			_unitOfWork = unitOfWork;
            _featureRepository = featureRepository;
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

        public async Task<int> CreateQuotaAsync(int userId)
        {
			try
			{
                var userMonthlyFreeQuotas = await _userMonthlyFreeQuotaRepository.CreateAsync(userId);
                await _userMonthlyFreeQuotaRepository.CreateRangeAsync(userMonthlyFreeQuotas);
                var result = await _unitOfWork.SaveChangesAsync();
                return result;
            }
			catch (Exception ex)
			{
				throw;
			}
        }

		public async Task<int> UpdateMonthQuotaAsync(int userId)
		{
			try
			{
				var userMonthlyFreeQuotas = await _userMonthlyFreeQuotaRepository.GetQuotasByUserId(userId);

                foreach (var userMonthlyFreeQuota in userMonthlyFreeQuotas)
                {
					var checkMonthly = CheckMonthly(userMonthlyFreeQuota.MonthYear);
					if (checkMonthly)
					{
						var currentMonth = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM");
						userMonthlyFreeQuota.MonthYear = currentMonth;
                        userMonthlyFreeQuota.UsageCount = 0;
                        await _userMonthlyFreeQuotaRepository.UpdateAsync(userMonthlyFreeQuota);
					}
					else
					{
						continue;
					}
                }
				var result = await _unitOfWork.SaveChangesAsync();
				return result;
                
            }
			catch (Exception ex)
			{
				throw;
			}
		}

		private bool CheckMonthly(string monthYear)
		{
            DateTime startOfTargetMonth = DateTime.Parse(monthYear + "-01");
            DateTime startOfNextMonth = startOfTargetMonth.AddMonths(1);

            if (DateTime.Now >= startOfNextMonth)
            {
				return true;
            }
            else
            {
				return false;
            }
        }
		public async Task<bool> CheckLimit(int userId, int featureId)
		{
			var userQuota = await _userMonthlyFreeQuotaRepository.GetQuotaByUserIdAndFeatureId(userId, featureId);
			if (userQuota != null)
			{
				if (userQuota.UsageCount < userQuota.MonthlyLimit)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else
			{
				return false;
            }
        }
		public async Task<int> IncrementUsageCount(int userId, int featureId)
		{
			var userQuota = await _userMonthlyFreeQuotaRepository.GetQuotaByUserIdAndFeatureId(userId, featureId);
			if (userQuota != null)
			{
				userQuota.UsageCount += 1;
				await _userMonthlyFreeQuotaRepository.UpdateAsync(userQuota);
				var result = await _unitOfWork.SaveChangesAsync();
				return result;
			}
			else
			{
				return 0;
            }
        }
    }
}
