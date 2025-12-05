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
    public class MetaDataSuggestionRepository : GenericRepository<MetaDataSuggestion>, IMetaDataSuggestionRepository
    {
        public MetaDataSuggestionRepository(SEP_SEOBoostAIContext context) : base(context)
        {
        }

        public async Task<List<MetaDataSuggestion>> GetByMetaDataAnalysisIdAsync(int metaDataAnalysisId)
        {
            return await _context.Set<MetaDataSuggestion>().Include(m => m.MetaTagSuggestionDetails)
                .Where(s => s.MetaDataAnalysisId == metaDataAnalysisId)
                .ToListAsync();
        }
    }
}
