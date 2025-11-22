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

		public async Task<ContentOptimizationDto> GetContentOptimizationByIdAsync(int id)
		{
			var entity = await _contentOptimizationRepository.GetByIdAsync(id);
			if (entity == null) return null;

			// Gọi hàm helper (bước 4a) để "giải mã"
			return MapToDto(entity);
		}

		public async Task<List<ContentOptimizationDto>> GetContentOptimizationsAsync()
		{
			var entities = await _contentOptimizationRepository.GetAllAsync();

			// "Giải mã" và "Map" hàng loạt
			var dtos = entities.Select(entity => MapToDto(entity))
							   .Where(dto => dto != null) // Lọc bỏ lỗi (nếu có)
							   .ToList();
			return dtos;
		}

		public async Task<PaginationResult<List<ContentOptimizationDto>>> GetContentOptimizationsWithPaginateAsync(SearchTransactionRequest searchRequest)
		{
			var paginateResult = await _contentOptimizationRepository.GetContentOptimizationWithPaginateAsync(searchRequest);

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

		public async Task<ContentOptimizationDto> OptimizeAndCreateAsync(OptimizeRequestDto request)
		{
			// BƯỚC 1: Xác định Feature ID (Ví dụ: 1 là tính năng Optimize Content)
			// Bạn nên lưu số 1 này vào Const hoặc Enum cho dễ quản lý
			int featureId = 1;

			// BƯỚC 2: Kiểm tra Quota (Check Limit)
			// Gọi hàm CheckLimit từ QuotaService
			bool canUse = await _userMonthlyFreeQuotaService.CheckLimit(request.UserId, featureId);

			if (!canUse)
			{
				// Nếu hết lượt, ném lỗi để Controller bắt và trả về 402 hoặc 403
				throw new InvalidOperationException("Bạn đã hết lượt sử dụng miễn phí trong tháng này.");
			}

			// --- NẾU CÒN LƯỢT THÌ CHẠY TIẾP ---

			// 3. Gọi Service AI (Như cũ)
			var aiResponse = await _geminiService.OptimizeContentAsync(request);

			// 4. Create Entity (Như cũ)
			var newOptimization = new ContentOptimization
			{
				UserID = request.UserId,
				Model = "gemini-2.0-flash",
				UserRequest = JsonSerializer.Serialize(request),
				AIResponse = JsonSerializer.Serialize(aiResponse),
				CreatedAt = DateTime.UtcNow.AddHours(7),
				IsDeleted = false
			};

			// 5. Save to Database & TRỪ LƯỢT (Increment Usage)
			try
			{
				await _contentOptimizationRepository.CreateAsync(newOptimization);

				// QUAN TRỌNG: Tăng số lần sử dụng lên 1
				await _userMonthlyFreeQuotaService.IncrementUsageCount(request.UserId, featureId);

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

			// Chuyển đổi (Map) sang DTO để trả về
			return new ContentOptimizationDto
			{
				ContentOptimizationID = entity.ContentOptimizationID,
				UserID = entity.UserID,
				Model = entity.Model,
				UserRequest = entity.UserRequest, // Giữ nguyên string JSON
				AiData = aiData, // <-- Gán ĐỐI TƯỢNG "dễ đọc"
				CreatedAt = entity.CreatedAt
			};
		}
	}
}
