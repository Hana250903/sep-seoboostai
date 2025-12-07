using Google.Apis.Discovery;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Ultils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    public class PerformanceHistoryService : IPerformanceHistoryService
    {
        private readonly IPerformanceHistoryRepository _performanceHistoryRepository;
        private readonly IUserRepository _userRepository;
        private readonly IAnalysisCacheService _analysisCacheService;
        private readonly IElementService _elementService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PerformanceHistoryService> _logger;
        private readonly ICompareUrlString _compareUrlString;
        private readonly IUserMonthlyFreeQuotaService _userMonthlyFreeQuotaService;

        public PerformanceHistoryService(IPerformanceHistoryRepository performanceHistoryRepository, IUserRepository userRepository,
            IAnalysisCacheService analysisCacheService, IElementService elementService, IUnitOfWork unitOfWork, ILogger<PerformanceHistoryService> logger,
            ICompareUrlString compareUrlString, IUserMonthlyFreeQuotaService userMonthlyFreeQuotaService)
        {
            _performanceHistoryRepository = performanceHistoryRepository;
            _userRepository = userRepository;
            _analysisCacheService = analysisCacheService;
            _elementService = elementService;
            _unitOfWork = unitOfWork;
            _logger = logger;
            _compareUrlString = compareUrlString;
            _userMonthlyFreeQuotaService = userMonthlyFreeQuotaService;
        }

        public async Task<PaginationResult<List<PerformanceHistory>>> GetPerformanceHistorysWithPagination(int currentPage, int pageSize, int? userId)
        {
            return await _performanceHistoryRepository.GetPerformanceHistorysWithPagination(currentPage, pageSize, userId);
        }

        public async Task<PerformanceHistory> GetPerformanceHistoryByIdAsync(int performanceHistoryId)
        {
            return await _performanceHistoryRepository.GetByIdAsync(performanceHistoryId);
        }

        public async Task<PerformanceHistory> AnalysisPerformanceHistoryAsync(int userId, string url, string strategy, int featureId)
        {
            string normalizedUrl = _compareUrlString.NormalizeUrlForComparison(url);
            if (string.IsNullOrEmpty(normalizedUrl))
            {
                throw new Exception("URL không hợp lệ.");
            }

            if (await _performanceHistoryRepository.CheckUserHasUrl(userId, normalizedUrl, strategy))
            {
                return await _performanceHistoryRepository.GetByUserIdAndUrlAsync(userId, normalizedUrl, strategy);
            }

            bool canAnalyze = await _userMonthlyFreeQuotaService.CheckLimit(userId, featureId);
            if (!canAnalyze)
            {
                throw new Exception("Bạn đã hết lượt sử dụng miễn phí cho tính năng này trong tháng.");
            }

            var analysisCache = await _analysisCacheService.GetOrCreateFreshAnalysisCacheAsync(normalizedUrl, strategy);

            var performanceHistory = new PerformanceHistory
            {
                UserID = userId,
                ScanTime = DateTime.UtcNow
            };

            if (analysisCache.AnalysisCacheID > 0)
            {
                performanceHistory.AnalysisCacheID = analysisCache.AnalysisCacheID;
            }   
            else
            {
                performanceHistory.AnalysisCache = analysisCache;
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Check limit lần 2(Hard check - Quan trọng để chống hack / spam click)
                if (!await _userMonthlyFreeQuotaService.CheckLimit(userId, featureId))
                {
                    throw new Exception("Bạn vừa hết lượt sử dụng.");
                }

                await _performanceHistoryRepository.CreateAsync(performanceHistory);
                await _userMonthlyFreeQuotaService.IncrementUsageCount(userId, featureId);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                // Có thể user bấm 2 lần rất nhanh
                _logger.LogWarning(ex, $"Lỗi khi tạo PerformanceHistory cho User {userId} và URL {url}");
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception("Lỗi khi lưu lịch sử phân tích.");
            }

            performanceHistory.AnalysisCache ??= analysisCache;
            return performanceHistory;
        }

        public async Task<PerformanceHistory> ReAnalyzePerformanceHistoryAsync(int performanceHistoryId, int userId, int featureId)
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

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var updatedCache = await _analysisCacheService.ReAnalyzeInternalAsync(
                    existingPerformanceHistory.AnalysisCache.Url,
                    existingPerformanceHistory.AnalysisCache.Strategy
                );

                await _performanceHistoryRepository.UpdateScanTimeAsync(performanceHistoryId);

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                existingPerformanceHistory.AnalysisCache = updatedCache;
                return existingPerformanceHistory;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw; // Nếu lỗi bất kỳ đâu, Database không thay đổi gì cả -> An toàn tuyệt đối
            }
        }
    }
}
