using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.ModelExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface ITrendSearchService
    {
        Task<TrendAnalysisResponseDto> AnalyzeTrendQueryAsync(int memberId, string originalQuestion, int featureID);
        Task<List<AdsPlannerItemDto>> GetAdsKeywordsDetailAsync(
            int queryHistoryId,
            int currentUserId,
            bool onlySuggestions = false);
        Task<PaginationResult<List<QueryHistory>>> GetQueryHistoriesAsync(int memberId, int currentPage, int pageSize);
    }
}
