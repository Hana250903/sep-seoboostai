using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Ultils;
using System.Configuration;

namespace SEOBoostAI.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWebAPIServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IElementRepository, ElementRepository>();
            services.AddScoped<IPerformanceRepository, PerformanceRepository>();
            services.AddScoped<IContentOptimizationRepository, ContentOptimizationRepository>();
            services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            services.AddScoped<ISystemConfigRepository, SystemConfigRepository>();  
            
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IElementService, ElementService>();
            services.AddScoped<IPerformanceService, PerformanceService>();
            services.AddScoped<IContentOptimizationService, ContentOptimizationService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
            services.AddScoped<IPageSpeedService, PageSpeedService>();
            services.AddScoped<ICrawlingService, CrawlingService>();
            services.AddSingleton<ISystemConfigService, SystemConfigService>();
            services.AddScoped<IGeminiAIService, GeminiAIService>();

            // test xong xóa ( 2 dòng dưới )
            services.AddScoped<IGeminiAiKeywordService, GeminiAiKeywordService>();
            services.AddScoped<IGeminiAiAnalysisService, GeminiAiAnalysisService>();
            services.AddScoped<ISerpApiService, SerpApiService>();
            services.AddScoped<ITrendSearchService, TrendSearchService>(); 
            services.AddScoped<IQueryHistoryRepository, QueryHistoryRepository>();
            services.AddScoped<ITrendSearchesRepository, TrendSearchesRepository>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddDbContext<SEP_SEOBoostAIContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            });
            services.AddHttpClient();
            services.AddLogging();
            return services;
        }
    }
}
