using Microsoft.Extensions.Configuration;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.ViewModels;
using Microsoft.Extensions.Http;
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
		private readonly IUnitOfWork _unitOfWork;

		private readonly IHttpClientFactory _httpClientFactory;
		private readonly string _geminiApiKey;
		private readonly string _geminiApiUrl;

		public ContentOptimizationService(
			IContentOptimizationRepository contentOptimizationRepository, 
			IUnitOfWork unitOfWork, 
			IConfiguration configuration,
			IHttpClientFactory httpClientFactory)
		{
			_contentOptimizationRepository = contentOptimizationRepository;
			_unitOfWork = unitOfWork;
			_httpClientFactory = httpClientFactory;
			_geminiApiKey = configuration["Gemini:ApiKey"];
			_geminiApiUrl = configuration["Gemini:ApiUrl"];
		}

		public async Task CreateAsync(ContentOptimization content)
		{
			try
			{
				await _contentOptimizationRepository.CreateAsync(content);
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
				var content = await _contentOptimizationRepository.GetByIdAsync(id);
				await _contentOptimizationRepository.RemoveAsync(content);
				await _unitOfWork.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public async Task<ContentOptimization> GetContentOptimizationByIdAsync(int id)
		{
			return await _contentOptimizationRepository.GetByIdAsync(id);
		}

		public async Task<List<ContentOptimization>> GetContentOptimizationsAsync()
		{
			return await _contentOptimizationRepository.GetAllAsync();
		}

		public async Task<PaginationResult<List<ContentOptimization>>> GetContentOptimizationsWithPaginateAsync(int currentPage, int pageSize)
		{
			return await _contentOptimizationRepository.GetContentOptimizationWithPaginateAsync(currentPage, pageSize);
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

		public async Task<ContentOptimization> OptimizeAndCreateAsync(OptimizeRequestDto request)
		{
			// 1. Build Prompt (Không đổi)
			string prompt = BuildGeminiPrompt(request);

			// Gọi hàm riêng bằng HttpClient
			AiOptimizationResponse aiResponse = await CallGeminiManuallyAsync(prompt);

			// 4. Create Entity (Không đổi)
			var newOptimization = new ContentOptimization
			{
				UserID = request.UserId,
				Keyword = request.Keyword,
				OriginalContent = request.Content,
				ContentLenght = request.ContentLength,
				OptimizationLevel = request.OptimizationLevel,
				ReadabilityLevel = request.ReadabilityLevel,
				OptimizedContent = aiResponse.OptimizedContent,
				SEOScore = aiResponse.SeoScore,
				Readability = aiResponse.Readability,
				Engagement = aiResponse.Engagement,
				Originality = aiResponse.Originality,
				CreatedAt = DateTime.UtcNow,
				IsDeleted = false
			};

			// 5. Save to Database (Không đổi)
			try
			{
				await _contentOptimizationRepository.CreateAsync(newOptimization);
				await _unitOfWork.SaveChangesAsync();
				return newOptimization;
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Failed to save optimization results to database.", ex);
			}
		}

		private string BuildGeminiPrompt(OptimizeRequestDto request)
		{
			string citationText = request.IncludeCitation ? "Yes, add high-quality citations to support claims." : "No, do not add external citations.";

			// Dùng C# 11+ Raw String Literals (dấu """...""")
			return $$"""
                You are an expert SEO content strategist and a highly skilled content writer.
                Your task is to optimize and rewrite the provided content based on specific criteria.
                The goal is to improve its search engine ranking and user engagement for the target keyword.

                Here are the details for content optimization:
                - **Target Keyword:** '{{request.Keyword}}'
                - **Original Content:**
                ```
                {{request.Content}}
                ```
                - **Desired Content Length:** {{request.ContentLength}} (Options: Short, Medium, Long, Comprehensive, In-depth)
                - **Optimization Level:** {{request.OptimizationLevel}} (Level 1: Basic, Level 2: Moderate, Level 3: Advanced, Level 4: Expert, Level 5: Aggressive)
                - **Desired Readability Level:** {{request.ReadabilityLevel}} (Options: Easy, Medium, Hard, Advanced, Expert)
                - **Include Citations:** {{citationText}}

                Based on these criteria, please provide the **fully optimized and rewritten content** along with estimated scores for SEO, Readability, Engagement, and Originality.
                Focus on natural language, user intent, and SEO best practices without keyword stuffing.
                Ensure the content logically flows and maintains its original core message while significantly enhancing its quality and SEO performance.

                **Your response must be in strict JSON format**, as a single object, with the following fields:
                - `optimized_content` (string): The fully rewritten and optimized content.
                - `seo_score` (integer): An estimated SEO score from 0-100...
                - `readability` (integer): An estimated readability score from 0-100...
                - `engagement` (integer): An estimated engagement score from 0-100...
                - `originality` (integer): An estimated originality score from 0-100...
                """;
		}

		private async Task<AiOptimizationResponse> CallGeminiManuallyAsync(string prompt)
		{
			// 1. Tạo Request Body (Không đổi)
			var requestBody = new
			{
				contents = new[]
				{
					new { parts = new[] { new { text = prompt } } }
				},
				generationConfig = new
				{
					responseMimeType = "application/json"
				}
			};

			// 2. Chuẩn bị HttpClient và Content (Không đổi)
			var client = _httpClientFactory.CreateClient();
			var httpContent = new StringContent(
				JsonSerializer.Serialize(requestBody),
				Encoding.UTF8,
				"application/json"); // <- Header 'Content-Type'

			// 3. Chuẩn bị Request Message (Cách mới)
			// URL bây giờ là URL gốc, KHÔNG CÓ "?key=..."
			var requestMessage = new HttpRequestMessage(HttpMethod.Post, _geminiApiUrl);

			// Thêm API Key vào HEADER, giống hệt ảnh cURL
			requestMessage.Headers.Add("x-goog-api-key", _geminiApiKey);
			requestMessage.Content = httpContent;

			// 4. Gửi Request bằng SendAsync (thay vì PostAsync)
			HttpResponseMessage response = await client.SendAsync(requestMessage);

			if (!response.IsSuccessStatusCode)
			{
				string errorContent = await response.Content.ReadAsStringAsync();
				throw new HttpRequestException($"Lỗi từ Gemini API: {response.StatusCode}. Chi tiết: {errorContent}");
			}

			// 5. Phân tích (Parse) Response (Không đổi)
			string jsonResponse = await response.Content.ReadAsStringAsync();
			var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

			var geminiResponse = JsonSerializer.Deserialize<GeminiApiResponse>(jsonResponse, options);
			string aiJsonText = geminiResponse.Candidates[0].Content.Parts[0].Text;

			AiOptimizationResponse aiResult = JsonSerializer.Deserialize<AiOptimizationResponse>(aiJsonText, options);
			return aiResult;
		}
		private class GeminiApiResponse { public GeminiCandidate[] Candidates { get; set; } }
		private class GeminiCandidate { public GeminiContent Content { get; set; } }
		private class GeminiContent { public GeminiPart[] Parts { get; set; } }
		private class GeminiPart { public string Text { get; set; } }
	}
}
