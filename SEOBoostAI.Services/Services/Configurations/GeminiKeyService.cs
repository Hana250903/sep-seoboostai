using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Configurations
{
    public class GeminiKeyService : IGeminiKeyService
    {
        private readonly IGeminiKeyRepository _geminiKeyRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGeminiRateLimitManager _geminiRateLimitManager;
        private readonly IEncryptionService _encryptionService;

        public GeminiKeyService(IGeminiKeyRepository geminiKeyRepository, IUnitOfWork unitOfWork, IGeminiRateLimitManager geminiRateLimitManager, IEncryptionService encryptionService)
        {
            _geminiKeyRepository = geminiKeyRepository;
            _unitOfWork = unitOfWork;
            _geminiRateLimitManager = geminiRateLimitManager;
            _encryptionService = encryptionService;
        }

        public async Task<IEnumerable<GeminiKey>> GetAllActiveKeysAsync()
        {
            return await _geminiKeyRepository.GetAllActiveKeysAsync();
        }

        public async Task<GeminiKey> GetKeyByIdAsync(int id)
        {
            return await _geminiKeyRepository.GetKeyByIdAsync(id);
        }

        public async Task<GeminiKey> CreateKeyAsync(GeminiKey geminiKey)
        {
            // Encrypt API key before saving to database
            if (!string.IsNullOrEmpty(geminiKey.ApiKey))
            {
                geminiKey.ApiKey = await _encryptionService.EncryptAsync(geminiKey.ApiKey);
            }

            // Set default values
            geminiKey.CreatedAt = DateTime.UtcNow.AddHours(7);
            geminiKey.LastResetDate = DateTime.UtcNow.AddHours(7).Date;
            geminiKey.RequestsUsedToday = 0;
            geminiKey.TokensUsedToday = 0;

            try
            {
                await _geminiKeyRepository.CreateAsync(geminiKey);
                await _unitOfWork.SaveChangesAsync();

                await _geminiRateLimitManager.ReloadKeysAsync();

                return geminiKey;
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi tạo Gemini key: " + ex.Message);
            }
        }

        public async Task UpdateKeyAsync(GeminiKey geminiKey)
        {
            try
            {
                // Encrypt API key if it's being updated
                if (!string.IsNullOrEmpty(geminiKey.ApiKey))
                {
                    geminiKey.ApiKey = await _encryptionService.EncryptAsync(geminiKey.ApiKey);
                }

                geminiKey.UpdatedAt = DateTime.UtcNow;
                await _geminiKeyRepository.UpdateAsync(geminiKey);
                await _unitOfWork.SaveChangesAsync();

                await _geminiRateLimitManager.ReloadKeysAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi cập nhật Gemini key: " + ex.Message);
            }
        }

        public async Task DeleteKeyAsync(int id)
        {
            try
            {
                var key = await _geminiKeyRepository.GetByIdAsync(id);
                if (key != null)
                {
                    await _geminiKeyRepository.RemoveAsync(key);
                    await _unitOfWork.SaveChangesAsync();

                    // CẬP NHẬT: Xóa key khỏi RAM
                    await _geminiRateLimitManager.ReloadKeysAsync();
                }
                else
                {
                    throw new KeyNotFoundException($"Gemini key với ID {id} không tồn tại");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi xóa Gemini key: " + ex.Message);
            }
        }

        public async Task ToggleActiveAsync(int id)
        {
            try
            {
                var key = await _geminiKeyRepository.GetByIdAsync(id);
                if (key == null)
                {
                    throw new KeyNotFoundException($"Gemini key với ID {id} không tồn tại");
                }

                key.IsActive = !key.IsActive;
                key.UpdatedAt = DateTime.UtcNow.AddHours(7);
                await _geminiKeyRepository.UpdateAsync(key);
                await _unitOfWork.SaveChangesAsync();

                await _geminiRateLimitManager.ReloadKeysAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi chuyển đổi trạng thái hoạt động của Gemini key: " + ex.Message);
            }
        }

        public async Task ResetUsageAsync(int id)
        {
            try
            {
                var key = await _geminiKeyRepository.ExistsAsync(id);
                if (!key)
                {
                    throw new KeyNotFoundException($"Gemini key với ID {id} không tồn tại");
                }

                await _geminiKeyRepository.ResetKeySpecificUsageAsync(id);

                await _geminiRateLimitManager.ReloadKeysAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Đã xảy ra lỗi khi đặt lại việc sử dụng Gemini key: " + ex.Message);
            }
        }

        public async Task<object> GetUsageStatsAsync()
        {
            var keys = await _geminiKeyRepository.GetAllActiveKeysAsync();

            foreach (var key in keys)
            {
                // Decrypt API key for display purposes
                if (!string.IsNullOrEmpty(key.ApiKey))
                {
                    key.ApiKey = await _encryptionService.DecryptAsync(key.ApiKey);
                }
            }

            var stats = keys.Select(async k => new
            {
                k.Id,
                k.ApiKey,
                k.KeyName,
                k.IsActive,
                RpmUsage = $"{k.RequestsUsedToday}/{k.RpmLimit}",
                TpmUsage = $"{k.TokensUsedToday}/{k.TpmLimit}",
                RpdUsage = $"{k.RequestsUsedToday}/{k.RpdLimit}",
                RpmPercentage = k.RpmLimit > 0 ? (k.RequestsUsedToday * 100.0 / k.RpmLimit) : 0,
                TpmPercentage = k.TpmLimit > 0 ? (k.TokensUsedToday * 100.0 / k.TpmLimit) : 0,
                RpdPercentage = k.RpdLimit > 0 ? (k.RequestsUsedToday * 100.0 / k.RpdLimit) : 0,
                k.LastResetDate,
                IsRateLimited = k.RateLimitedUntil.HasValue && k.RateLimitedUntil > DateTime.UtcNow
            });

            return stats;
        }
    }
}
