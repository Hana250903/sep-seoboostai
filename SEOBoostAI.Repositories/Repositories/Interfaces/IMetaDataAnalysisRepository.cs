using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories.Interfaces
{
    public interface IMetaDataAnalysisRepository : IGenericRepository<MetaDataAnalysis>
    {
        Task<MetaDataAnalysis> GetMetaDataAnalysisAsync(int id);
        Task DeleteMetaDataAnalysesForCacheAsync(int analysisCacheId);
        Task<MetaDataAnalysis> GetByAnalysisCacheIdAsync(int analysisCacheId);
        Task<List<MetaDataAnalysis>> GetAllMetaDataAnalysesAsync();
    }
}
