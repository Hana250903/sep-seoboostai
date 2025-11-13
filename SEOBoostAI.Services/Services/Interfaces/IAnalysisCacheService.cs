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
        Task<AnalysisCache> AnalyzeAndSaveAnalysisCacheAsync(string url, string strategy);
        Task<bool> CheckDuplicateUrl(string url);
    }
}
