using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Helpers;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    public class MetaDataAnalysisService : IMetaDataAnalysisService
    {
        private readonly IMetaDataAnalysisRepository _metaDataAnalysisRepository;
        private readonly IMetaDataSuggestionRepository _metaDataSuggestionRepository;
        private readonly IMetaTagSuggestionDetailRepository _metaTagSuggestionDetailRepository;
        private readonly IGeminiAIService _geminiAIService;
        private readonly IUnitOfWork _unitOfWork;

        public MetaDataAnalysisService(
            IMetaDataAnalysisRepository metaDataAnalysisRepository,
            IMetaDataSuggestionRepository metaDataSuggestionRepository,
            IMetaTagSuggestionDetailRepository metaTagSuggestionDetailRepository,
            IGeminiAIService geminiAIService,
            IUnitOfWork unitOfWork)
        {
            _metaDataAnalysisRepository = metaDataAnalysisRepository;
            _metaDataSuggestionRepository = metaDataSuggestionRepository;
            _metaTagSuggestionDetailRepository = metaTagSuggestionDetailRepository;
            _geminiAIService = geminiAIService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Phân tích metadata của URL với AI và lưu kết quả vào database
        /// </summary>
        /// <param name="analysisCacheId">ID của AnalysisCache để link (optional)</param>
        /// <returns>MetaDataAnalysis entity đã được tạo</returns>
        public async Task<MetaDataAnalysis> AnalyzeMetaDataAsync(int analysisCacheId)
        {
            var metaData = await _metaDataAnalysisRepository.GetByAnalysisCacheIdAsync(analysisCacheId);

            var aiAnalysisResult = await _geminiAIService.AnalyzeMetaDataSEO(metaData);

            var suggestion = await SaveAISuggestionsAsync(metaData.Id, aiAnalysisResult);
            metaData.MetaDataSuggestions = suggestion != null ? new List<MetaDataSuggestion> { suggestion } : new List<MetaDataSuggestion>();

            return metaData;
        }

        /// <summary>
        /// Lưu kết quả AI analysis vào MetaDataSuggestion và MetaTagSuggestionDetail
        /// </summary>
        private async Task<MetaDataSuggestion> SaveAISuggestionsAsync(int metaDataAnalysisId, MetaDataAnalysisResult aiResult)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var metaDataSuggestion = new MetaDataSuggestion
                {
                    MetaDataAnalysisId = metaDataAnalysisId,
                    GeneralAssessment = aiResult.GeneralAssessment,
                    CreatedAt = DateTime.UtcNow
                };


                var details = new List<MetaTagSuggestionDetail>();

                if (aiResult.Suggestions != null && aiResult.Suggestions.Count > 0)
                {
                    foreach (var suggestion in aiResult.Suggestions)
                    {
                        details.Add(new MetaTagSuggestionDetail
                        {
                            MetaDataSuggestionId = metaDataSuggestion.Id,
                            TagName = suggestion.TagName,
                            CurrentValue = suggestion.CurrentValue,
                            Issue = suggestion.Issue,
                            Recommendation = suggestion.Recommendation,
                            IsImportant = suggestion.IsImportant,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                metaDataSuggestion.MetaTagSuggestionDetails = details;

                await _metaDataSuggestionRepository.CreateAsync(metaDataSuggestion);
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                return metaDataSuggestion;
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw new Exception($"Lỗi khi lưu xuống database: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy MetaDataAnalysis theo ID kèm suggestions
        /// </summary>
        public async Task<MetaDataAnalysis> GetMetaDataAnalysisWithIdAsync(int id)
        {
            return await _metaDataAnalysisRepository.GetMetaDataAnalysisAsync(id);
        }

        /// <summary>
        /// Lấy MetaDataAnalysis theo analysisCacheId
        /// </summary>
        public async Task<MetaDataAnalysis> GetMetaDataAnalysisByAnalysisCacheIdAsync(int analysisCacheId)
        {
            return await _metaDataAnalysisRepository.GetByAnalysisCacheIdAsync(analysisCacheId);
        }

        /// <summary>
        /// Lấy latest AI suggestions cho một MetaDataAnalysis
        /// </summary>
        public async Task<MetaDataSuggestion> GetLatestSuggestionAsync(int metaDataAnalysisId)
        {
            var suggestions = await _metaDataSuggestionRepository.GetByMetaDataAnalysisIdAsync(metaDataAnalysisId);
            return suggestions?.OrderByDescending(s => s.CreatedAt).FirstOrDefault();
        }

        /// <summary>
        /// Lấy tất cả MetaDataAnalyses
        /// </summary>
        public async Task<List<MetaDataAnalysis>> GetAllMetaDataAnalysesAsync()
        {
            return await _metaDataAnalysisRepository.GetAllMetaDataAnalysesAsync();
        }
    }
}
