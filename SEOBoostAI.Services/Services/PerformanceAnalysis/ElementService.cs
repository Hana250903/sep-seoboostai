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
    /// <summary>
    /// ElementService - Quản lý Elements và Deep Element Analysis
    /// 
    /// FLOW CHÍNH:
    /// 1. Suggestion() - Phân tích chuyên sâu bằng AI, đưa ra AIRecommendation
    /// 2. GetElementsByAnalysisCacheIdAsync() - Lấy danh sách issues quan trọng
    /// </summary>
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

        /// <summary>
        /// DEEP ELEMENT ANALYSIS - Phân tích chuyên sâu từng Element
        /// 
        /// Flow:
        /// 1. Lấy danh sách Elements từ AnalysisCache
        /// 2. Chia batch (50 elements/batch), xử lý song song (5 batch = 250 elements)
        /// 3. Gửi Gemini AI phân tích từng element
        /// 4. Update kết quả vào DB
        /// 5. Trả về danh sách elements có suggestion
        /// 
        /// Output cho mỗi element:
        /// - HasSuggestion: Có cần sửa không?
        /// - Description: Mô tả vấn đề
        /// - AIRecommendation: Gợi ý cách fix
        /// </summary>
        public async Task<List<ElementViewModel>> Suggestion(int analysisCacheID)
        {
            // ===== BƯỚC 1: LẤY ELEMENTS =====
            var elements = await _elementRepository.GetElementsByAnalysisCacheIdAsync(analysisCacheID);
            if (!elements.Any()) return null;

            // ===== BƯỚC 2: GỌI GEMINI AI =====
            // AI sẽ phân tích từng element theo loại:
            // - img: kiểm tra alt, width, height, lazy load
            // - a: kiểm tra href, aria-label
            // - link: kiểm tra render-blocking, preconnect
            // - script: kiểm tra async/defer
            var geminiResults = await _geminiAIService.SuggestionElement(elements);

            // ===== BƯỚC 3: MAP KẾT QUẢ AI VÀO ELEMENTS =====
            foreach (var aiResult in geminiResults)
            {
                var targetElement = elements.FirstOrDefault(e => e.ElementID == aiResult.ElementID);

                if (targetElement != null)
                {
                    // Cập nhật thông tin từ AI
                    targetElement.HasSuggestion = aiResult.HasSuggestion;     // Có cần sửa không?
                    targetElement.Description = aiResult.Description;          // Mô tả vấn đề
                    targetElement.AIRecommendation = aiResult.AIRecommendation; // Gợi ý cách fix
                    targetElement.UpdatedAt = DateTime.UtcNow.AddHours(7);
                }
            }
            // ===== BƯỚC 4: LƯU VÀO DATABASE =====
            try
            {
                await _elementRepository.UpdateRangeAsync(elements);
                await _unitOfWork.SaveChangesAsync();

                // ===== BƯỚC 5: TRẢ VỀ ELEMENTS CÓ SUGGESTION =====
                // Lọc chỉ lấy elements có HasSuggestion = true (để hiển thị cho user)
                var elementViewModels = new List<ElementViewModel>();
                foreach (var element in elements)
                {
                    if (element.HasSuggestion)
                    {
                        elementViewModels.Add(new ElementViewModel
                        {
                            AuditId = element.AuditId,                    // Loại issue (img-missing-alt...)
                            Title = element.Title,                        // Tiêu đề vấn đề
                            ExtractedEvidenceJson = element.ExtractedEvidenceJson, // Đoạn code lỗi
                            HasSuggestion = element.HasSuggestion,        // Có suggestion
                            AIRecommendation = element.AIRecommendation,  // Gợi ý fix từ AI
                            Description = element.Description             // Mô tả chi tiết
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

        /// <summary>
        /// Lấy danh sách Elements QUAN TRỌNG cho Auto-Fix
        /// Chỉ lấy elements có HasSuggestion = true và Important = true
        /// </summary>
        public async Task<List<Element>> GetElementsByAnalysisCacheIdAsync(int analysisCacheId)
        {
            return await _elementRepository.GetElementsImportantByAnalysisCacheIdAsync(analysisCacheId);
        }
    }
}
