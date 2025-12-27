using HtmlAgilityPack;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Helpers;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.PerformanceAnalysis
{
    public class ElementService : IElementService
    {
        private readonly IElementRepository _elementRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAnalysisCacheRepository _analysisCacheRepository;
        private readonly IGeminiAIService _geminiAIService;

        public ElementService(IElementRepository elementRepository, IUnitOfWork unitOfWork, 
            IAnalysisCacheRepository analysisCacheRepository,
            IGeminiAIService geminiAIService)
        {
            _elementRepository = elementRepository;
            _unitOfWork = unitOfWork;
            _analysisCacheRepository = analysisCacheRepository;
            _geminiAIService = geminiAIService;
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
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task DeleteElementsForCacheAsync(int analysisCacheId)
        {
            try
            {
                await _elementRepository.DeleteElementsForCacheAsync(analysisCacheId);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<List<ElementViewModel>> Suggestion(int analysisCacheID)
        {
            var elements = await _elementRepository.GetElementsByAnalysisCacheIdAsync(analysisCacheID);
            if (!elements.Any()) return null;

            var geminiResults = await _geminiAIService.SuggestionElement(elements);
            
            foreach (var aiResult in geminiResults)
            {
                // Tìm phần tử tương ứng trong list đang track
                var targetElement = elements.FirstOrDefault(e => e.ElementID == aiResult.ElementID);

                if (targetElement != null)
                {
                    targetElement.HasSuggestion = aiResult.HasSuggestion;
                    targetElement.Description = aiResult.Description;
                    targetElement.AIRecommendation = aiResult.AIRecommendation;
                    targetElement.UpdatedAt = DateTime.UtcNow.AddHours(7);
                }
            }
            try
            {
                await _elementRepository.UpdateRangeAsync(elements);
                await _unitOfWork.SaveChangesAsync();

                var elementViewModels = new List<ElementViewModel>();
                foreach (var element in elements)
                {
                    if (element.HasSuggestion)
                    {
                        elementViewModels.Add(new ElementViewModel
                        {
                            AuditId = element.AuditId,
                            Title = element.Title,
                            ExtractedEvidenceJson = element.ExtractedEvidenceJson,
                            HasSuggestion = element.HasSuggestion,
                            AIRecommendation = element.AIRecommendation,
                            Description = element.Description
                        });
                    }                    
                }

                return elementViewModels;
            }
            catch (Exception ex)
            {
                throw new Exception("Lỗi khi update kết quả Element vào DB.", ex);
            }
        }

        public async Task<List<Element>> GetElementsByAnalysisCacheIdAsync(int analysisCacheId)
        {
            return await _elementRepository.GetElementsImportantByAnalysisCacheIdAsync(analysisCacheId);
        }
    }
}
