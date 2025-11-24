using SEOBoostAI.Service.DTOs;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IGeminiAiGoogleAdsService
    {
        // Nhận vào: Lời khuyên của AI 1 + Dữ liệu Ads thô
        // Trả về: List đánh giá để lưu DB
        Task<List<AdsEvaluationItem>> EvaluateAdsKeywordsAsync(string aiAdvice, List<AdsPlannerItemDto> adsData);
    }
}