using Microsoft.Extensions.Caching.Memory;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
    public class SpamCacheRepository : ISpamCacheRepository
    {
        private readonly IMemoryCache _memoryCache;

        public SpamCacheRepository(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public DateTime? GetLastMessageTime(string userId)
        {
            string key = $"spam_check_{userId}";
            if (_memoryCache.TryGetValue(key, out DateTime lastTime))
            {
                return lastTime;
            }
            return null;
        }

        public void SetLastMessageTime(string userId, DateTime time)
        {
            string key = $"spam_check_{userId}";
            // Lưu trong cache 1 phút thôi cho nhẹ RAM
            _memoryCache.Set(key, time, TimeSpan.FromMinutes(1));
        }
    }
}
