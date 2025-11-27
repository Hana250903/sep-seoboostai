using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface ITrendSearchService
    {
        // Đây là phương thức nghiệp vụ chính, thực hiện toàn bộ workflow
        Task<QueryHistory> AnalyzeTrendQueryAsync(int memberId, string originalQuestion);
        // Thêm vào Interface
        Task<List<AdsPlannerItemDto>> GetAdsKeywordsDetailAsync(int queryHistoryId, int currentUserId);

        Task<PaginationResult<List<QueryHistory>>> GetQueryHistoriesAsync(int memberId, int currentPage, int pageSize);
    }
}
