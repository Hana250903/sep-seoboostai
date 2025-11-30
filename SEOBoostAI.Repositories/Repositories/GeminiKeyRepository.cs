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

        public async Task UpdateKeyUsageAsync(int keyId, int requestsToAdd, int tokensToAdd, DateTime resetDate)
        {
            var key = await _context.GeminiKeys.FindAsync(keyId);
            if (key != null)
            {
                // Logic Lazy Reset: Nếu ngày trong DB khác ngày truyền vào -> Reset về 0
                if (key.LastResetDate.Date < resetDate.Date)
                {
                    key.RequestsUsedToday = 0;
                    key.TokensUsedToday = 0;
                    key.LastResetDate = resetDate;
                }

                key.RequestsUsedToday += requestsToAdd;
                key.TokensUsedToday += tokensToAdd;
                
                _context.GeminiKeys.Update(key);
            }
        }

        public async Task MarkKeyRateLimitedAsync(int keyId, DateTime until)
        {
            var key = await _context.GeminiKeys.FindAsync(keyId);
            if (key != null)
            {
                key.RateLimitedUntil = until;
                _context.GeminiKeys.Update(key);
            }
        }
    }
}
