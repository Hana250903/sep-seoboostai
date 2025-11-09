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

namespace SEOBoostAI.Service.Services
{
    public class SystemConfigService : ISystemConfigService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ConcurrentDictionary<string, string> _settingsCache;

        public SystemConfigService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _settingsCache = new ConcurrentDictionary<string, string>();

            LoadAllSettings();
        }

        private void LoadAllSettings()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();

                var allSettings = configRepo.GetAllAsync().Result;

                foreach (var setting in allSettings)
                {
                    _settingsCache[setting.SettingKey] = setting.SettingValue;
                }
            }
        }

        public T GetValue<T>(string key, T defaultValue)
        {
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

        public async Task UpdateValueAsync(string key, string newValue)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var configRepo = scope.ServiceProvider.GetRequiredService<ISystemConfigRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var setting = await configRepo.GetByIdAsync(key);

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
                        LastUpdatedDate = DateTime.UtcNow
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
    }
}
