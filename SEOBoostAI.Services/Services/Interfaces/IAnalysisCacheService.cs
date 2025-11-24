using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IAnalysisCacheService
    {
        Task<List<AnalysisCache>> GetAnalysisCachesAsync();
        Task<PaginationResult<List<AnalysisCache>>> GetAnalysisCachesWithPaginateAsync(int currentPage, int pageSize);
        Task<AnalysisCache> GetAnalysisCacheByIdAsync(int id);
        Task CreateAsync(AnalysisCache analysisCache);
        Task UpdateAsync(AnalysisCache analysisCache);
        Task DeleteAsync(int id);
        Task<AnalysisCache> AnalyzeInternalAsync(string url, string strategy);
        Task<AnalysisCache> ReAnalyzeInternalAsync(string url, string strategy);
        Task<AnalysisCache> GetOrCreateFreshAnalysisCacheAsync(string url, string strategy);
        Task<AnalysisResultModel> GetAnalysisResultAsync(int analysisCacheId);
    }
}
