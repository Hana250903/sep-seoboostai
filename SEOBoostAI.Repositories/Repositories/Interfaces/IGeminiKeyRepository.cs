using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories.Interfaces
{
    public interface IGeminiKeyRepository : IGenericRepository<GeminiKey>
    {
        Task<List<GeminiKey>> GetAllActiveKeysAsync();
        Task<GeminiKey> GetKeyByIdAsync(int keyId);
        Task UpdateKeyUsageAsync(int keyId, int tokensToAdd, DateTime resetDate);
        Task MarkKeyRateLimitedAsync(int keyId, DateTime until);
        Task AdjustTokenUsageAsync(int keyId, int tokenDifference);
        Task ResetKeySpecificUsageAsync(int keyId);
        Task<bool> ExistsAsync(int id);
    }
}
