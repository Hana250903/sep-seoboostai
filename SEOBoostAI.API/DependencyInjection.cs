using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;

namespace SEOBoostAI.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddWebAPIServices(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IElementService, ElementService>();
            services.AddScoped<IPerformanceService, PerformanceService>();
            services.AddScoped<IContentOptimizationService, ContentOptimizationService>();
            services.AddScoped<IFeedbackService, FeedbackService>();
			services.AddScoped<UserRepository>();
            services.AddScoped<ElementRepository>();
            services.AddScoped<PerformanceRepository>();
            services.AddScoped<ContentOptimizationRepository>();
            services.AddScoped<FeedbackRepository>();
			services.AddHttpClient();
            return services;
        }
    }
}
