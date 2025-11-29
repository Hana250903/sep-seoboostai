using Microsoft.Extensions.Logging;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Repository.ModelExtensions;
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

        // Repositories
        private readonly ITrendSearchesRepository _trendSearchesRepo;
        private readonly IQueryHistoryRepository _queryHistoryRepo;
        private readonly IAdsSearchRequestRepository _adsRequestRepo; // Repo cho bảng cha Ads

        // Services
        private readonly IGeminiAiKeywordService _keywordService;     // AI 1
        private readonly IGeminiAiAnalysisService _analysisService;   // AI 2 (Tư vấn)
        private readonly IGeminiAiGoogleAdsService _adsEvaluationService; // AI 3 (Đánh giá Ads)
        private readonly ISerpApiService _serpApiService;
        private readonly IAdsPlannerService _adsPlannerService;

        private readonly IAdsKeywordDatumRepository _adsKeywordDatumRepo;
        private readonly IUserMonthlyFreeQuotaService _userMonthlyFreeQuotaService;


        // === 2. CONSTRUCTOR ===
        public TrendSearchService(
            IUnitOfWork unitOfWork,
            ILogger<TrendSearchService> logger,
            ITrendSearchesRepository trendSearchesRepo,
            IQueryHistoryRepository queryHistoryRepo,
            IGeminiAiKeywordService keywordService,
            IGeminiAiAnalysisService analysisService,
            ISerpApiService serpApiService,
            IAdsPlannerService adsPlannerService,
            IAdsSearchRequestRepository adsRequestRepo,
            IGeminiAiGoogleAdsService adsEvaluationService,
            IAdsKeywordDatumRepository adsKeywordDatumRepo,
            IUserMonthlyFreeQuotaService userMonthlyFreeQuotaService
            )
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _trendSearchesRepo = trendSearchesRepo;
            _queryHistoryRepo = queryHistoryRepo;
            _keywordService = keywordService;
            _analysisService = analysisService;
            _serpApiService = serpApiService;
            _adsPlannerService = adsPlannerService;
            _adsRequestRepo = adsRequestRepo;
            _adsEvaluationService = adsEvaluationService;
            _adsKeywordDatumRepo = adsKeywordDatumRepo;
            _userMonthlyFreeQuotaService = userMonthlyFreeQuotaService;
        }

        // === 3. PHƯƠNG THỨC NGHIỆP VỤ CHÍNH ===
        public async Task<TrendAnalysisResponseDto> AnalyzeTrendQueryAsync(int memberId, string originalQuestion, int featureID)
        {
            if (await _userMonthlyFreeQuotaService.CheckLimit(memberId, featureID))
            {
                throw new Exception("Bạn đã vượt quá hạn mức sử dụng miễn phí hàng tháng cho tính năng này.");
            }

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

            // BƯỚC 3: XỬ LÝ GOOGLE TRENDS (CHECK CACHE / GỌI API)
            var trendData = await CheckCacheAsync(parameters);
            if (trendData == null)
            {
                _logger.LogInformation("Trend Cache MISS. Đang gọi API bên thứ 3 cho: {query}", parameters.Query);
                trendData = await FetchAndMapNewDataAsync(parameters);
            }
            else
            {
                _logger.LogInformation("Trend Cache HIT. Tái sử dụng dữ liệu từ DB cho: {query}", parameters.Query);
            }

            // BƯỚC 4: XỬ LÝ GOOGLE ADS (LẤY DỮ LIỆU + ID BẢN GHI)
            // Lưu ý: Hàm này giờ trả về Tuple (Data, RequestId)
            var adsResult = await ProcessAdsDataAsync(parameters.Query);
            var adsData = adsResult.Data;           // Dữ liệu để gửi cho AI
            var adsRequestId = adsResult.RequestId; // ID để update DB sau này

            // BƯỚC 5: GỌI AI 1 (TƯ VẤN CHIẾN LƯỢC)
            string dataForAI = ReconstructJsonForAi(trendData, adsData);
            var finalAiResponseString = await _analysisService.GetTrendAnalysisSuggestionAsync(originalQuestion, dataForAI);

            // BƯỚC 6: GỌI AI 2 (ĐÁNH GIÁ ADS & UPDATE DB)
            if (adsData != null && adsData.Any() && adsRequestId.HasValue)
            {
                try
                {
                    // Gọi AI đánh giá
                    var evaluations = await _adsEvaluationService.EvaluateAdsKeywordsAsync(finalAiResponseString, adsData);

                    // Cập nhật vào Database
                    if (evaluations != null && evaluations.Any())
                    {
                        await UpdateAdsEvaluationsInDb(adsRequestId.Value, evaluations);
                    }
                }
                catch (Exception ex)
                {
                    // Lỗi ở bước phụ này không được làm sập luồng chính
                    _logger.LogWarning("Lỗi khi gọi AI đánh giá Ads: " + ex.Message);
                }
            }

            // BƯỚC 7: LƯU LỊCH SỬ
            var historyLog = new QueryHistory
            {
                MemberId = memberId,
                OriginalQuestion = originalQuestion,
                FinalAiResponse = finalAiResponseString,
                CreatedAt = DateTime.UtcNow,

                AdsSearchRequestId = adsRequestId 
            };

            try
            {
                await _queryHistoryRepo.CreateAsync(historyLog);
                await _unitOfWork.SaveChangesAsync();

                await _userMonthlyFreeQuotaService.IncrementUsageCount(memberId, featureID);

                _logger.LogInformation("Hoàn tất quy trình cho MemberId: {memberId}", memberId);
                return new TrendAnalysisResponseDto
                {
                    Id = historyLog.Id,
                    OriginalQuestion = historyLog.OriginalQuestion,
                    FinalAiResponse = historyLog.FinalAiResponse,
                    CreatedAt = historyLog.CreatedAt
                };
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi lưu lịch sử truy vấn: " + ex.Message);
            }
        }

        // === 4. CÁC PHƯƠNG THỨC HELPER ===

        // --- XỬ LÝ GOOGLE ADS (ĐÃ SỬA TRẢ VỀ TUPLE) ---
        private async Task<(List<AdsPlannerItemDto> Data, int? RequestId)> ProcessAdsDataAsync(string queryFromAi)
        {
            var keywords = queryFromAi.Split(',').Select(k => k.Trim()).Where(k => !string.IsNullOrEmpty(k)).ToList();
            if (!keywords.Any()) return (new List<AdsPlannerItemDto>(), null);

            string queryListString = string.Join(",", keywords);

            // 1. Check Cache
            var cachedData = await _adsRequestRepo.GetValidCacheAsync(queryListString);
            if (cachedData != null)
            {
                _logger.LogInformation("Ads Cache HIT: {query}", queryListString);
                var dto = cachedData.AdsKeywordData.Select(x => new AdsPlannerItemDto
                {
                    Keyword = x.Keyword,
                    AvgSearchVolume = x.AvgSearchVolume,
                    Competition = x.Competition,
                    LowBid = x.LowBid,
                    HighBid = x.HighBid
                }).ToList();

                return (dto, cachedData.Id); // Trả về ID cũ
            }

            // 2. Cache Miss -> Gọi API
            _logger.LogInformation("Ads Cache MISS: {query}", queryListString);
            var apiDataRaw = await _adsPlannerService.GetAdsDataAsync(keywords);

            // Xử lý dữ liệu thô
            var apiDataProcessed = apiDataRaw
                .Take(50) // Chỉ lấy 50 kết quả đầu tiên
                .Select(x => new AdsPlannerItemDto
            {
                Keyword = x.Keyword,
                AvgSearchVolume = x.AvgSearchVolume,
                Competition = x.Competition,
                LowBid = string.IsNullOrWhiteSpace(x.LowBid) || x.LowBid == "N/A" ? "Chưa có dữ liệu" : x.LowBid,
                HighBid = string.IsNullOrWhiteSpace(x.HighBid) || x.HighBid == "N/A" ? "Chưa có dữ liệu" : x.HighBid
            }).ToList();

            int? newRequestId = null;

            if (apiDataProcessed.Any())
            {
                // Lưu vào DB
                var newRequest = new AdsSearchRequest
                {
                    QueryList = queryListString,
                    CreatedAt = DateTime.UtcNow,
                    AdsKeywordData = apiDataProcessed.Select(x => new AdsKeywordDatum
                    {
                        Keyword = x.Keyword,
                        AvgSearchVolume = x.AvgSearchVolume,
                        Competition = x.Competition,
                        LowBid = x.LowBid,
                        HighBid = x.HighBid,
                        AiSuggestion = false, // Mặc định
                        AiMessage = null      // Mặc định
                    }).ToList()
                };

                await _adsRequestRepo.CreateAsync(newRequest);
                await _unitOfWork.SaveChangesAsync();
                newRequestId = newRequest.Id; // Lấy ID mới
            }

            return (apiDataProcessed, newRequestId);
        }

        // --- HÀM CẬP NHẬT DB SAU KHI AI ĐÁNH GIÁ ---
        private async Task UpdateAdsEvaluationsInDb(int requestId, List<AdsEvaluationItem> evaluations)
        {
            _logger.LogInformation($"--- BẮT ĐẦU UPDATE (Repository Pattern) (RequestId: {requestId}) ---");

            if (evaluations == null || !evaluations.Any()) return;

            // Lặp qua từng đánh giá và gọi Repository để update
            foreach (var eval in evaluations)
            {
                if (!string.IsNullOrEmpty(eval.Keyword))
                {
                    // Gọi hàm nghiệp vụ trong Repository
                    // Hàm này chạy bất đồng bộ và update thẳng vào DB
                    await _adsKeywordDatumRepo.UpdateAiEvaluationAsync(
                        requestId,
                        eval.Keyword,
                        eval.IsPotential,
                        eval.Message ?? ""
                    );
                }
            }

            _logger.LogInformation("Đã hoàn tất cập nhật đánh giá.");
        }

        // --- XỬ LÝ GOOGLE TRENDS (CACHE) ---
        private async Task<TrendSearch> CheckCacheAsync(TrendParameters parameters)
        {
            var cacheExpiry = DateTime.UtcNow.AddHours(-6);
            return await _trendSearchesRepo.GetAsync(
                filter: t => t.Query == parameters.Query &&
                             t.Geolocation == parameters.Geolocation &&
                             t.Timeframe == parameters.Timeframe &&
                             t.Language == parameters.Language &&
                             t.CreatedAt >= cacheExpiry,
                includeProperties: "InterestOverTimes,RelatedTopics,InterestByRegions,RelatedQueries,RegionComparisons"
            );
        }

        // --- XỬ LÝ GOOGLE TRENDS (API & MAP) ---
        private async Task<TrendSearch> FetchAndMapNewDataAsync(TrendParameters parameters)
        {
            var newTrendSearch = new TrendSearch
            {
                Query = parameters.Query,
                Geolocation = parameters.Geolocation,
                Language = parameters.Language,
                Timeframe = parameters.Timeframe,
                CreatedAt = DateTime.UtcNow
            };

            bool isComparison = parameters.Query.Contains(",");
            var tasks = new List<Task>();

            tasks.Add(FetchInterestOverTimeAsync(newTrendSearch, parameters));
            tasks.Add(FetchRelatedTopicsAsync(newTrendSearch, parameters));
            tasks.Add(FetchRelatedQueriesAsync(newTrendSearch, parameters));

            if (isComparison)
            {
                tasks.Add(FetchRegionComparisonAsync(newTrendSearch, parameters));
            }
            else
            {
                tasks.Add(FetchInterestByRegionAsync(newTrendSearch, parameters));
            }

            await Task.WhenAll(tasks);

            await _trendSearchesRepo.CreateAsync(newTrendSearch);
            await _unitOfWork.SaveChangesAsync();

            return newTrendSearch;
        }

        // --- TẠO JSON CHO AI ---
        private string ReconstructJsonForAi(TrendSearch trendData, List<AdsPlannerItemDto> adsData)
        {
            var topAdsData = adsData.Take(50).ToList(); // Lấy top 50 để AI đánh giá

            var dataForAI = new
            {
                SearchParameters = new
                {
                    trendData.Query,
                    trendData.Geolocation,
                    trendData.Timeframe
                },
                GoogleTrendsData = new
                {
                    InterestOverTime = trendData.InterestOverTimes.Select(iot => new { iot.Query, iot.DateRange, iot.InterestValue }),
                    InterestByRegion = trendData.InterestByRegions.Select(ibr => new { ibr.LocationName, ibr.InterestValue }),
                    RegionComparison = trendData.RegionComparisons.Select(rc => new { rc.LocationName, rc.Query, rc.InterestPercentage }),
                    RelatedTopics = trendData.RelatedTopics.Select(rt => new { rt.Category, rt.TopicTitle, rt.ValueString }),
                    RelatedQueries = trendData.RelatedQueries.Select(rq => new { rq.Category, rq.Query, rq.Value })
                },
                GoogleAdsPlannerData = topAdsData
            };

            return JsonSerializer.Serialize(dataForAI, new JsonSerializerOptions { WriteIndented = true });
        }

        // --- CÁC HÀM FETCH CON (HELPER) ---
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
                            trendSearch.InterestOverTimes.Add(new InterestOverTime
                            {
                                Query = value.Query,
                                DateRange = timeline.Date,
                                TimestampVal = long.Parse(timeline.Timestamp),
                                InterestValue = value.ExtractedValue
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Lỗi FetchInterestOverTimeAsync"); }
        }

        private async Task FetchRelatedTopicsAsync(TrendSearch trendSearch, TrendParameters parameters)
        {
            try
            {
                var response = await _serpApiService.GetRelatedTopicsAsync(parameters);
                if (response?.RelatedTopics == null) return;

                if (response.RelatedTopics.Top != null)
                {
                    foreach (var topic in response.RelatedTopics.Top)
                    {
                        trendSearch.RelatedTopics.Add(new RelatedTopic
                        {
                            Category = "top",
                            TopicTitle = topic.Topic.Title,
                            TopicType = topic.Topic.Type,
                            ExtractedValue = topic.ExtractedValue,
                            ValueString = topic.ValueString
                        });
                    }
                }
                if (response.RelatedTopics.Rising != null)
                {
                    foreach (var topic in response.RelatedTopics.Rising)
                    {
                        trendSearch.RelatedTopics.Add(new RelatedTopic
                        {
                            Category = "rising",
                            TopicTitle = topic.Topic.Title,
                            TopicType = topic.Topic.Type,
                            ExtractedValue = topic.ExtractedValue,
                            ValueString = topic.ValueString
                        });
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Lỗi FetchRelatedTopicsAsync"); }
        }

        private async Task FetchRelatedQueriesAsync(TrendSearch trendSearch, TrendParameters parameters)
        {
            try
            {
                var response = await _serpApiService.GetRelatedQueriesAsync(parameters);
                if (response?.RelatedQueries == null) return;

                if (response.RelatedQueries.Top != null)
                {
                    foreach (var query in response.RelatedQueries.Top)
                    {
                        trendSearch.RelatedQueries.Add(new RelatedQuery
                        {
                            Category = "top",
                            Query = query.Query,
                            Value = query.ExtractedValue
                        });
                    }
                }
                if (response.RelatedQueries.Rising != null)
                {
                    foreach (var query in response.RelatedQueries.Rising)
                    {
                        trendSearch.RelatedQueries.Add(new RelatedQuery
                        {
                            Category = "rising",
                            Query = query.Query,
                            Value = query.ExtractedValue
                        });
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Lỗi FetchRelatedQueriesAsync"); }
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
                        trendSearch.InterestByRegions.Add(new InterestByRegion
                        {
                            LocationName = region.Location,
                            InterestValue = region.ExtractedValue
                        });
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Lỗi FetchInterestByRegionAsync"); }
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
                        foreach (var value in region.Values)
                        {
                            trendSearch.RegionComparisons.Add(new RegionComparison
                            {
                                LocationName = region.Location,
                                Query = value.Query,
                                InterestPercentage = value.ExtractedValue
                            });
                        }
                    }
                }
            }
            catch (Exception ex) { _logger.LogWarning(ex, "Lỗi FetchRegionComparisonAsync"); }
        }

        // === HÀM BỊ THIẾU (DÁN VÀO CUỐI CLASS) ===

        // === CẬP NHẬT HÀM NÀY TRONG TrendSearchService.cs ===

        public async Task<List<AdsPlannerItemDto>> GetAdsKeywordsDetailAsync(
            int queryHistoryId,
            int currentUserId,
            bool onlySuggestions = false)
        {
            // 1. Tìm bản ghi lịch sử
            var history = await _queryHistoryRepo.GetAsync(
                filter: x => x.Id == queryHistoryId
            );

            if (history == null)
            {
                return new List<AdsPlannerItemDto>();
            }

            // --- KIỂM TRA BẢO MẬT (MỚI) ---
            // Nếu người dùng đang đăng nhập (currentUserId) khác với người tạo lịch sử (MemberId)
            // -> Chặn ngay lập tức (tránh trường hợp user 1 xem trộm data của user 2)
            if (history.MemberId != currentUserId)
            {
                throw new UnauthorizedAccessException("Bạn không có quyền xem dữ liệu này.");
            }
            // -----------------------------

            if (history.AdsSearchRequestId == null)
            {
                return new List<AdsPlannerItemDto>();
            }

            // 2. Lấy dữ liệu từ BẢNG CHA và INCLUDE BẢNG CON
            var adsRequest = await _adsRequestRepo.GetAsync(
                filter: x => x.Id == history.AdsSearchRequestId,
                includeProperties: "AdsKeywordData"
            );

            if (adsRequest == null || adsRequest.AdsKeywordData == null)
            {
                return new List<AdsPlannerItemDto>();
            }

            var query = adsRequest.AdsKeywordData.AsQueryable();

            if (onlySuggestions)
            {
                // Chỉ lấy cái nào AI khuyên dùng
                query = query.Where(x => x.AiSuggestion == true);
            }

            // 4. Map sang DTO
            return query.Select(x => new AdsPlannerItemDto
            {
                Keyword = x.Keyword,
                AvgSearchVolume = x.AvgSearchVolume,
                Competition = x.Competition,
                LowBid = x.LowBid,
                HighBid = x.HighBid,
                AiSuggestion = x.AiSuggestion,
                AiMessage = x.AiMessage
            }).ToList();
        }

        public async Task<PaginationResult<List<QueryHistory>>> GetQueryHistoriesAsync(int memberId, int currentPage, int pageSize)
        {
            // Gọi hàm có sẵn trong QueryHistoryRepository của bạn
            return await _queryHistoryRepo.GetQueryHistorisWithPaginateAsync(memberId, currentPage, pageSize);
        }

    }
}