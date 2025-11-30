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
		private readonly IPurchasedFeatureRepository _purchasedFeatureRepository;
		private readonly IUnitOfWork _unitOfWork;
        private readonly IFeatureRepository _featureRepository;

        public UserMonthlyFreeQuotaService(IUserMonthlyFreeQuotaRepository userMonthlyFreeQuotaRepository, 
			IUnitOfWork unitOfWork, IFeatureRepository featureRepository, IPurchasedFeatureRepository purchasedFeatureRepository)
		{
			_userMonthlyFreeQuotaRepository = userMonthlyFreeQuotaRepository;
			_unitOfWork = unitOfWork;
            _featureRepository = featureRepository;
			_purchasedFeatureRepository = purchasedFeatureRepository;
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
                await _userMonthlyFreeQuotaRepository.CreateAsync(userId);
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
			// 1. KIỂM TRA LƯỢT FREE TRƯỚC
			var userQuota = await _userMonthlyFreeQuotaRepository.GetQuotaByUserIdAndFeatureId(userId, featureId);

			// Nếu có quota free VÀ chưa dùng hết
			if (userQuota != null && userQuota.UsageCount < userQuota.MonthlyLimit)
			{
				return true; // Được phép dùng (xài lượt free)
			}

			// 2. NẾU HẾT FREE -> KIỂM TRA GÓI ĐÃ MUA (PurchasedFeatures)
			var availablePack = await _purchasedFeatureRepository.GetAvailablePackAsync(userId, featureId);

			if (availablePack != null)
			{
				return true; // Được phép dùng (xài lượt mua)
			}

			// 3. Hết cả hai -> Chặn
			return false;
		}

		public async Task IncrementUsageCount(int userId, int featureId)
		{
			// 1. ƯU TIÊN TRỪ LƯỢT FREE
			var userQuota = await _userMonthlyFreeQuotaRepository.GetQuotaByUserIdAndFeatureId(userId, featureId);

			if (userQuota != null && userQuota.UsageCount < userQuota.MonthlyLimit)
			{
				userQuota.UsageCount += 1;
				userQuota.LastUsedAt = DateTime.UtcNow.AddHours(7);
				await _userMonthlyFreeQuotaRepository.UpdateAsync(userQuota);
				// Lưu ý: SaveChangesAsync sẽ được gọi ở ContentOptimizationService
				return;
			}

			// 2. NẾU HẾT FREE -> TRỪ LƯỢT MUA
			var availablePack = await _purchasedFeatureRepository.GetAvailablePackAsync(userId, featureId);

			if (availablePack != null)
			{
				availablePack.RemainingQuantity -= 1;
				// Nếu muốn track ngày dùng cuối của gói này, cần thêm cột LastUsedAt vào PurchasedFeature

				await _purchasedFeatureRepository.UpdateAsync(availablePack);
				return;
			}

			// Nếu chạy đến đây nghĩa là logic CheckLimit và Increment không đồng bộ
			throw new InvalidOperationException("Không tìm thấy lượt sử dụng khả dụng (Lỗi hệ thống).");
		}

		public async Task<List<UserQuotaDto>> GetUserQuotaInfoAsync(int userId)
		{
			var result = new List<UserQuotaDto>();

			// 1. Lấy tất cả Feature (để đảm bảo hiển thị đủ các tính năng)
			var features = await _featureRepository.GetAllAsync();

			// 2. Lấy Quota Free hiện tại của User
			var currentMonth = DateTime.UtcNow.AddHours(7).ToString("yyyy-MM");

			var freeQuotas = await _userMonthlyFreeQuotaRepository.GetQuotasByUserId(userId);

			foreach (var feature in features)
			{
				var dto = new UserQuotaDto
				{
					FeatureId = feature.FeatureID,
					FeatureName = feature.Name,
					FreeLimit = 0,
					FreeUsage = 0,
					PaidRemaining = 0
				};

				// --- TÍNH TOÁN FREE ---
				var userFreeQuota = freeQuotas.FirstOrDefault(q => q.FeatureID == feature.FeatureID && q.MonthYear == currentMonth);
				if (userFreeQuota != null)
				{
					dto.FreeLimit = userFreeQuota.MonthlyLimit;
					dto.FreeUsage = userFreeQuota.UsageCount;
				}

				var totalPaidRemaining = await _purchasedFeatureRepository.GetTotalRemainingByFeatureAsync(userId, feature.FeatureID);
				dto.PaidRemaining = totalPaidRemaining;

				result.Add(dto);
			}

			return result;
		}
	}
}
