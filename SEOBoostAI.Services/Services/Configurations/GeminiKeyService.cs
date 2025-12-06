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
            geminiKey.CreatedAt = DateTime.UtcNow;
            geminiKey.LastResetDate = DateTime.UtcNow.Date;
            geminiKey.RequestsUsedToday = 0;
            geminiKey.TokensUsedToday = 0;

            // Set default limits if not provided
            if (geminiKey.RpmLimit == 0)
                geminiKey.RpmLimit = 10;

            if (geminiKey.TpmLimit == 0)
                geminiKey.TpmLimit = 250000;

            if (geminiKey.RpdLimit == 0)
                geminiKey.RpdLimit = 250;

            await _geminiKeyRepository.CreateAsync(geminiKey);
            await _unitOfWork.SaveChangesAsync();

            await _geminiRateLimitManager.ReloadKeysAsync();

            return geminiKey;
        }

        public async Task UpdateKeyAsync(GeminiKey geminiKey)
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

        public async Task DeleteKeyAsync(int id)
        {
            var key = await _geminiKeyRepository.GetByIdAsync(id);
            if (key != null)
            {
                await _geminiKeyRepository.RemoveAsync(key);
                await _unitOfWork.SaveChangesAsync();

                // CẬP NHẬT: Xóa key khỏi RAM
                await _geminiRateLimitManager.ReloadKeysAsync();
            }
        }

        public async Task ToggleActiveAsync(int id)
        {
            var key = await _geminiKeyRepository.GetByIdAsync(id);
            if (key == null)
            {
                throw new KeyNotFoundException($"Gemini key với ID {id} không tồn tại");
            }

            key.IsActive = !key.IsActive;
            key.UpdatedAt = DateTime.UtcNow;
            await _geminiKeyRepository.UpdateAsync(key);
            await _unitOfWork.SaveChangesAsync();

            await _geminiRateLimitManager.ReloadKeysAsync();
        }

        public async Task ResetUsageAsync(int id)
        {
            var key = await _geminiKeyRepository.GetByIdAsync(id);
            if (key == null)
            {
                throw new KeyNotFoundException($"Gemini key với ID {id} không tồn tại");
            }

            await _geminiKeyRepository.UpdateKeyUsageAsync(
                id, 
                -(int)key.TokensUsedToday, 
                DateTime.UtcNow.Date
            );
            await _unitOfWork.SaveChangesAsync();

            await _geminiRateLimitManager.ReloadKeysAsync();
        }

        public async Task<object> GetUsageStatsAsync()
        {
            var keys = await _geminiKeyRepository.GetAllActiveKeysAsync();

            var stats = keys.Select(k => new
            {
                k.Id,
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
