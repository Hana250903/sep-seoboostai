using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface ISpamProtectionService
    {
        // Trả về true nếu là spam, false nếu hợp lệ
        bool IsUserSpamming(string userId);
    }
}
