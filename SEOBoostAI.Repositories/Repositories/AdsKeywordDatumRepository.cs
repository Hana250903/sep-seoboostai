using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
    public class AdsKeywordDatumRepository : GenericRepository<AdsKeywordDatum>, IAdsKeywordDatumRepository
    {
        public AdsKeywordDatumRepository(SEP_SEOBoostAIContext content) : base(content) { }

        public async Task UpdateAiEvaluationAsync(int requestId, string keyword, bool isPotential, string message)
        {
            // Chuẩn hóa từ khóa để so sánh chính xác
            string keywordTrimmed = keyword.Trim();

            // ExecuteUpdateAsync: Cập nhật trực tiếp tại Database
            // KHÔNG load dữ liệu về RAM -> Không bị lỗi Tracking -> Rất nhanh
            await _context.Set<AdsKeywordDatum>()
                .Where(x => x.AdsSearchRequestId == requestId && x.Keyword == keywordTrimmed)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.AiSuggestion, isPotential)
                    .SetProperty(x => x.AiMessage, message)
                );
        }
    }
}