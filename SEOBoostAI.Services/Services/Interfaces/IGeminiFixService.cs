using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    /// <summary>
    /// Interface cho Gemini Fix Service - gọi AI để fix code
    /// </summary>
    public interface IGeminiFixService
    {
        /// <summary>
        /// Gọi Gemini AI để fix code dựa trên issue
        /// </summary>
        /// <param name="code">Code cần fix</param>
        /// <param name="errorTitle">Tiêu đề lỗi</param>
        /// <param name="issueKey">Key của issue (vd: meta-missing-desc)</param>
        /// <returns>Code đã được fix</returns>
        Task<string> FixCodeAsync(string code, string errorTitle, string issueKey);
    }
}
