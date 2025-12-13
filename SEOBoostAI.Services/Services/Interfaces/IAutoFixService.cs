using SEOBoostAI.Repository.ModelExtensions;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    /// <summary>
    /// Interface cho AutoFix Service - batch fix issues
    /// </summary>
    public interface IAutoFixService
    {
        /// <summary>
        /// Batch fix tất cả issues từ một AnalysisCache
        /// </summary>
        Task<BatchFixResponse> BatchFixAsync(BatchFixRequest request);

        /// <summary>
        /// Preview issues - xem trước file nào chứa issue nào
        /// </summary>
        Task<PreviewIssuesResponse> PreviewIssuesAsync(PreviewIssuesRequest request);
    }
}
