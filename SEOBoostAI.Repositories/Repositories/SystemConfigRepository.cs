using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
    public class SystemConfigRepository : GenericRepository<SystemSetting>, ISystemConfigRepository
    {
        public SystemConfigRepository(SEP_SEOBoostAIContext conent) : base(conent) { }

        public async Task<List<SystemSetting>> GetAllSystemSettingsByFeatureIDAsync(int? featureID)
        {
            return await _context.SystemSettings
                .Where(s => s.FeatureID == featureID)
                .ToListAsync();
        }

        public async Task<SystemSetting?> GetByKeyAsync(string key)
        {
            return await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.SettingKey == key);
        }
    }
}
