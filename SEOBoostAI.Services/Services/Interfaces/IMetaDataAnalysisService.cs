using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IMetaDataAnalysisService
    {
        /// <summary>
        /// Phân tích metadata của URL với AI và lưu kết quả vào database
        /// </summary>
        Task<MetaDataAnalysis> AnalyzeMetaDataAsync(int analysisCacheId);

        /// <summary>
        /// Lấy MetaDataAnalysis theo ID kèm suggestions
        /// </summary>
        Task<MetaDataAnalysis> GetMetaDataAnalysisWithIdAsync(int id);

        /// <summary>
        /// Lấy latest AI suggestions cho một MetaDataAnalysis
        /// </summary>
        Task<MetaDataSuggestion> GetLatestSuggestionAsync(int metaDataAnalysisId);

        Task<MetaDataAnalysis> GetMetaDataAnalysisByAnalysisCacheIdAsync(int analysisCacheId);
        Task<List<MetaDataAnalysis>> GetAllMetaDataAnalysesAsync();
    }
}
