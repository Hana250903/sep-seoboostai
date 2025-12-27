using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Feedbacks
{
    public class SpamProtectionService : ISpamProtectionService
    {
        private readonly ISpamCacheRepository _spamRepo;

        // BUSINESS RULE: Cấu hình chặn spam tại đây (ví dụ: 1.0 giây)
        private readonly TimeSpan _limitInterval = TimeSpan.FromMilliseconds(1000);

        public SpamProtectionService(ISpamCacheRepository spamRepo)
        {
            _spamRepo = spamRepo;
        }

        public bool IsUserSpamming(string userId)
        {
            var lastTime = _spamRepo.GetLastMessageTime(userId);
            var now = DateTime.UtcNow.AddHours(7);

            if (lastTime.HasValue)
            {
                // Nếu thời gian hiện tại - lần cuối < giới hạn => SPAM
                if ((now - lastTime.Value) < _limitInterval)
                {
                    return true; // Spam
                }
            }

            _spamRepo.SetLastMessageTime(userId, now);
            return false; // Not Spam
        }
    }
}
