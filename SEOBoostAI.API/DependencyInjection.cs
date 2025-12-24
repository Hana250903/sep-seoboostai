using DinkToPdf;
using DinkToPdf.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SEOBoostAI.API.Hubs;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Helpers;
using SEOBoostAI.Service.Services.Configurations;
using SEOBoostAI.Service.Services.ContentOptimizations;
using SEOBoostAI.Service.Services.Feedbacks;
using SEOBoostAI.Service.Services.GithubServices;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Services.Payments;
using SEOBoostAI.Service.Services.PerformanceAnalysis;
using SEOBoostAI.Service.Services.SearchKeywords;
using SEOBoostAI.Service.Services.UserAndAuthen;
using SEOBoostAI.Service.Utils;
using System.Configuration;

namespace SEOBoostAI.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWebAPIServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IElementRepository, ElementRepository>();
            services.AddScoped<IAnalysisCacheRepository, AnalysisCacheRepository>();
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
			services.AddScoped<IContentOptimizationRepository, ContentOptimizationRepository>();
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();  
            services.AddScoped<IPerformanceHistoryRepository, PerformanceHistoryRepository>();
            services.AddScoped<ITransactionRepository, TransactionRepository>();
            services.AddScoped<IUserMonthlyFreeQuotaRepository, UserMonthlyFreeQuotaRepository>();
            services.AddScoped<IFeatureInformationRepository, FeatureInformationRepository>();
			services.AddScoped<IFeatureRepository, FeatureRepository>();
            services.AddScoped<IAnalysisSnapshotRepository, AnalysisSnapshotRepository>();
            services.AddScoped<IPurchasedFeatureRepository, PurchasedFeatureRepository>();
            services.AddScoped<IFeedbackMessageRepository, FeedbackMessageRepository>();
            services.AddScoped<ISpamCacheRepository, SpamCacheRepository>();

            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IElementService, ElementService>();
            services.AddScoped<IAnalysisCacheService, AnalysisCacheService>();
            services.AddScoped<IContentOptimizationService, ContentOptimizationService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<IPageSpeedService, PageSpeedService>();
            services.AddSingleton<ISystemConfigService, SystemConfigService>();
            services.AddScoped<IGeminiAIService, GeminiAIService>();
			services.AddScoped<IGeminiContentOptimizer, GeminiContentOptimizer>();
			services.AddScoped<IPerformanceHistoryService, PerformanceHistoryService>();
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<IUserMonthlyFreeQuotaService, UserMonthlyFreeQuotaService>();
            services.AddScoped<IFeatureService, FeatureService>();
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();
			services.AddScoped<IAuthenService, AuthenService>();
            services.AddScoped<IAnalysisSnapshotService, AnalysisSnapshotService>();
            services.AddScoped<IPurchasedFeatureService, PurchasedFeatureService>();
            services.AddScoped<IFeatureInformationService, FeatureInformationService>();
			services.AddScoped<IFeedbackMessageService, FeedbackMessageService>();
            services.AddScoped<IChatNotifier, SignalRChatNotifier>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<ISpamProtectionService, SpamProtectionService>();
            services.AddScoped<IEncryptionService, EncryptionService>();
            services.AddScoped<IPdfService, PdfService>();

			// test xong xóa ( 3 dòng dưới )
			services.AddScoped<IAdsPlannerService, AdsPlannerService>();
            services.AddScoped<IAdsSearchRequestRepository, AdsSearchRequestRepository>();
            services.AddScoped<IAdsKeywordDatumRepository, AdsKeywordDatumRepository>();
            services.AddScoped<IGeminiAiGoogleAdsService, GeminiAiGoogleAdsService>();


            services.AddScoped<IGeminiAiKeywordService, GeminiAiKeywordService>();
            services.AddScoped<IGeminiAiAnalysisService, GeminiAiAnalysisService>();
            services.AddScoped<ISerpApiService, SerpApiService>();
            services.AddScoped<ITrendSearchService, TrendSearchService>(); 
            services.AddScoped<IQueryHistoryRepository, QueryHistoryRepository>();
            services.AddScoped<ITrendSearchesRepository, TrendSearchesRepository>();

            // Gemini Rate Limit Management
            services.AddScoped<IGeminiKeyRepository, GeminiKeyRepository>();
            services.AddScoped<IEncryptionService, EncryptionService>(); // Encryption for Gemini API Keys
            services.AddScoped<IGeminiKeyService, GeminiKeyService>();
            services.AddSingleton<IGeminiRateLimitManager, GeminiRateLimitManager>();
            services.AddTransient<GeminiRateLimitHelper>();

            // Puppeteer + GitHub Integration Services (AutoFix)
            services.AddScoped<IPuppeteerAuditService, PuppeteerAuditService>();
            services.AddScoped<IGitHubIntegrationService, GitHubIntegrationService>();
            services.AddScoped<IGeminiFixService, GeminiFixService>();
            services.AddScoped<IAutoFixService, AutoFixService>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<ICompareUrlString, CompareUrlString>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
			services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));
			services.AddDbContext<SEP_SEOBoostAIContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
            services.AddHttpClient();
            services.AddLogging();
            services.AddMemoryCache();
            return services;
        }
    }
}
