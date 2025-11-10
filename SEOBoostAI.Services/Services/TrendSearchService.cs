using Microsoft.Extensions.Logging;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.DTOs;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class TrendSearchService : ITrendSearchService
    {
        // === 1. KHAI BÁO TẤT CẢ CÁC PHỤ THUỘC ===
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TrendSearchService> _logger;

        // Repositories (Database)
        private readonly ITrendSearchesRepository _trendSearchesRepo; // Cache (Bảng 1-6)
        private readonly IQueryHistoryRepository _queryHistoryRepo; // Log (Bảng 7)

        // Services (Nghiệp vụ)
        private readonly IGeminiAiKeywordService _keywordService;     // AI Lần 1
        private readonly IGeminiAiAnalysisService _analysisService;  // AI Lần 2
        private readonly ISerpApiService _serpApiService;             // API Bên thứ 3

        // === 2. TIÊM PHỤ THUỘC (CONSTRUCTOR) ===
        public TrendSearchService(
            IUnitOfWork unitOfWork,
            ILogger<TrendSearchService> logger,
            ITrendSearchesRepository trendSearchesRepo,
            IQueryHistoryRepository queryHistoryRepo,
            IGeminiAiKeywordService keywordService,
            IGeminiAiAnalysisService analysisService,
            ISerpApiService serpApiService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _trendSearchesRepo = trendSearchesRepo;
            _queryHistoryRepo = queryHistoryRepo;
            _keywordService = keywordService;
            _analysisService = analysisService;
            _serpApiService = serpApiService;
        }

        // === 3. PHƯƠNG THỨC NGHIỆP VỤ CHÍNH ===
        public async Task<QueryHistory> AnalyzeTrendQueryAsync(int memberId, string originalQuestion)
        {
            _logger.LogInformation("Bắt đầu quy trình AnalyzeTrendQueryAsync cho MemberId: {memberId}", memberId);

            // BƯỚC 1: XÁC THỰC ĐẦU VÀO
            if (string.IsNullOrWhiteSpace(originalQuestion))
                throw new ArgumentException("Câu hỏi không được rỗng.");

            // BƯỚC 2: GỌI AI (LẦN 1) ĐỂ LẤY TỪ KHÓA
            var parameters = await _keywordService.ExtractKeywordsFromQuestionAsync(originalQuestion);
            if (parameters == null || string.IsNullOrWhiteSpace(parameters.Query))
            {
                _logger.LogWarning("AI không thể xác định từ khóa từ: {question}", originalQuestion);
                throw new Exception("AI không thể xác định từ khóa hợp lệ từ câu hỏi.");
            }

            // BƯỚC 3: "CHECK NGƯỢC" (CACHE)
            var trendData = await CheckCacheAsync(parameters);

            if (trendData == null)
            {
                // BƯỚC 4: CACHE MISS -> GỌI API BÊN THỨ 3 VÀ LƯU VÀO DB
                _logger.LogInformation("Cache MISS. Đang gọi API bên thứ 3 cho: {query}", parameters.Query);
                trendData = await FetchAndMapNewDataAsync(parameters);
            }
            else
            {
                _logger.LogInformation("Cache HIT. Tái sử dụng dữ liệu từ DB cho: {query}", parameters.Query);
            }

            // BƯỚC 5: TÁI TẠO JSON TỪ MODEL ĐỂ GỬI CHO AI
            string dataForAI = ReconstructJsonFromModel(trendData);

            // BƯỚC 6: GỌI AI (LẦN 2) ĐỂ TỔNG HỢP KẾT QUẢ
            var finalAiResponse = await _analysisService.GetTrendAnalysisSuggestionAsync(originalQuestion, dataForAI);

            // BƯỚC 7: LƯU LỊCH SỬ (BẢNG 7)
            var historyLog = new QueryHistory
            {
                MemberId = memberId,
                OriginalQuestion = originalQuestion,
                FinalAiResponse = finalAiResponse,
                CreatedAt = DateTime.UtcNow
            };

            await _queryHistoryRepo.CreateAsync(historyLog);
            await _unitOfWork.SaveChangesAsync(); // Lưu bảng QueryHistory

            _logger.LogInformation("Hoàn tất quy trình cho MemberId: {memberId}", memberId);
            return historyLog; // Trả về kết quả cuối cùng
        }

        // === 4. CÁC PHƯƠNG THỨC HELPER ===

        // Phương thức này thực hiện yêu cầu của thầy bạn
        private async Task<TrendSearch> CheckCacheAsync(TrendParameters parameters)
        {
            var cacheExpiry = DateTime.UtcNow.AddHours(-6); // Tuổi thọ cache là 6 tiếng

            // Tìm trong Bảng 1 (TrendSearches)
            var cachedSearch = await _trendSearchesRepo.GetAsync(
                filter: t => t.Query == parameters.Query &&
                             t.Geolocation == parameters.Geolocation &&
                             t.Timeframe == parameters.Timeframe &&
                             t.Language == parameters.Language &&
                             t.CreatedAt >= cacheExpiry, // Chỉ lấy dữ liệu còn mới

                // Quan trọng: Phải Include tất cả các bảng con (Bảng 2-6)
                includeProperties: "InterestOverTimes,RelatedTopics,InterestByRegions,RelatedQueries,RegionComparisons"
            );

            return cachedSearch; // Sẽ là null nếu không tìm thấy
        }

        // Phương thức này gọi 5-6 API và map chúng vào 1 object Entity
        // Phương thức này gọi 5-6 API và map chúng vào 1 object Entity
        private async Task<TrendSearch> FetchAndMapNewDataAsync(TrendParameters parameters)
        {
            // 1. Tạo đối tượng cha (Bảng 1)
            var newTrendSearch = new TrendSearch
            {
                Query = parameters.Query,
                Geolocation = parameters.Geolocation,
                Language = parameters.Language,
                Timeframe = parameters.Timeframe,
                CreatedAt = DateTime.UtcNow
            };

            // 2. Quyết định logic gọi API (Phân tích 1 từ hay so sánh 2 từ)
            bool isComparison = parameters.Query.Contains(",");

            // 3. Gọi các API song song để tiết kiệm thời gian
            var tasks = new List<Task>();

            // Luôn gọi 3 API này
            tasks.Add(FetchInterestOverTimeAsync(newTrendSearch, parameters));
            tasks.Add(FetchRelatedTopicsAsync(newTrendSearch, parameters));    // <-- Mở khóa dòng này
            tasks.Add(FetchRelatedQueriesAsync(newTrendSearch, parameters));   // <-- Mở khóa dòng này

            if (isComparison)
            {
                // Nếu so sánh (phở,cháo), gọi API So sánh khu vực
                tasks.Add(FetchRegionComparisonAsync(newTrendSearch, parameters)); // <-- Mở khóa dòng này
            }
            else
            {
                // Nếu 1 từ (phở), gọi API Khu vực đơn
                tasks.Add(FetchInterestByRegionAsync(newTrendSearch, parameters)); // <-- Mở khóa dòng này
            }

            // 4. Chờ tất cả API hoàn thành
            await Task.WhenAll(tasks);

            // 5. Lưu vào DB (Bảng 1 và 5 bảng con cùng lúc)
            await _trendSearchesRepo.CreateAsync(newTrendSearch);
            // Dòng SaveChangesAsync này RẤT QUAN TRỌNG
            // Nó sẽ thực thi UnitOfWork, lưu Bảng 1 và TẤT CẢ các bảng con (2-6)
            // mà bạn đã .Add() vào collections
            await _unitOfWork.SaveChangesAsync();

            return newTrendSearch;
        }

        // Tái tạo JSON từ Entity để gửi cho AI (Lần 2)
        private string ReconstructJsonFromModel(TrendSearch trendData)
        {
            // Chúng ta chỉ serialize những gì cần thiết cho AI
            var dataForAI = new
            {
                SearchParameters = new
                {
                    trendData.Query,
                    trendData.Geolocation,
                    trendData.Timeframe
                },
                InterestOverTime = trendData.InterestOverTimes.Select(iot => new { iot.Query, iot.DateRange, iot.InterestValue }),
                InterestByRegion = trendData.InterestByRegions.Select(ibr => new { ibr.LocationName, ibr.InterestValue }),
                RegionComparison = trendData.RegionComparisons.Select(rc => new { rc.LocationName, rc.Query, rc.InterestPercentage }),
                RelatedTopics = trendData.RelatedTopics.Select(rt => new { rt.Category, rt.TopicTitle, rt.ValueString }),
                RelatedQueries = trendData.RelatedQueries.Select(rq => new { rq.Category, rq.Query, rq.Value })
            };

            return JsonSerializer.Serialize(dataForAI, new JsonSerializerOptions { WriteIndented = true });
        }

        // --- CÁC HÀM MAP DTO -> ENTITY (Bạn cần hoàn thiện nốt 4 hàm) ---
        // Dưới đây là ví dụ cho 1 hàm

        private async Task FetchInterestOverTimeAsync(TrendSearch trendSearch, TrendParameters parameters)
        {
            try
            {
                var response = await _serpApiService.GetInterestOverTimeAsync(parameters);
                if (response?.InterestOverTime?.TimelineData != null)
                {
                    foreach (var timeline in response.InterestOverTime.TimelineData)
                    {
                        foreach (var value in timeline.Values)
                        {
                            var newRecord = new InterestOverTime
                            {
                                // TrendSearchId sẽ được EF tự động gán
                                Query = value.Query,
                                DateRange = timeline.Date,
                                TimestampVal = long.Parse(timeline.Timestamp),
                                InterestValue = value.ExtractedValue
                            };
                            trendSearch.InterestOverTimes.Add(newRecord); // Thêm vào collection của cha
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi fetch InterestOverTime cho: {query}", parameters.Query);
                // Không ném lỗi, để các API khác tiếp tục chạy
            }
        }

        // ... (Hàm FetchInterestOverTimeAsync của bạn ở đây) ...


        // --- CÁC HÀM MAP DTO -> ENTITY (4 HÀM CÒN LẠI) ---

        private async Task FetchRelatedTopicsAsync(TrendSearch trendSearch, TrendParameters parameters)
        {
            try
            {
                var response = await _serpApiService.GetRelatedTopicsAsync(parameters);
                if (response?.RelatedTopics == null) return;

                // Xử lý "Top" Topics
                if (response.RelatedTopics.Top != null)
                {
                    foreach (var topic in response.RelatedTopics.Top)
                    {
                        var newRecord = new RelatedTopic
                        {
                            Category = "top",
                            TopicTitle = topic.Topic.Title,
                            TopicType = topic.Topic.Type,
                            ExtractedValue = topic.ExtractedValue,
                            // ValueString và GoogleTrendsLink bạn có thể map tương tự nếu cần
                        };
                        trendSearch.RelatedTopics.Add(newRecord);
                    }
                }

                // Xử lý "Rising" Topics
                if (response.RelatedTopics.Rising != null)
                {
                    foreach (var topic in response.RelatedTopics.Rising)
                    {
                        var newRecord = new RelatedTopic
                        {
                            Category = "rising",
                            TopicTitle = topic.Topic.Title,
                            TopicType = topic.Topic.Type,
                            ExtractedValue = topic.ExtractedValue,
                            ValueString = topic.ValueString // (DTO cần cập nhật để có trường này)
                        };
                        trendSearch.RelatedTopics.Add(newRecord);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi fetch RelatedTopics cho: {query}", parameters.Query);
            }
        }

        private async Task FetchRelatedQueriesAsync(TrendSearch trendSearch, TrendParameters parameters)
        {
            try
            {
                var response = await _serpApiService.GetRelatedQueriesAsync(parameters);
                if (response?.RelatedQueries == null) return;

                // Xử lý "Top" Queries
                if (response.RelatedQueries.Top != null)
                {
                    foreach (var query in response.RelatedQueries.Top)
                    {
                        var newRecord = new RelatedQuery
                        {
                            Category = "top",
                            Query = query.Query,
                            Value = query.ExtractedValue
                        };
                        trendSearch.RelatedQueries.Add(newRecord);
                    }
                }

                // Xử lý "Rising" Queries
                if (response.RelatedQueries.Rising != null)
                {
                    foreach (var query in response.RelatedQueries.Rising)
                    {
                        var newRecord = new RelatedQuery
                        {
                            Category = "rising",
                            Query = query.Query,
                            Value = query.ExtractedValue
                            // (Lưu ý: DTO của bạn cần có ValueString nếu bạn muốn map "Đột phá")
                        };
                        trendSearch.RelatedQueries.Add(newRecord);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi fetch RelatedQueries cho: {query}", parameters.Query);
            }
        }

        private async Task FetchInterestByRegionAsync(TrendSearch trendSearch, TrendParameters parameters)
        {
            try
            {
                var response = await _serpApiService.GetInterestByRegionAsync(parameters);
                if (response?.InterestByRegion != null)
                {
                    foreach (var region in response.InterestByRegion)
                    {
                        var newRecord = new InterestByRegion
                        {
                            LocationName = region.Location,
                            InterestValue = region.ExtractedValue
                            // (Bạn có thể map Latitude/Longitude nếu DTO có)
                        };
                        trendSearch.InterestByRegions.Add(newRecord);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi fetch InterestByRegion cho: {query}", parameters.Query);
            }
        }

        private async Task FetchRegionComparisonAsync(TrendSearch trendSearch, TrendParameters parameters)
        {
            try
            {
                var response = await _serpApiService.GetComparedBreakdownByRegionAsync(parameters);
                if (response?.ComparedBreakdownByRegion != null)
                {
                    foreach (var region in response.ComparedBreakdownByRegion)
                    {
                        // Vòng lặp bên trong để lấy từng giá trị (ví dụ: Phở: 52%, Cháo: 48%)
                        foreach (var value in region.Values)
                        {
                            var newRecord = new RegionComparison
                            {
                                LocationName = region.Location,
                                Query = value.Query,
                                InterestPercentage = value.ExtractedValue
                            };
                            trendSearch.RegionComparisons.Add(newRecord);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi fetch RegionComparison cho: {query}", parameters.Query);
            }
        }
    }
}
