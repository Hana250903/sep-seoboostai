using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IPerformanceHistoryService
    {
        Task<PaginationResult<List<PerformanceHistory>>> GetPerformanceHistorysWithPagination(int currentPage, int pageSize);
        Task<PerformanceHistory> AnalysisPerformanceHistoryAsync(int userId, string url, string strategy);
    }
}
