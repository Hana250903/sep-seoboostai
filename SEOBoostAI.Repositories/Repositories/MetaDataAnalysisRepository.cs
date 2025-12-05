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
    public class MetaDataAnalysisRepository : GenericRepository<MetaDataAnalysis>, IMetaDataAnalysisRepository
    {
        public MetaDataAnalysisRepository(SEP_SEOBoostAIContext context) : base(context)
        {
        }

        public async Task<MetaDataAnalysis> GetMetaDataAnalysisAsync(int id)
        {
            return await _context.Set<MetaDataAnalysis>()
                .Include(m => m.MetaDataSuggestions)
                    .ThenInclude(s => s.MetaTagSuggestionDetails)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task DeleteMetaDataAnalysesForCacheAsync(int analysisCacheId)
        {
            await _context.Set<MetaDataAnalysis>()
                .Where(m => m.AnalysisCacheID == analysisCacheId)
                .ExecuteDeleteAsync();
        }

        public async Task<MetaDataAnalysis> GetByAnalysisCacheIdAsync(int analysisCacheId)
        {
            return await _context.Set<MetaDataAnalysis>().Include(md => md.MetaDataSuggestions).ThenInclude(mds => mds.MetaTagSuggestionDetails).FirstOrDefaultAsync(md => md.AnalysisCacheID == analysisCacheId);
        }

        public async Task<List<MetaDataAnalysis>> GetAllMetaDataAnalysesAsync()
        {
            return await _context.Set<MetaDataAnalysis>()
                .Include(m => m.MetaDataSuggestions)
                    .ThenInclude(s => s.MetaTagSuggestionDetails)
                .ToListAsync();
        }
    }
}
