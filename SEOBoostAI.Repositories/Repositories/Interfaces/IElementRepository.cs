using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories.Interfaces
{
    public interface IElementRepository : IGenericRepository<Element>
    {
        Task<PaginationResult<List<Element>>> GetElementWithPaginateAsync(int currentPage, int pageSize);
        Task<List<Element>> GetElementsImportantByAnalysisCacheIdAsync(int analysisCacheId);
        Task<List<Element>> GetElementsByAnalysisCacheIdAsync(int analysisCacheId);
        Task DeleteElementsForCacheAsync(int analysisCacheId);
    }
}
