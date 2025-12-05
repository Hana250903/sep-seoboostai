using Microsoft.EntityFrameworkCore;
using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
    public class GeminiKeyRepository : GenericRepository<GeminiKey>, IGeminiKeyRepository
    {
        public GeminiKeyRepository(SEP_SEOBoostAIContext context) : base(context)
        {
        }

        public async Task<List<GeminiKey>> GetAllActiveKeysAsync()
        {
            // Lấy tất cả key đang Active để đẩy lên Cache của tầng BLL
            return await _context.GeminiKeys
                                 .AsNoTracking() // Read-only cho nhanh
                                 .Where(k => k.IsActive)
                                 .ToListAsync();
        }

        public async Task<GeminiKey> GetKeyByIdAsync(int keyId)
        {
            return await _context.GeminiKeys
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(k => k.Id == keyId);
        }

        public async Task UpdateKeyUsageAsync(int keyId, int tokensToAdd, DateTime resetDate)
        {
            // Kiểm tra xem có cần reset không
            var key = await _context.GeminiKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(k => k.Id == keyId);
                
            if (key == null) return;
            
            if (key.LastResetDate.Date < resetDate.Date)
            {
                // Reset về 0 và set giá trị mới
                await _context.GeminiKeys
                    .Where(k => k.Id == keyId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(k => k.RequestsUsedToday, 1)
                        .SetProperty(k => k.TokensUsedToday, tokensToAdd)
                        .SetProperty(k => k.LastResetDate, resetDate)
                        .SetProperty(k => k.UpdatedAt, DateTime.UtcNow.AddHours(7)));
            }
            else
            {
                // Increment giá trị hiện tại
                await _context.GeminiKeys
                    .Where(k => k.Id == keyId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(k => k.RequestsUsedToday, k => k.RequestsUsedToday + 1)
                        .SetProperty(k => k.TokensUsedToday, k => k.TokensUsedToday + tokensToAdd)
                        .SetProperty(k => k.UpdatedAt, DateTime.UtcNow.AddHours(7)));
            }
        }

        public async Task MarkKeyRateLimitedAsync(int keyId, DateTime until)
        {
            await _context.GeminiKeys
                .Where(k => k.Id == keyId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(k => k.RateLimitedUntil, until)
                    .SetProperty(k => k.UpdatedAt, DateTime.UtcNow.AddHours(7)));
        }

        public async Task AdjustTokenUsageAsync(int keyId, int tokenDifference)
        {
            // Điều chỉnh token count bằng cách cộng/trừ difference (có thể âm hoặc dương)
            await _context.GeminiKeys
                .Where(k => k.Id == keyId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(k => k.TokensUsedToday, k => k.TokensUsedToday + tokenDifference)
                    .SetProperty(k => k.UpdatedAt, DateTime.UtcNow.AddHours(7)));
        }
    }
}
