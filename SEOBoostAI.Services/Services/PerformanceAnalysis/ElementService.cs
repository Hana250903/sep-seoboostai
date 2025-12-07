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

        private List<ElementFinding> CheckElement(HtmlDocument htmlDoc, string url)
        {
            var lists = new List<ElementFinding>();

            var lcp = CheckElementHelper.CheckLCP(htmlDoc);
            foreach (var element in lcp)
            {
                lists.Add(element);
            }

            var cls = CheckElementHelper.CheckCLS(htmlDoc);
            foreach (var element in cls)
            {
                lists.Add(element);
            }

            var fcp = CheckElementHelper.CheckFCP(htmlDoc);
            foreach (var element in fcp)
            {
                lists.Add(element);
            }

            var heading = CheckElementHelper.CheckHeadingStructure(htmlDoc);
            foreach (var element in heading)
            {
                lists.Add(element);
            }

            var altText = CheckElementHelper.CheckMissingAltText(htmlDoc);
            foreach (var element in altText)
            {
                lists.Add(element);
            }

            var tbt = CheckElementHelper.FindThirdPartyScripts(htmlDoc, url);
            foreach (var element in tbt)
            {
                lists.Add(element);
            }

            return lists;
        }

        public async Task<AnalysisResultViewModel> AnalyzeFullPageAsync(string url)
        {
            string htmlContent = await HtmlFetchHelper.FetchHtmlAsync(url, useSelenium: true);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            var metaData = HtmlOptimizerHelper.ExtractMetaData(htmlContent);

            var lists = CheckElement(htmlDoc, url);

            var elements = new List<Element>();
            foreach (var item in lists)
            {
                elements.Add(new Element
                {
                    TagName = item.TagName,
                    InnerText = item.InnerHtml,
                    OuterHTML = item.OuterHtml,
                    HasSuggestion = true,
                    Important = true
                });
            }

            var jsonOptions = new JsonSerializerOptions
            {
                // Chấp nhận mọi ký tự, không encode tiếng Việt
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = false
            };

            var metaDataAnalysis = new MetaDataAnalysis
            {
                Url = url,
                Title = metaData.Title,
                Description = metaData.Description,
                Keywords = metaData.Keywords,
                Charset = metaData.Charset,
                Viewport = metaData.Viewport,
                Canonical = metaData.Canonical,
                Robots = metaData.Robots,
                OpenGraphData = metaData.OpenGraph.Count > 0 ? JsonSerializer.Serialize(metaData.OpenGraph, jsonOptions) : null,
                TwitterCardData = metaData.TwitterCard.Count > 0 ? JsonSerializer.Serialize(metaData.TwitterCard, jsonOptions) : null,
                OtherMetaData = metaData.OtherMeta.Count > 0 ? JsonSerializer.Serialize(metaData.OtherMeta, jsonOptions) : null,
            };
            var result = new AnalysisResultViewModel
            {
                Elements = elements,
                MetaDataAnalysis = metaDataAnalysis
            };
            return result;
        }

        public async Task<List<ElementViewModel>> Suggestion(int analysisCacheID)
        {
            var analysisCache = await _analysisCacheRepository.GetAnalysisCacheAsync(analysisCacheID);

            var dbElements = analysisCache.Elements.ToList();
            if (!dbElements.Any()) return null;

            var requests = new List<ElementRequest>();

            foreach (var item in dbElements)
            {
                // Gọi hàm Helper ở bước 2 để trích xuất dữ liệu gọn nhẹ
                var req = HtmlOptimizerHelper.OptimizeForAi(item.ElementID, item.TagName, item.OuterHTML);
                requests.Add(req);
            }

            var geminiResults = await _geminiAIService.SuggestionElement(requests);
            
            foreach (var aiResult in geminiResults)
            {
                // Tìm phần tử tương ứng trong list đang track
                var targetElement = dbElements.FirstOrDefault(e => e.ElementID == aiResult.ElementID);

                if (targetElement != null)
                {
                    targetElement.HasSuggestion = aiResult.HasSuggestion;
                    targetElement.Important = aiResult.Important;
                    targetElement.Description = aiResult.Description;
                    targetElement.AIRecommendation = aiResult.AIRecommendation;
                    targetElement.UpdatedAt = DateTime.UtcNow;
                }
            }
            try
            {
                await _elementRepository.UpdateRangeAsync(dbElements);
                await _unitOfWork.SaveChangesAsync();

                var elementViewModels = new List<ElementViewModel>();
                foreach (var element in dbElements)
                {
                    if (element.HasSuggestion)
                    {
                        elementViewModels.Add(new ElementViewModel
                        {
                            TagName = element.TagName,
                            InnerText = element.InnerText,
                            OuterHTML = element.OuterHTML,
                            Important = element.Important,
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
