using SEOBoostAI.Repository.ModelExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IAdsPlannerService
    {
        // Hàm này nhận danh sách từ khóa (ví dụ: ["phở", "cơm tấm"])
        // Và trả về danh sách chi tiết từ Google Ads
        Task<List<AdsPlannerItemDto>> GetAdsDataAsync(List<string> keywords);
    }
}
