using SEOBoostAI.Repository.ModelExtensions;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    /// <summary>
    /// Interface cho GitHub Service - Debug và Fork-based PR
    /// </summary>
    public interface IGitHubIntegrationService
    {
        /// <summary>
        /// Kiểm tra cấu trúc repository để debug
        /// </summary>
        Task<RepoDebugInfo> InspectRepoAsync(string owner, string repo, string branch = null);

        /// <summary>
        /// Lấy nội dung file từ repository
        /// </summary>
        Task<string?> GetFileContentAsync(string owner, string repo, string path, string branch = "main");

        /// <summary>
        /// Tìm file chứa evidence (code snippet)
        /// </summary>
        Task<string?> FindFileByEvidenceAsync(string owner, string repo, string evidence);

        /// <summary>
        /// Tìm file theo URL path
        /// </summary>
        Task<string?> FindFileByUrlPathAsync(string owner, string repo, string urlPath);

        /// <summary>
        /// Auto-detect cấu trúc repo (monorepo, nextjs, vite, etc.)
        /// </summary>
        Task<RepoStructure> DetectRepoStructureAsync(string owner, string repo);

        /// <summary>
        /// Lấy cached structure (nếu có)
        /// </summary>
        RepoStructure? GetCachedStructure(string owner, string repo);

        /// <summary>
        /// Tạo batch PR (commit nhiều file)
        /// </summary>
        Task<string> CreateBatchPullRequestAsync(string owner, string repo, Dictionary<string, string> fileContents, string message);

        /// <summary>
        /// Fork repo rồi tạo cross-repo PR
        /// </summary>
        Task<string> ForkAndCreatePullRequestAsync(string owner, string repo, Dictionary<string, string> fileContents, string message);

        /// <summary>
        /// Lấy username của GitHub token owner (để so sánh với RepoOwner)
        /// </summary>
        Task<string> GetCurrentUserLoginAsync();
    }
}
