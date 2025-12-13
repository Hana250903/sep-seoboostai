using SEOBoostAI.Repository.Models;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    /// <summary>
    /// Interface cho Puppeteer Audit Service
    /// </summary>
    public interface IPuppeteerAuditService
    {
        /// <summary>
        /// Chạy audit SEO/Performance và lưu vào database
        /// </summary>
        /// <param name="url">URL cần audit</param>
        /// <param name="strategy">mobile hoặc desktop</param>
        /// <returns>List<Element></returns>
        Task<List<Element>> RunAuditAsync(string url, string strategy = "desktop");

        /// <summary>
        /// Debug scan - xem Puppeteer đọc được gì từ trang (không lưu DB)
        /// </summary>
        Task<object> DebugScanAsync(string url);
    }
}
