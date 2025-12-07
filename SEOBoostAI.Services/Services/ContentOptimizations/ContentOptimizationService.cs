using AutoMapper;
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

namespace SEOBoostAI.Service.Services.ContentOptimizations
{
	public class ContentOptimizationService : IContentOptimizationService
	{
		private readonly IContentOptimizationRepository _contentOptimizationRepository;
		private readonly IUserRepository _userRepository;
		private readonly IFeatureRepository _featureRepository;
		private readonly IUnitOfWork _unitOfWork;
		private readonly IGeminiContentOptimizer _geminiService;
		private readonly IUserMonthlyFreeQuotaService _userMonthlyFreeQuotaService;
		private readonly IMapper _mapper;

		public ContentOptimizationService(
			IContentOptimizationRepository contentOptimizationRepository, 
			IUserRepository userRepository, IFeatureRepository featureRepository,
			IUserMonthlyFreeQuotaService userMonthlyFreeQuotaService,
			IUnitOfWork unitOfWork, IGeminiContentOptimizer geminiService,
			IMapper mapper)
		{
			_contentOptimizationRepository = contentOptimizationRepository;
			_featureRepository = featureRepository;
			_userRepository = userRepository;
			_userMonthlyFreeQuotaService = userMonthlyFreeQuotaService;
			_unitOfWork = unitOfWork;
			_geminiService = geminiService;
			_mapper = mapper;
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
			return _mapper.Map<List<ContentOptimizationDto>>(entities);
		}

		public async Task<List<ContentOptimizationDto>> GetContentOptimizationsAsync()
		{
			var entities = await _contentOptimizationRepository.GetAllAsync();

			var sortedEntities = entities.OrderByDescending(co => co.CreatedAt);
			return _mapper.Map<List<ContentOptimizationDto>>(sortedEntities);
		}

		public async Task<PaginationResult<List<ContentOptimizationDto>>> GetContentOptimizationsWithPaginateAsync(SearchTransactionRequest searchRequest, int userId)
		{
			var paginateResult = await _contentOptimizationRepository.GetContentOptimizationWithPaginateAsync(searchRequest, userId);

			var dtos = _mapper.Map<List<ContentOptimizationDto>>(paginateResult.Items);

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

			// 3. Gọi Service AI
			var aiResponse = await _geminiService.OptimizeContentAsync(request);

			// 4. Create Entity
			var newOptimization = new ContentOptimization
			{
				UserID = userId,
				Model = "gemini-2.5-flash",
				UserRequest = JsonSerializer.Serialize(request, jsonOptions),
				AIResponse = JsonSerializer.Serialize(aiResponse, jsonOptions),
				CreatedAt = DateTime.UtcNow,
				IsDeleted = false
			};

			// 5. Save to Database & TRỪ LƯỢT (Increment Usage)
			try
			{
				await _contentOptimizationRepository.CreateAsync(newOptimization);

				// QUAN TRỌNG: Tăng số lần sử dụng lên 1
				await _userMonthlyFreeQuotaService.IncrementUsageCount(userId, request.FeatureId);

				await _unitOfWork.SaveChangesAsync(); // Lưu cả 2 việc (tạo bài viết + trừ lượt) cùng lúc

				return _mapper.Map<ContentOptimizationDto>(newOptimization);
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Failed to save optimization results or update quota.", ex);
			}
		}
	}
}
