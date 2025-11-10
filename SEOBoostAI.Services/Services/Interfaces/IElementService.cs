using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IElementService
    {
        Task<List<Element>> GetElementsAsync();
        Task<PaginationResult<List<Element>>> GetElementsWithPaginateAsync(int currentPage, int pageSize);
        Task<Element> GetElementByIdAsync(int id);
        Task CreateAsync(Element element);
        Task CreateRangeAsync(List<Element> elements);
        Task UpdateAsync(Element element);
        Task UpdateRangeAsync(List<Element> elements);
        Task DeleteAsync(int id);
        Task ShortDeleteRangeAsync(List<Element> ids);
        Task<List<Element>> GetElement(int performanceId, string url);
        Task<List<Element>> Suggestion(int performanceID);
    }
}
