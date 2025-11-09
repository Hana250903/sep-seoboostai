using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class ElementService : IElementService
    {
        private readonly IElementRepository _elementRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ElementService(IElementRepository elementRepository, IUnitOfWork unitOfWork)
        {
            _elementRepository = elementRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task CreateAsync(Element element)
        {
            try
            {
                await _elementRepository.CreateAsync(element);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task CreateRangeAsync(List<Element> lists)
        {
            try
            {
                await _elementRepository.CreateRangeAsync(lists);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task DeleteAsync(int id)
        {
            try
            {
                var element = await _elementRepository.GetByIdAsync(id);
                await _elementRepository.RemoveAsync(element);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
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
            return await _elementRepository.GetElementWithPaginateAsync(currentPage, pageSize);
        }

        public async Task ShortDeleteRangeAsync(List<Element> elements)
        {
            try
            {
                await _elementRepository.ShortDeleteRangeAsync(elements);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task UpdateAsync(Element element)
        {
            try
            {
                await _elementRepository.UpdateAsync(element);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task UpdateRangeAsync(List<Element> elements)
        {
            try
            {
                await _elementRepository.UpdateRangeAsync(elements);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

    }
}
