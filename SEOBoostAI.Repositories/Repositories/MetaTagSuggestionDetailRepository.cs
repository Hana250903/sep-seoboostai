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
    public class MetaTagSuggestionDetailRepository : GenericRepository<MetaTagSuggestionDetail>, IMetaTagSuggestionDetailRepository
    {
        public MetaTagSuggestionDetailRepository(SEP_SEOBoostAIContext context) : base(context)
        {
        }

        public async Task<List<MetaTagSuggestionDetail>> GetByMetaDataSuggestionIdAsync(int metaDataSuggestionId)
        {
            return await _context.Set<MetaTagSuggestionDetail>()
                .Where(d => d.MetaDataSuggestionId == metaDataSuggestionId)
                .ToListAsync();
        }
    }
}
