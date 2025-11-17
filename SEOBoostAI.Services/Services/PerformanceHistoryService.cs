using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
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
        private readonly ILogger<PerformanceHistoryService> _logger;

        public PerformanceHistoryService(IPerformanceHistoryRepository performanceHistoryRepository, IUserRepository userRepository,
            IAnalysisCacheService analysisCacheService, IElementService elementService, IUnitOfWork unitOfWork, ILogger<PerformanceHistoryService> logger)
        {
            _performanceHistoryRepository = performanceHistoryRepository;
            _userRepository = userRepository;
            _analysisCacheService = analysisCacheService;
            _elementService = elementService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<PaginationResult<List<PerformanceHistory>>> GetPerformanceHistorysWithPagination(int currentPage, int pageSize, int? userId)
        {
            return await _performanceHistoryRepository.GetPerformanceHistorysWithPagination(currentPage, pageSize, userId);
        }

        public async Task<PerformanceHistory> GetPerformanceHistoryByIdAsync(int performanceHistoryId)
        {
            return await _performanceHistoryRepository.GetByIdAsync(performanceHistoryId);
        }

        public async Task<PerformanceHistory> AnalysisPerformanceHistoryAsync(int userId, string url, string strategy)
        {
            if (await _performanceHistoryRepository.CheckUserHasUrl(userId, url, strategy))
            {
                // Thay vì báo lỗi, có thể bạn nên trả về history cũ?
                // Hoặc báo lỗi là tùy logic của bạn.
                throw new Exception("User has already analyzed this URL.");
            }

            var analysisCache = await _analysisCacheService.GetOrCreateFreshAnalysisCacheAsync(url, strategy);

            var performanceHistory = new PerformanceHistory
            {
                UserID = userId,
                ScanTime = DateTime.UtcNow.AddHours(7),
                AnalysisCacheID = analysisCache.AnalysisCacheID // Liên kết với cache đã tìm/tạo
            };

            try
            {
                await _performanceHistoryRepository.CreateAsync(performanceHistory);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Có thể user bấm 2 lần rất nhanh
                _logger.LogWarning(ex, $"Lỗi khi tạo PerformanceHistory cho User {userId} và URL {url}");
                throw new Exception("Lỗi khi lưu lịch sử phân tích.");
            }

            performanceHistory.AnalysisCache = analysisCache;
            return performanceHistory;
        }

        public async Task<PerformanceHistory> ReAnalyzePerformanceHistoryAsync(int performanceHistoryId, int userId)
        {
            var existingPerformanceHistory = await _performanceHistoryRepository.GetByIdAsync(performanceHistoryId) ?? throw new Exception("Performance history not found.");

            if (existingPerformanceHistory.UserID != userId)
            {
                throw new UnauthorizedAccessException("User does not have permission for this item.");
            }

            if (existingPerformanceHistory.AnalysisCache == null)
            {
                // Điều này hiếm khi xảy ra nếu DB của bạn có Foreign Key constraint
                _logger.LogWarning($"Dữ liệu mồ côi (Orphan data): PerformanceHistory {performanceHistoryId} tham chiếu đến AnalysisCacheID không tồn tại.");
                throw new UnauthorizedAccessException("User does not have permission for this item.");
            }

            var updatedCache = await _analysisCacheService.ReAnalyzeAndSaveAnalysisCacheAsync(existingPerformanceHistory.AnalysisCache.Url,
                existingPerformanceHistory.AnalysisCache.Strategy);

            existingPerformanceHistory.ScanTime = DateTime.UtcNow.AddHours(7);
            await _performanceHistoryRepository.UpdateAsync(existingPerformanceHistory);
            await _unitOfWork.SaveChangesAsync();

            existingPerformanceHistory.AnalysisCache = updatedCache;

            return existingPerformanceHistory;
        }
    }
}
