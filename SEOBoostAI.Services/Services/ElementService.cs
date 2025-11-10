using HtmlAgilityPack;
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
        private readonly ICrawlingService _crawlingService;
        private readonly IPerformanceRepository _performanceRepository;
        private readonly IGeminiAIService _geminiAIService;

        public ElementService(IElementRepository elementRepository, IUnitOfWork unitOfWork, 
            ICrawlingService crawlingService, IPerformanceRepository performanceRepository,
            IGeminiAIService geminiAIService)
        {
            _elementRepository = elementRepository;
            _unitOfWork = unitOfWork;
            _crawlingService = crawlingService;
            _performanceRepository = performanceRepository;
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
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private List<ElementFinding> CheckElement(HtmlDocument htmlDoc, string url)
        {
            var lists = new List<ElementFinding>();

            var lcp = _crawlingService.CheckLCP(htmlDoc);
            foreach (var element in lcp)
            {
                lists.Add(element);
            }

            var cls = _crawlingService.CheckCLS(htmlDoc);
            foreach (var element in cls)
            {
                lists.Add(element);
            }

            var fcp = _crawlingService.CheckFCP(htmlDoc);
            foreach (var element in fcp)
            {
                lists.Add(element);
            }

            var tbt = _crawlingService.FindThirdPartyScripts(htmlDoc, url);
            foreach (var element in tbt)
            {
                lists.Add(element);
            }

            return lists;
        }

        public async Task<List<Element>> GetElement(int performanceId, string url)
        {
            var htmlDoc = await _crawlingService.GetHtmlDocumentAsync(url);
            
            var elements = new List<Element>();

            var lists = CheckElement(htmlDoc, url);

            foreach (var item in lists)
            {
                elements.Add(new Element
                {
                    PerformanceID = performanceId,
                    TagName = item.TagName,
                    InnerText = item.InnerHtml,
                    OuterHTML = item.OuterHtml,
                    HasSuggestion = true,
                    Important = true
                });
            }

            await _elementRepository.CreateRangeAsync(elements);
            await _unitOfWork.SaveChangesAsync();

            return elements;
        }

        public async Task<List<Element>> Suggestion(int performanceID)
        {
            var performance = await _performanceRepository.GetPerformanceAsync(performanceID);

            List<Element> elements = (List<Element>)performance.Elements;
            List<ElementRequest> requests = new List<ElementRequest>();

            foreach (var item in elements)
            {
                requests.Add(new ElementRequest
                {
                    ElementID = item.ElementID,
                    TagName = item.TagName,
                    InnerHtml = item.InnerText,
                    OuterHtml = item.OuterHTML
                });
            }

            var geminiResponse = await _geminiAIService.SuggestionElement(requests);

            elements = elements.Join(geminiResponse,
                original => original.ElementID,
                suggestion => suggestion.ElementID,
                (original, suggestion) => new Element
                {
                    ElementID = original.ElementID,
                    TagName = original.TagName,
                    Attribute = original.Attribute,
                    Status = original.Status,
                    Important = original.Important,
                    Description = suggestion.Description,
                    AIRecommendation = suggestion.AIRecommendation,
                    HasSuggestion = original.HasSuggestion,
                    CreatedAt = original.CreatedAt,
                    InnerText = original.InnerText,
                    IsDeleted = original.IsDeleted,
                    OuterHTML = original.OuterHTML,
                    PerformanceID = original.PerformanceID,
                    UpdatedAt = original.UpdatedAt,
                }).ToList();
            try
            {
                await _elementRepository.UpdateRangeAsync(elements);
                await _unitOfWork.SaveChangesAsync();

                return elements;
            }
            catch(Exception ex)
            {
                throw new Exception("Lỗi khi update kết quả Element vào DB.", ex);
            }
        }
    }
}
