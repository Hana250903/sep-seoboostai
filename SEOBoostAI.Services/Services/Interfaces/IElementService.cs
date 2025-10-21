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
        Task<int> CreateAsync(Element element);
        Task<int> UpdateAsync(Element element);
        Task<bool> DeleteAsync(int id);
    }
}
