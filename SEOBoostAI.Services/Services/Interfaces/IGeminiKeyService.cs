using SEOBoostAI.Repository.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IGeminiKeyService
    {
        Task<IEnumerable<GeminiKey>> GetAllActiveKeysAsync();
        Task<GeminiKey> GetKeyByIdAsync(int id);
        Task<GeminiKey> CreateKeyAsync(GeminiKey geminiKey);
        Task UpdateKeyAsync(GeminiKey geminiKey);
        Task DeleteKeyAsync(int id);
        Task ToggleActiveAsync(int id);
        Task ResetUsageAsync(int id);
        Task<object> GetUsageStatsAsync();
    }
}
