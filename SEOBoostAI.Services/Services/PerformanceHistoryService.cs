using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class PerformanceHistoryService : IPerformanceHistoryService
    {
        private readonly IPerformanceHistoryRepository _performanceHistoryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAnalysisCacheService _analysisCacheService;
        private readonly IElementService _elementService;
        private readonly IUnitOfWork _unitOfWork;

        public PerformanceHistoryService(IPerformanceHistoryRepository performanceHistoryRepository, IUserRepository userRepository, IAnalysisCacheService analysisCacheService, IElementService elementService, IUnitOfWork unitOfWork)
        {
            _performanceHistoryRepository = performanceHistoryRepository;
            _userRepository = userRepository;
            _analysisCacheService = analysisCacheService;
            _elementService = elementService;
            _unitOfWork = unitOfWork;
        }

        public async Task<PaginationResult<List<PerformanceHistory>>> GetPerformanceHistorysWithPagination(int currentPage, int pageSize)
        {
            return await _performanceHistoryRepository.GetPerformanceHistorysWithPagination(currentPage, pageSize);
        }

        public async Task<PerformanceHistory> AnalysisPerformanceHistoryAsync(int userId, string url, string strategy)
        {
            if (await _analysisCacheService.CheckDuplicateUrl(url))
            {
                throw new Exception("URL already analyzed.");
            }

            var analysisCache = await _analysisCacheService.AnalyzeAndSaveAnalysisCacheAsync(url, strategy);

            var performanceHistory = new PerformanceHistory
            {
                UserID = userId,
                AnalysisCacheID = analysisCache.AnalysisCacheID,
                ScanTime = DateTime.UtcNow.AddHours(7)
            };

            try
            {
                await _performanceHistoryRepository.CreateAsync(performanceHistory);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true ||
                      ex.InnerException?.Message.Contains("duplicate key") == true)
            {
                // Bắt lỗi nếu CSDL báo "trùng lặp" (do race condition)
                throw new Exception($"URL already analyzed (race condition detected).");
            }
            catch (Exception)
            {
                throw;
            }

            return performanceHistory;
        }
    }
}
