using Microsoft.Extensions.Logging;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    /// <summary>
    /// AnalysisCacheService - Orchestrator chính cho URL Analysis
    /// 
    /// FLOW CHÍNH:
    /// 1. AnalyzeInternalAsync() - Phân tích URL lần đầu
    /// 2. ReAnalyzeInternalAsync() - Phân tích lại URL (có so sánh với lần trước)
    /// 3. GetAnalysisResultAsync() - Lấy kết quả + so sánh metrics
    /// </summary>
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

        /// <summary>
        /// BƯỚC 1: PHÂN TÍCH URL LẦN ĐẦU
        /// 
        /// Flow:
        /// 1. Gọi PageSpeed API → Lấy metrics (FCP, LCP, TBT, CLS, SpeedIndex)
        /// 2. Gọi Gemini AI → Tạo Suggestion + GeneralAssessment
        /// 3. Gọi Puppeteer → Crawl HTML, tìm issues (img thiếu alt, script blocking...)
        /// 4. Lưu tất cả vào AnalysisCache
        /// </summary>
        public async Task<AnalysisCache> AnalyzeInternalAsync(string normalizedUrl, string strategy)
        {
            // ===== KHỞI TẠO MODEL =====
            var analysisCacheModel = new AnalysisCache
            {
                Url = normalizedUrl,
                NormalizedUrl = normalizedUrl,
                Strategy = strategy,
                LastAnalyzedAt = DateTime.UtcNow.AddHours(7)
            };

            // ===== BƯỚC 1: GỌI PAGESPEED API =====
            // Gửi URL đến Google PageSpeed Insights API
            // Trả về: Performance Score, FCP, LCP, CLS, TBT, SpeedIndex
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

            // ===== BƯỚC 2: TRÍCH XUẤT METRICS =====
            // Lấy 5 chỉ số quan trọng từ Lighthouse result:
            // - PerformanceScore: Điểm tổng (0-100)
            // - FCP: First Contentful Paint (thời gian hiển thị nội dung đầu tiên)
            // - LCP: Largest Contentful Paint (thời gian hiển thị phần tử lớn nhất)
            // - CLS: Cumulative Layout Shift (độ ổn định giao diện)
            // - TBT: Total Blocking Time (thời gian JS block main thread)
            // - SpeedIndex: Tốc độ hiển thị nội dung visible
            var metrics = new PageSpeedMetrics(
                PerformanceScore: (int)((lighthouse.Categories?.Performance?.Score ?? 0) * 100),
                FCP: lighthouse.Audits?.Fcp?.NumericValue,
                LCP: lighthouse.Audits?.Lcp?.NumericValue,
                CLS: lighthouse.Audits?.Cls?.NumericValue,
                TBT: lighthouse.Audits?.Tbt?.NumericValue,
                SpeedIndex: lighthouse.Audits?.Si?.NumericValue
            );

            // ===== BƯỚC 3: GỌI GEMINI AI =====
            // AI sẽ phân tích metrics và trả về:
            // - Suggestion: Gợi ý cải thiện
            // - GeneralAssessment: Đánh giá tổng quan
            var geminiResponse = await _geminiAIService.SuggestionAnalysisPerformance(JsonSerializer.Serialize(metrics), null);

            analysisCacheModel.PageSpeedResponse = JsonSerializer.Serialize(metrics);
            analysisCacheModel.Suggestion = geminiResponse.Suggestion;
            analysisCacheModel.GeneralAssessment = geminiResponse.GeneralAssessment;

            // ===== BƯỚC 4: GỌI PUPPETEER CRAWL HTML =====
            // Puppeteer sẽ mở trang web và kiểm tra:
            // - Metadata: title, description, viewport
            // - Images: alt, lazy load, width/height
            // - Scripts: async/defer
            // - SEO: H1, OG tags, canonical, lang
            // - Performance: DOM size, inline CSS, fonts
            // - Accessibility: buttons, labels, links
            var elements = await _puppeteerAuditService.RunAuditAsync(normalizedUrl, strategy);

            analysisCacheModel.Elements = elements;

            // ===== BƯỚC 5: LƯU VÀO DATABASE =====
            await _analysisCacheRepository.CreateAsync(analysisCacheModel);
            return analysisCacheModel;
        }

        /// <summary>
        /// BƯỚC RE-ANALYZE: PHÂN TÍCH LẠI URL (CÓ SO SÁNH)
        /// 
        /// Flow:
        /// 1. Tạo AnalysisSnapshot từ data cũ (lưu history)
        /// 2. Gọi lại PageSpeed API + Puppeteer
        /// 3. Gemini AI so sánh metrics mới vs cũ
        /// 4. Update AnalysisCache với data mới
        /// </summary>
        public async Task<AnalysisCache> ReAnalyzeInternalAsync(string normalizedUrl, string strategy)
        {
            if (string.IsNullOrEmpty(normalizedUrl))
            {
                throw new Exception("URL không hợp lệ.");
            }

            // ===== LẤY CACHE CŨ =====
            var analysisCacheModel = await _analysisCacheRepository.GetByUrlAndStrategyAsync(normalizedUrl, strategy);

            if (analysisCacheModel == null)
            {
                throw new Exception($"Không tìm thấy AnalysisCache để cập nhật cho URL: {normalizedUrl} và Strategy: {strategy}");
            }

            // ===== BƯỚC 1: TẠO SNAPSHOT (LƯU HISTORY) =====
            // Lưu data cũ vào AnalysisSnapshot để sau này so sánh
            // Giúp user thấy được sự tiến bộ sau khi fix
            var analysisSnapshot = new AnalysisSnapshot
            {
                AnalysisCacheID = analysisCacheModel.AnalysisCacheID,
                PageSpeedResponse = analysisCacheModel.PageSpeedResponse,
                AnalyzedAt = analysisCacheModel.LastAnalyzedAt,
                ArchivedAt = DateTime.UtcNow.AddHours(7)
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

            // ===== BƯỚC 3: GEMINI SO SÁNH METRICS MỚI VS CŨ =====
            // Tham số thứ 2 (analysisCacheModel.PageSpeedResponse) là metrics CŨ
            // AI sẽ so sánh và đưa ra nhận xét về sự thay đổi
            var geminiResponse = await _geminiAIService.SuggestionAnalysisPerformance(JsonSerializer.Serialize(newMetrics), analysisCacheModel.PageSpeedResponse);

            // ===== BƯỚC 4: UPDATE CACHE VỚI DATA MỚI =====
            analysisCacheModel.LastAnalyzedAt = DateTime.UtcNow.AddHours(7);
            analysisCacheModel.PageSpeedResponse = JsonSerializer.Serialize(newMetrics);
            analysisCacheModel.Suggestion = geminiResponse.Suggestion;
            analysisCacheModel.GeneralAssessment = geminiResponse.GeneralAssessment;

            // ===== BƯỚC 5: XÓA ELEMENTS CŨ VÀ CRAWL LẠI =====
            // Xóa tất cả elements cũ (vì trang có thể đã thay đổi)
            await _elementService.DeleteElementsForCacheAsync(analysisCacheModel.AnalysisCacheID);

            // Puppeteer crawl lại để tìm issues mới
            analysisCacheModel.Elements = new List<Element>();
            var newElements = await _puppeteerAuditService.RunAuditAsync(normalizedUrl, strategy);

            foreach (var item in newElements)
            {
                analysisCacheModel.Elements.Add(item);
            }

            await _analysisCacheRepository.UpdateAsync(analysisCacheModel);

            return analysisCacheModel;
        }

        /// <summary>
        /// LẤY KẾT QUẢ + SO SÁNH METRICS
        /// 
        /// Trả về:
        /// - PageSpeedMetrics: Metrics hiện tại
        /// - ComparisonModel: So sánh với lần trước (nếu có snapshot)
        ///   + ScoreChange: Thay đổi điểm
        ///   + FcpChange, LcpChange, ClsChange, TbtChange, SiChange
        /// </summary>
        public async Task<AnalysisResultModel> GetAnalysisResultAsync(int analysisCacheId)
        {
            // Lấy metrics hiện tại
            var current = await _analysisCacheRepository.GetByIdAsync(analysisCacheId) ?? throw new Exception($"Không tìm thấy AnalysisCache với ID: {analysisCacheId}");
            var currentMetrics = JsonSerializer.Deserialize<PageSpeedMetrics>(current.PageSpeedResponse);

            // Lấy snapshot (metrics cũ) để so sánh
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
