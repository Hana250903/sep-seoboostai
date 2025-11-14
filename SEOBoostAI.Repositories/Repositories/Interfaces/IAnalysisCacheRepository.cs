using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories.Interfaces
{
    public interface IAnalysisCacheRepository : IGenericRepository<AnalysisCache>
    {
        Task<PaginationResult<List<AnalysisCache>>> GetAnalysisCachesWithPaginateAsync(int currentPage, int pageSize);
        Task<AnalysisCache> GetAnalysisCacheAsync(int id);
        Task<bool> IsDuplicateAsync(string normalizedUrlToCheck);
    }
}
