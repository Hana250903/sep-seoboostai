using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories.Interfaces
{
    public interface ISpamCacheRepository
    {
        // Lấy thời gian nhắn tin lần cuối của user
        DateTime? GetLastMessageTime(string userId);

        // Lưu thời gian nhắn tin mới nhất
        void SetLastMessageTime(string userId, DateTime time);
    }
}
