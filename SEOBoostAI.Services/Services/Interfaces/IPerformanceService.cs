using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IPerformanceService
    {
        Task<List<Performance>> GetPerformancesAsync();
        Task<PaginationResult<List<Performance>>> GetPerformancesWithPaginateAsync(int currentPage, int pageSize);
        Task<Performance> GetPerformanceByIdAsync(int id);
        Task CreateAsync(Performance performance);
        Task UpdateAsync(Performance performance);
        Task DeleteAsync(int id);
        Task<Performance> AnalyzeAndSavePerformanceAsync(int userId, string url, string strategy);
    }
}
