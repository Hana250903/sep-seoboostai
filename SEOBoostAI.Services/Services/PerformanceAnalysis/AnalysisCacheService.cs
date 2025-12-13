using HtmlAgilityPack;
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
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    public class AnalysisCacheService : IAnalysisCacheService
    {
        private readonly IAnalysisCacheRepository _analysisCacheRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPageSpeedService _pageSpeedService;
        private readonly IElementService _elementService;
        private readonly ILogger<AnalysisCacheService> _logger;
        private readonly IGeminiAIService _geminiAIService;
        private readonly IAnalysisSnapshotRepository _analysisSnapshotRepository;
        private readonly IPuppeteerAuditService _puppeteerAuditService;

        public AnalysisCacheService(IAnalysisCacheRepository analysisCacheRepository, IUserRepository userRepository,
            IUnitOfWork unitOfWork, IPageSpeedService pageSpeedService, IElementService elementService,
            ILogger<AnalysisCacheService> logger, IGeminiAIService geminiAIService,
            IAnalysisSnapshotRepository analysisSnapshotRepository, IPuppeteerAuditService puppeteerAuditService)
        {
            _analysisCacheRepository = analysisCacheRepository;
            _unitOfWork = unitOfWork;
            _pageSpeedService = pageSpeedService;
            _elementService = elementService;
            _logger = logger;
            _geminiAIService = geminiAIService;
            _analysisSnapshotRepository = analysisSnapshotRepository;
            _puppeteerAuditService = puppeteerAuditService;
        }

        public async Task CreateAsync(AnalysisCache analysisCache)
        {
            try
            {
                await _analysisCacheRepository.CreateAsync(analysisCache);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var analysisCache = await _analysisCacheRepository.GetByIdAsync(id);
                await _analysisCacheRepository.RemoveAsync(analysisCache);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<AnalysisCache> GetAnalysisCacheByIdAsync(int id)
        {
            return await _analysisCacheRepository.GetByIdAsync(id);
        }

        public async Task<List<AnalysisCache>> GetAnalysisCachesAsync()
        {
            return await _analysisCacheRepository.GetAllAsync();
        }

        public async Task<PaginationResult<List<AnalysisCache>>> GetAnalysisCachesWithPaginateAsync(int currentPage, int pageSize)
        {
            return await _analysisCacheRepository.GetAnalysisCachesWithPaginateAsync(currentPage, pageSize);
        }

        public async Task UpdateAsync(AnalysisCache analysisCache)
        {
            try
            {
                await _analysisCacheRepository.UpdateAsync(analysisCache);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<AnalysisCache> AnalyzeInternalAsync(string normalizedUrl, string strategy)
        {
            var analysisCacheModel = new AnalysisCache
            {
                Url = normalizedUrl,
                NormalizedUrl = normalizedUrl,
                Strategy = strategy,
                LastAnalyzedAt = DateTime.UtcNow
            };

            var apiResult = await _pageSpeedService.GetPageSpeedAsync(normalizedUrl, strategy);

            if (apiResult == null || apiResult.LighthouseResult == null)
            {
                throw new Exception("Không nhận được kết quả từ PageSpeed API.");
            }

            var lighthouse = apiResult.LighthouseResult;

            try
            {
                string lighthouseJson = JsonSerializer.Serialize(lighthouse, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("--- START DESERIALIZED LIGHTHOUSE OBJECT ---");
                _logger.LogInformation(lighthouseJson);
                _logger.LogInformation("--- END DESERIALIZED LIGHTHOUSE OBJECT ---");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể serialize đối tượng lighthouse để debug.");
            }

            var metrics = new PageSpeedMetrics(
                PerformanceScore: (int)((lighthouse.Categories?.Performance?.Score ?? 0) * 100),
                FCP: lighthouse.Audits?.Fcp?.NumericValue,
                LCP: lighthouse.Audits?.Lcp?.NumericValue,
                CLS: lighthouse.Audits?.Cls?.NumericValue,
                TBT: lighthouse.Audits?.Tbt?.NumericValue,
                SpeedIndex: lighthouse.Audits?.Si?.NumericValue
            );

            var geminiResponse = await _geminiAIService.SuggestionAnalysisPerformance(JsonSerializer.Serialize(metrics), null);

            analysisCacheModel.PageSpeedResponse = JsonSerializer.Serialize(metrics);
            analysisCacheModel.Suggestion = geminiResponse.Suggestion;
            analysisCacheModel.GeneralAssessment = geminiResponse.GeneralAssessment;

            var elements = await _puppeteerAuditService.RunAuditAsync(normalizedUrl, strategy);

            analysisCacheModel.Elements = elements;

            await _analysisCacheRepository.CreateAsync(analysisCacheModel);
            return analysisCacheModel;
        }

        public async Task<AnalysisCache> ReAnalyzeInternalAsync(string normalizedUrl, string strategy)
        {
            if (string.IsNullOrEmpty(normalizedUrl))
            {
                throw new Exception("URL không hợp lệ.");
            }

            var analysisCacheModel = await _analysisCacheRepository.GetByUrlAndStrategyAsync(normalizedUrl, strategy);

            if (analysisCacheModel == null)
            {
                // Nếu không tìm thấy (ví dụ: đã bị xóa), thì không thể "Re-Analyze".
                // Hoặc bạn có thể chọn gọi hàm CreateAsync tại đây nếu muốn.
                throw new Exception($"Không tìm thấy AnalysisCache để cập nhật cho URL: {normalizedUrl} và Strategy: {strategy}");
            }

            //Create AnalysisSnapshot from existing AnalysisCache
            var analysisSnapshot = new AnalysisSnapshot
            {
                AnalysisCacheID = analysisCacheModel.AnalysisCacheID,
                PageSpeedResponse = analysisCacheModel.PageSpeedResponse,
                AnalyzedAt = analysisCacheModel.LastAnalyzedAt,
                ArchivedAt = DateTime.UtcNow
            };

            try
            {
                await _analysisSnapshotRepository.CreateAsync(analysisSnapshot);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể tạo AnalysisSnapshot khi Re-Analyze cho AnalysisCacheID: {AnalysisCacheID}", analysisCacheModel.AnalysisCacheID);
            }

            var apiResult = await _pageSpeedService.GetPageSpeedAsync(normalizedUrl, strategy);

            if (apiResult == null || apiResult.LighthouseResult == null)
            {
                throw new Exception("Không nhận được kết quả từ PageSpeed API.");
            }

            var lighthouse = apiResult.LighthouseResult;

            try
            {
                string lighthouseJson = JsonSerializer.Serialize(lighthouse, new JsonSerializerOptions { WriteIndented = true });
                _logger.LogInformation("--- START DESERIALIZED LIGHTHOUSE OBJECT (RE-ANALYZE) ---");
                _logger.LogInformation(lighthouseJson);
                _logger.LogInformation("--- END DESERIALIZED LIGHTHOUSE OBJECT (RE-ANALYZE) ---");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể serialize đối tượng lighthouse để debug (Re-Analyze).");
            }

            var newMetrics = new PageSpeedMetrics(
                PerformanceScore: (int)((lighthouse.Categories?.Performance?.Score ?? 0) * 100),
                FCP: lighthouse.Audits?.Fcp?.NumericValue,
                LCP: lighthouse.Audits?.Lcp?.NumericValue,
                CLS: lighthouse.Audits?.Cls?.NumericValue,
                TBT: lighthouse.Audits?.Tbt?.NumericValue,
                SpeedIndex: lighthouse.Audits?.Si?.NumericValue
            );

            var geminiResponse = await _geminiAIService.SuggestionAnalysisPerformance(JsonSerializer.Serialize(newMetrics), analysisCacheModel.PageSpeedResponse);

            analysisCacheModel.LastAnalyzedAt = DateTime.UtcNow;
            analysisCacheModel.PageSpeedResponse = JsonSerializer.Serialize(newMetrics);
            analysisCacheModel.Suggestion = geminiResponse.Suggestion;
            analysisCacheModel.GeneralAssessment = geminiResponse.GeneralAssessment;

            await _elementService.DeleteElementsForCacheAsync(analysisCacheModel.AnalysisCacheID);

            analysisCacheModel.Elements = new List<Element>();
            var newElements = await _puppeteerAuditService.RunAuditAsync(normalizedUrl, strategy);

            foreach (var item in newElements)
            {
                analysisCacheModel.Elements.Add(item);
            }

            await _analysisCacheRepository.UpdateAsync(analysisCacheModel);

            return analysisCacheModel;
        }

        public async Task<AnalysisResultModel> GetAnalysisResultAsync(int analysisCacheId)
        {
            var current = await _analysisCacheRepository.GetByIdAsync(analysisCacheId) ?? throw new Exception($"Không tìm thấy AnalysisCache với ID: {analysisCacheId}");
            var currentMetrics = JsonSerializer.Deserialize<PageSpeedMetrics>(current.PageSpeedResponse);

            var previousSnapshot = await _analysisSnapshotRepository.GetAnalysisSnapshotByAnalysisCacheIdAsync(analysisCacheId);

            var result = new AnalysisResultModel
            {
                PageSpeedMetrics = currentMetrics,
                ComparisonModel = null
            };

            if (previousSnapshot != null)
            {
                var previousMetrics = JsonSerializer.Deserialize<PageSpeedMetrics>(previousSnapshot.PageSpeedResponse);

                result.ComparisonModel = new ComparisonModel
                {
                    ScoreChange = currentMetrics.PerformanceScore - previousMetrics.PerformanceScore,
                    FcpChange = Math.Round((currentMetrics.FCP ?? 0) - (previousMetrics.FCP ?? 0), 2),
                    LcpChange = Math.Round((currentMetrics.LCP ?? 0) - (previousMetrics.LCP ?? 0), 2),
                    ClsChange = Math.Round((currentMetrics.CLS ?? 0) - (previousMetrics.CLS ?? 0), 3), // CLS thường lấy 3 số
                    TbtChange = Math.Round((currentMetrics.TBT ?? 0) - (previousMetrics.TBT ?? 0), 0), // TBT thường là số nguyên ms
                    SiChange = Math.Round((currentMetrics.SpeedIndex ?? 0) - (previousMetrics.SpeedIndex ?? 0), 2)
                };
            }
            return result;
        }
    }
}
