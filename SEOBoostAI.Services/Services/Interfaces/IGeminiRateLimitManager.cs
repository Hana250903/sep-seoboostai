using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IGeminiRateLimitManager
    {
        /// <summary>
        /// Lấy một API key khả dụng. Nếu tất cả keys đều busy, sẽ chờ trong queue.
        /// </summary>
        Task<GeminiKey> GetAvailableKeyAsync();

        /// <summary>
        /// Ghi nhận usage sau khi thực hiện request thành công
        /// </summary>
        /// <param name="keyId">ID của key đã sử dụng</param>
        /// <param name="estimatedTokens">Số tokens ước tính đã sử dụng</param>
        Task RecordUsageAsync(int keyId, int estimatedTokens);

        /// <summary>
        /// Đánh dấu key bị rate limited (gặp 428/429 error)
        /// </summary>
        /// <param name="keyId">ID của key bị rate limited</param>
        Task MarkKeyRateLimitedAsync(int keyId);

        /// <summary>
        /// Reload danh sách keys từ database (dùng khi thêm/xóa key)
        /// </summary>
        Task ReloadKeysAsync();
    }
}
