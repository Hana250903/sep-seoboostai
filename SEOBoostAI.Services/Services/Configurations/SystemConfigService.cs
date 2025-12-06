using Microsoft.Extensions.DependencyInjection;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Configurations
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ConcurrentDictionary<string, string> _settingsCache;

        private bool _isLoaded = false;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public SystemConfigService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            _settingsCache = new ConcurrentDictionary<string, string>();
        }

        // Hàm này đảm bảo data luôn được load trước khi lấy
        private async Task EnsureLoadedAsync()
        {
            if (_isLoaded) return;

            await _lock.WaitAsync();
            try
            {
                if (_isLoaded) return; // Double-check locking

                using (var scope = _scopeFactory.CreateScope())
                {
                    var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                    var allSettings = await configRepo.GetAllAsync();

                    foreach (var setting in allSettings)
                    {
                        _settingsCache[setting.SettingKey] = setting.SettingValue;
                    }
                    _isLoaded = true;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public T GetValue<T>(string key, T defaultValue)
        {
            // Nếu bắt buộc giữ nguyên signature Sync:
            if (!_isLoaded)
            {
                // Chạy async task để load nếu chưa có
                Task.Run(async () => await EnsureLoadedAsync()).Wait();
            }

            if (_settingsCache.TryGetValue(key, out var valueAsString))
            {
                try
                {
                    return (T)Convert.ChangeType(valueAsString, typeof(T));
                }
                catch
                {
                    return defaultValue;
                }
            }
            return defaultValue;
        }

        public async Task UpdateValueAsync(string key, string newValue, int? featureID)
        {
            await EnsureLoadedAsync();

            using (var scope = _scopeFactory.CreateScope())
            {
                var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var setting = await configRepo.GetByKeyAsync(key);

                if (setting != null)
                {
                    setting.SettingValue = newValue;
                    setting.LastUpdatedDate = DateTime.UtcNow;
                    await configRepo.UpdateAsync(setting);
                }
                else
                {
                    var newSetting = new SystemSetting
                    {
                        SettingKey = key,
                        SettingValue = newValue,
                        LastUpdatedDate = DateTime.UtcNow,
                        FeatureID = featureID == 0 ? null : featureID,
                    };
                    await configRepo.CreateAsync(newSetting);
                }

                await unitOfWork.SaveChangesAsync();

                _settingsCache.AddOrUpdate(key, newValue, (k, oldValue) => newValue);
            }
        }

        public Dictionary<string, string> GetAllSettings()
        {
            return new Dictionary<string, string>(_settingsCache);
        }

        public async Task<List<SystemSetting>> GetAllSettingsByFeatureIDAsync(int? featureID)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                return await configRepo.GetAllSystemSettingsByFeatureIDAsync(featureID);
            }
        }
    }
}
