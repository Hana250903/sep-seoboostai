using Azure.Core;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
	public class ContentOptimizationService : IContentOptimizationService
	{
		private readonly IContentOptimizationRepository _contentOptimizationRepository;
		private readonly IUserRepository _userRepository;
		private readonly IFeatureRepository _featureRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGeminiAIService _geminiService;
		private readonly IUserMonthlyFreeQuotaService _userMonthlyFreeQuotaService;

		public ContentOptimizationService(
			IContentOptimizationRepository contentOptimizationRepository, IUserRepository userRepository, IFeatureRepository featureRepository,
			IUserMonthlyFreeQuotaService userMonthlyFreeQuotaService,
			IUnitOfWork unitOfWork,IGeminiAIService geminiService)
		{
			_contentOptimizationRepository = contentOptimizationRepository;
			_featureRepository = featureRepository;
			_userRepository = userRepository;
			_userMonthlyFreeQuotaService = userMonthlyFreeQuotaService;
			_unitOfWork = unitOfWork;
			_geminiService = geminiService;
		}

		public async Task DeleteAsync(int id)
		{
			try
			{
				var content = await _contentOptimizationRepository.GetByIdAsync(id);
				await _contentOptimizationRepository.RemoveAsync(content);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task<List<ContentOptimizationDto>> GetContentOptimizationsByUserIdAsync(int userId)
		{
			// Gọi hàm Repository bạn vừa viết
			var entities = await _contentOptimizationRepository.GetAllByUserIdAsync(userId);

			if (entities == null || !entities.Any())
			{
				return new List<ContentOptimizationDto>();
			}

			// Map sang DTO
			return entities.Select(entity => MapToDto(entity))
						   .Where(dto => dto != null)
						   .ToList();
		}

		public async Task<List<ContentOptimizationDto>> GetContentOptimizationsAsync()
		{
			var entities = await _contentOptimizationRepository.GetAllAsync();

			// "Giải mã" và "Map" hàng loạt
			var dtos = entities.Select(entity => MapToDto(entity))
							   .Where(dto => dto != null) // Lọc bỏ lỗi (nếu có)
							   .OrderByDescending(co => co.CreatedAt)
							   .ToList();
			return dtos;
		}

		public async Task<PaginationResult<List<ContentOptimizationDto>>> GetContentOptimizationsWithPaginateAsync(SearchTransactionRequest searchRequest, int userId)
		{
			var paginateResult = await _contentOptimizationRepository.GetContentOptimizationWithPaginateAsync(searchRequest, userId);

			var dtos = paginateResult.Items.Select(entity => MapToDto(entity))
										   .Where(dto => dto != null)
										   .ToList();

			// Tạo lại kết quả phân trang với DTO
			return new PaginationResult<List<ContentOptimizationDto>>
			{
				Items = dtos,
				TotalItems = paginateResult.TotalItems,
				CurrentPage = paginateResult.CurrentPage,
				PageSize = paginateResult.PageSize,
				TotalPages = paginateResult.TotalPages
			};
		}

		public async Task UpdateAsync(ContentOptimization contentOptimization)
		{
			try
			{
				await _contentOptimizationRepository.UpdateAsync(contentOptimization);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task<ContentOptimizationDto> OptimizeAndCreateAsync(OptimizeRequestDto request, int userId)
		{
			// BƯỚC 2: Kiểm tra Quota (Check Limit)
			bool canUse = await _userMonthlyFreeQuotaService.CheckLimit(userId, request.FeatureId);

			if (!canUse)
			{
				// Nếu hết lượt, ném lỗi để Controller bắt và trả về 402 hoặc 403
				throw new InvalidOperationException("Bạn đã hết lượt sử dụng miễn phí trong tháng này.");
			}

			// --- NẾU CÒN LƯỢT THÌ CHẠY TIẾP ---

			// Cấu hình để KHÔNG mã hóa tiếng Việt
			var jsonOptions = new JsonSerializerOptions
			{
				Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				WriteIndented = false
			};

			// 3. Gọi Service AI (Như cũ)
			var aiResponse = await _geminiService.OptimizeContentAsync(request);

			// 4. Create Entity (Như cũ)
			var newOptimization = new ContentOptimization
			{
				UserID = userId,
				Model = "gemini-2.0-flash",
				UserRequest = JsonSerializer.Serialize(request, jsonOptions),
				AIResponse = JsonSerializer.Serialize(aiResponse, jsonOptions),
				CreatedAt = DateTime.UtcNow.AddHours(7),
				IsDeleted = false
			};

			// 5. Save to Database & TRỪ LƯỢT (Increment Usage)
			try
			{
				await _contentOptimizationRepository.CreateAsync(newOptimization);

				// QUAN TRỌNG: Tăng số lần sử dụng lên 1
				await _userMonthlyFreeQuotaService.IncrementUsageCount(userId, request.FeatureId);

				await _unitOfWork.SaveChangesAsync(); // Lưu cả 2 việc (tạo bài viết + trừ lượt) cùng lúc

				return MapToDto(newOptimization);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Failed to save optimization results or update quota.", ex);
			}
		}

		private ContentOptimizationDto MapToDto(ContentOptimization entity)
		{
			if (entity == null) return null;

			AiOptimizationResponse aiData = null;
			if (!string.IsNullOrEmpty(entity.AIResponse))
			{
				try
				{
					// Dùng JsonSerializer để "GIẢI MÃ" chuỗi khó đọc
					aiData = JsonSerializer.Deserialize<AiOptimizationResponse>(entity.AIResponse,
								new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				}
				catch (Exception ex)
				{
					// Xử lý nếu JSON trong DB bị lỗi
					// Bạn có thể log lỗi 'ex' ở đây
					aiData = null; // Hoặc new AiOptimizationResponse { OptimizedContent = "Lỗi đọc JSON" };
				}
			}

			OptimizeRequestDto requestData = null;
			if (!string.IsNullOrEmpty(entity.UserRequest))
			{
				try 
				{
					requestData = JsonSerializer.Deserialize<OptimizeRequestDto>(entity.UserRequest,
								new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
				}
				catch (Exception ex)
				{
					// Xử lý nếu JSON trong DB bị lỗi
					// Bạn có thể log lỗi 'ex' ở đây
					requestData = null; // Hoặc new OptimizeRequestDto { Keyword = "Lỗi đọc JSON" };
				}
			}

			// Chuyển đổi (Map) sang DTO để trả về
			return new ContentOptimizationDto
			{
				ContentOptimizationID = entity.ContentOptimizationID,
				UserID = entity.UserID,
				Model = entity.Model,
				UserRequest = requestData, // Giữ nguyên string JSON
				AiData = aiData, // <-- Gán ĐỐI TƯỢNG "dễ đọc"
				CreatedAt = entity.CreatedAt
			};
		}
	}
}
