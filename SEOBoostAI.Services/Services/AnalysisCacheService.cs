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

namespace SEOBoostAI.Service.Services
{
    public class AnalysisCacheService : IAnalysisCacheService
    {
        private readonly IAnalysisCacheRepository _analysisCacheRepository;
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPageSpeedService _pageSpeedService;
        private readonly IElementService _elementService;
        private readonly ILogger<AnalysisCacheService> _logger;
        private readonly ICrawlingService _crawlingService;
        private readonly IGeminiAIService _geminiAIService;
        private readonly ICompareUrlString _compareUrlString;
        private readonly IAnalysisSnapshotRepository _analysisSnapshotRepository;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromDays(7);

        public AnalysisCacheService(IAnalysisCacheRepository analysisCacheRepository, IUserRepository userRepository,
            IUnitOfWork unitOfWork, IPageSpeedService pageSpeedService, IElementService elementService,
            ILogger<AnalysisCacheService> logger, ICrawlingService crawlingService, IGeminiAIService geminiAIService,
            ICompareUrlString compareUrlString, IAnalysisSnapshotRepository analysisSnapshotRepository)
        {
            _analysisCacheRepository = analysisCacheRepository;
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _pageSpeedService = pageSpeedService;
            _elementService = elementService;
            _logger = logger;
            _crawlingService = crawlingService;
            _geminiAIService = geminiAIService;
            _compareUrlString = compareUrlString;
            _analysisSnapshotRepository = analysisSnapshotRepository;
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
                LastAnalyzedAt = DateTime.UtcNow.AddHours(7)
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
                SpeedIndex: lighthouse.Audits?.Si?.NumericValue,
                TimeToInteractive: lighthouse.Audits?.Tti?.NumericValue
            );

            var geminiResponse = await _geminiAIService.SuggestionAnalysisPerformance(JsonSerializer.Serialize(metrics), null);

            analysisCacheModel.PageSpeedResponse = JsonSerializer.Serialize(metrics);
            analysisCacheModel.Suggestion = geminiResponse.Suggestion;
            analysisCacheModel.GeneralAssessment = geminiResponse.GeneralAssessment;

            var elements = await _elementService.PrepareElementsAsync(normalizedUrl);
            analysisCacheModel.Elements = elements;

            await _analysisCacheRepository.CreateAsync(analysisCacheModel);
            return analysisCacheModel;
        }

        public async Task<AnalysisCache> ReAnalyzeInternalAsync(string url, string strategy)
        {
            string normalizedUrl = _compareUrlString.NormalizeUrlForComparison(url);

            if (string.IsNullOrEmpty(normalizedUrl))
            {
                throw new Exception("URL không hợp lệ.");
            }

            var analysisCacheModel = await _analysisCacheRepository.GetByUrlAndStrategyAsync(normalizedUrl, strategy);

            if (analysisCacheModel == null)
            {
                // Nếu không tìm thấy (ví dụ: đã bị xóa), thì không thể "Re-Analyze".
                // Hoặc bạn có thể chọn gọi hàm CreateAsync tại đây nếu muốn.
                throw new Exception($"Không tìm thấy AnalysisCache để cập nhật cho URL: {url} và Strategy: {strategy}");
            }

            //Create AnalysisSnapshot from existing AnalysisCache
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
                SpeedIndex: lighthouse.Audits?.Si?.NumericValue,
                TimeToInteractive: lighthouse.Audits?.Tti?.NumericValue
            );

            var geminiResponse = await _geminiAIService.SuggestionAnalysisPerformance(JsonSerializer.Serialize(newMetrics), analysisCacheModel.PageSpeedResponse);

            analysisCacheModel.LastAnalyzedAt = DateTime.UtcNow.AddHours(7);
            analysisCacheModel.PageSpeedResponse = JsonSerializer.Serialize(newMetrics);
            analysisCacheModel.Suggestion = geminiResponse.Suggestion;
            analysisCacheModel.GeneralAssessment = geminiResponse.GeneralAssessment;

            await _elementService.DeleteElementsForCacheAsync(analysisCacheModel.AnalysisCacheID);

            analysisCacheModel.Elements = new List<Element>();
            var newElements = await _elementService.PrepareElementsAsync(url);

            foreach (var item in newElements)
            {
                analysisCacheModel.Elements.Add(item);
            }

            await _analysisCacheRepository.UpdateAsync(analysisCacheModel);

            return analysisCacheModel;
        }

        public async Task<AnalysisCache> GetOrCreateFreshAnalysisCacheAsync(string normalizedUrl, string strategy)
        {
            var staleThreshold = DateTime.UtcNow.AddHours(7).Subtract(_cacheDuration);

            var existingCache = await _analysisCacheRepository.GetByUrlAndStrategyAsync(normalizedUrl, strategy);

            if (existingCache == null)
            {
                _logger.LogInformation($"Cache MISS. Tạo mới cache cho: {normalizedUrl}");
                return await AnalyzeInternalAsync(normalizedUrl, strategy);
            }

            if (existingCache.LastAnalyzedAt < staleThreshold)
            {
                _logger.LogInformation($"Cache STALE. Chạy lại phân tích cho: {normalizedUrl}");
                return await ReAnalyzeInternalAsync(normalizedUrl, strategy);
            }

            _logger.LogInformation($"Cache HIT. Trả về cache có sẵn cho: {normalizedUrl}");
            return existingCache;
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
                    SiChange = Math.Round((currentMetrics.SpeedIndex ?? 0) - (previousMetrics.SpeedIndex ?? 0), 2),
                    TtiChange = Math.Round((currentMetrics.TimeToInteractive ?? 0) - (previousMetrics.TimeToInteractive ?? 0), 2)
                };
            }
            return result;
        }

        //public async Task Suggestion()
        //{

        //}

        //public async Task<AnalysisCache> AnalyzeAndSaveAnalysisCacheAsync(int userId, string url, string strategy)
        //{
        //    // 1. Gọi API bên ngoài để lấy dữ liệu
        //    var apiResult = await _pageSpeedService.GetPageSpeedAsync(url, strategy);

        //    if (apiResult == null || apiResult.LighthouseResult == null)
        //    {
        //        throw new Exception("Không nhận được kết quả từ PageSpeed API.");
        //    }

        //    var lighthouse = apiResult.LighthouseResult;

        //    var htmlDoc = await _crawlingService.GetHtmlDocumentAsync(url);
        //    if (htmlDoc == null)
        //    {
        //        throw new Exception("Không thể tải hoặc phân tích HTML của trang.");
        //    }

        //    try
        //    {
        //        string lighthouseJson = JsonSerializer.Serialize(lighthouse, new JsonSerializerOptions { WriteIndented = true });
        //        _logger.LogInformation("--- START DESERIALIZED LIGHTHOUSE OBJECT ---");
        //        _logger.LogInformation(lighthouseJson);
        //        _logger.LogInformation("--- END DESERIALIZED LIGHTHOUSE OBJECT ---");
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogWarning(ex, "Không thể serialize đối tượng lighthouse để debug.");
        //    }

        //    var metrics = new PageSpeedMetrics(
        //        AnalysisCacheScore: (int)((lighthouse.Categories?.AnalysisCache?.Score ?? 0) * 100),
        //        FCP: lighthouse.Audits?.Fcp?.NumericValue,
        //        LCP: lighthouse.Audits?.Lcp?.NumericValue,
        //        CLS: lighthouse.Audits?.Cls?.NumericValue,
        //        TBT: lighthouse.Audits?.Tbt?.NumericValue,
        //        SpeedIndex: lighthouse.Audits?.Si?.NumericValue,
        //        TimeToInteractive: lighthouse.Audits?.Tti?.NumericValue
        //    );

        //    // 2. Map dữ liệu từ DTO sang Model (AnalysisCache)
        //    var analysisCacheModel = new AnalysisCache
        //    {
        //        UserID = userId,
        //        Url = url,
        //        Strategy = strategy, // Lưu lại chiến lược (desktop/mobile)
        //        PageSpeedResponse = JsonSerializer.Serialize(metrics),
        //        FetchTime = DateTime.UtcNow, // Ghi lại thời gian gọi
        //        IsDeleted = false
        //        // CompletedTime có thể bạn muốn xử lý riêng
        //    };

        //    var allSuggestions = new List<Element>();

        //    // 3. Dùng UnitOfWork để lưu vào DB
        //    await _unitOfWork.BeginTransactionAsync();
        //    try
        //    {
        //        await _analysisCacheRepository.CreateAsync(analysisCacheModel);
        //        await _unitOfWork.SaveChangesAsync();

        //        _logger.LogInformation("Đã lưu AnalysisCache ID: {AnalysisCacheID}", analysisCacheModel.AnalysisCacheID);

        //        allSuggestions.AddRange(GenerateGeneralSuggestions(metrics)); // Cách đơn giản
        //        allSuggestions.AddRange(GenerateAdvancedSuggestions(metrics, htmlDoc));     // Cách nâng cao

        //        _logger.LogInformation("Đã tạo tổng cộng {Count} gợi ý.", allSuggestions.Count);

        //        if (allSuggestions.Any())
        //        {
        //            // 6. Gán AnalysisCacheID cho tất cả gợi ý
        //            foreach (var suggestion in allSuggestions)
        //            {
        //                suggestion.AnalysisCacheID = analysisCacheModel.AnalysisCacheID;
        //                suggestion.IsDeleted = false;
        //                suggestion.Status = "Gợi ý"; // Hoặc "Pending"
        //            }

        //            // 7. LƯU LẦN 2: Lưu tất cả Element (gợi ý)
        //            _logger.LogInformation("Đang lưu {Count} Element vào DB...", allSuggestions.Count);
        //            await _elementRepository.CreateRangeAsync(allSuggestions);
        //            await _unitOfWork.SaveChangesAsync();
        //            _logger.LogInformation("Đã lưu Element thành công.");
        //        }

        //        await _unitOfWork.CommitTransactionAsync();

        //        analysisCacheModel.Elements = allSuggestions;
        //        return analysisCacheModel;
        //    }
        //    catch (Exception ex)
        //    {
        //        await _unitOfWork.RollbackTransactionAsync();
        //        _logger.LogError(ex, "Lỗi trong transaction AnalyzeAndSaveAnalysisCache");
        //        throw new Exception("Lỗi khi lưu kết quả AnalysisCache và Element vào DB.", ex);
        //    }
        //}

        private List<Element> GenerateGeneralSuggestions(PageSpeedMetrics metrics)
        {
            var suggestions = new List<Element>();

            // CLS (ngưỡng 0.1)
            if (metrics.CLS > 0.1)
            {
                suggestions.Add(new Element
                {
                    TagName = "Chung",
                    Description = "Chỉ số CLS (Cumulative Layout Shift) quá cao.",
                    AIRecommendation = "Điểm CLS cao làm trang bị 'nhảy' khi tải. Hãy kiểm tra lại tất cả thẻ `<img>` và `<iframe>` để đảm bảo chúng có thuộc tính `width` và `height` rõ ràng. Tránh chèn nội dung động (như quảng cáo) vào đầu trang.",
                    Important = true
                });
            }

            // TBT (ngưỡng 300ms)
            if (metrics.TBT > 300)
            {
                suggestions.Add(new Element
                {
                    TagName = "Chung",
                    Description = "Chỉ số TBT (Total Blocking Time) quá cao.",
                    AIRecommendation = "Thời gian TBT cao cho thấy trang bị 'đơ' do JavaScript. Hãy kiểm tra các file `<script>` lớn, sử dụng thuộc tính `async` hoặc `defer` để tải không đồng bộ, và giảm thiểu các tác vụ nặng (heavy tasks) trên luồng chính.",
                    Important = true
                });
            }

            // LCP (ngưỡng 2500ms)
            if (metrics.LCP > 2500)
            {
                suggestions.Add(new Element
                {
                    TagName = "Chung",
                    Description = "Chỉ số LCP (Largest Contentful Paint) quá chậm.",
                    AIRecommendation = "Tốc độ tải ảnh/nội dung lớn nhất (LCP) đang chậm. Hãy xác định phần tử LCP (thường là ảnh hero, banner) và tối ưu nó: nén ảnh (dùng .webp), giảm kích thước, và ưu tiên tải (preload) tài nguyên này.",
                    Important = true
                });
            }

            _logger.LogInformation("GenerateGeneralSuggestions đã tạo {Count} gợi ý.", suggestions.Count);
            return suggestions;
        }

        private List<Element> GenerateAdvancedSuggestions(PageSpeedMetrics metrics, HtmlDocument htmlDoc)
        {
            var suggestions = new List<Element>();
            var documentNode = htmlDoc.DocumentNode;

            // 1. Phân tích CLS (Lỗi `<img>` thiếu width/height)
            if (metrics.CLS > 0.1) // Triệu chứng
            {
                _logger.LogInformation("Phân tích CLS: Điểm cao {CLS}, đang tìm thẻ img/iframe...", metrics.CLS);
                try
                {
                    // Nguyên nhân: Tìm <img> không có width/height
                    var imagesWithoutSize = documentNode.SelectNodes("//img[not(@width) or not(@height)]");
                    if (imagesWithoutSize != null)
                    {
                        foreach (var imgNode in imagesWithoutSize.Take(5)) // Lấy 5 lỗi đầu tiên
                        {
                            suggestions.Add(new Element
                            {
                                TagName = "img",
                                Description = "Thẻ <img> thiếu thuộc tính kích thước.",
                                OuterHTML = imgNode.OuterHtml,
                                AIRecommendation = "Thẻ `<img>` này thiếu 'width' hoặc 'height', là nguyên nhân hàng đầu gây 'nhảy' trang (CLS). Hãy bổ sung kích thước (ví dụ: width='300' height='200') hoặc dùng CSS `aspect-ratio`.",
                                Important = true
                            });
                        }
                    }
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Lỗi khi parse thẻ <img> (CLS)"); }
            }

            // 2. Phân tích TBT (Lỗi `<script>` chặn hiển thị)
            if (metrics.TBT > 300) // Triệu chứng
            {
                _logger.LogInformation("Phân tích TBT: Điểm cao {TBT}, đang tìm thẻ script...", metrics.TBT);
                try
                {
                    // Nguyên nhân: Tìm <script> có 'src' nhưng không có 'async' hoặc 'defer'
                    var blockingScripts = documentNode.SelectNodes("//script[@src and not(@async) and not(@defer)]");
                    if (blockingScripts != null)
                    {
                        foreach (var scriptNode in blockingScripts)
                        {
                            suggestions.Add(new Element
                            {
                                TagName = "script",
                                Description = "File JavaScript này đang chặn luồng chính (blocking).",
                                OuterHTML = scriptNode.OuterHtml,
                                AIRecommendation = "File JavaScript này không có `async` hoặc `defer`. Điều này chặn trình duyệt phân tích HTML, gây ra TBT cao. Hãy thêm `async` (nếu script độc lập) hoặc `defer` (nếu script cần chạy theo thứ tự).",
                                Important = true
                            });
                        }
                    }
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Lỗi khi parse thẻ <script> (TBT)"); }
            }

            // 3. Phân tích LCP (Lỗi <img> không được ưu tiên)
            if (metrics.LCP > 2500) // Triệu chứng
            {
                _logger.LogInformation("Phân tích LCP: Điểm cao {LCP}, đang tìm thẻ img không lazy...", metrics.LCP);
                try
                {
                    // Nguyên nhân (Dự đoán): Các ảnh không lazy-load (thường là LCP)
                    var nonLazyImages = documentNode.SelectNodes("//img[not(@loading='lazy')]");
                    if (nonLazyImages != null)
                    {
                        // Giả định: Ảnh không-lazy đầu tiên là LCP
                        var lcpCandidate = nonLazyImages.FirstOrDefault();
                        if (lcpCandidate != null)
                        {
                            suggestions.Add(new Element
                            {
                                TagName = "img",
                                Description = "Dự đoán đây là phần tử LCP (ảnh lớn nhất).",
                                OuterHTML = lcpCandidate.OuterHtml,
                                AIRecommendation = "Đây có thể là ảnh LCP của bạn. Hãy tối ưu nó: 1. Nén ảnh (dùng .webp). 2. Thêm thuộc tính `fetchpriority='high'` để trình duyệt ưu tiên tải ảnh này.",
                                Important = true
                            });
                        }
                    }
                }
                catch (Exception ex) { _logger.LogWarning(ex, "Lỗi khi parse thẻ <img> (LCP)"); }
            }

            _logger.LogInformation("GenerateAdvancedSuggestions (đã nâng cấp) đã tạo {Count} gợi ý.", suggestions.Count);
            return suggestions;
        }
    }
}
