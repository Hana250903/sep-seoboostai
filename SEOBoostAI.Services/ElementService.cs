using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service
{
    public class ElementService : IElementService
    {
        private readonly ElementRepository _elementRepository;

        public ElementService(ElementRepository elementRepository)
        {
            _elementRepository = elementRepository;
        }

        public async Task<int> CreateAsync(Element element)
        {
            return await _elementRepository.CreateAsync(element);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var element = await _elementRepository.GetByIdAsync(id);
            return await _elementRepository.RemoveAsync(element);
        }

        public async Task<Element> GetElementByIdAsync(int id)
        {
            return await _elementRepository.GetByIdAsync(id);
        }

        public async Task<List<Element>> GetElementsAsync()
        {
            return await _elementRepository.GetAllAsync();
        }

        public async Task<PaginationResult<List<Element>>> GetElementsWithPaginateAsync(int currentPage, int pageSize)
        {
            return await _elementRepository.GetElementWithPaginateAsync(currentPage,pageSize);
        }

        public async Task<int> UpdateAsync(Element element)
        {
            return await _elementRepository.UpdateAsync(element);
        }
    }
}
