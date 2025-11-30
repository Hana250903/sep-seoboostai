using AutoMapper;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using SEOBoostAI.Repository.Models;
using System.Text.Json;

namespace SEOBoostAI.API.Mappers
{
    public class MappingProfile : Profile
    {
        /// <summary>
        /// Configures bidirectional mapping between repository and API request models for the Element type.
        /// </summary>
        public MappingProfile()
        {
			CreateMap<ContentOptimization, ContentOptimizationDto>()
				.ForMember(dest => dest.AiData, opt => opt.MapFrom((src, dest) => {
					if (string.IsNullOrEmpty(src.AIResponse)) return null;
					try
					{
						return JsonSerializer.Deserialize<AiOptimizationResponse>(src.AIResponse,
							new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					}
					catch
					{
						return null; // Hoặc new AiOptimizationResponse() nếu muốn
					}
				}))
				// 2. Map trường UserRequest (Chuỗi JSON -> Object OptimizeRequestDto)
				.ForMember(dest => dest.UserRequest, opt => opt.MapFrom((src, dest) =>
				{
					if (string.IsNullOrEmpty(src.UserRequest)) return null;
					try
					{
						return JsonSerializer.Deserialize<OptimizeRequestDto>(src.UserRequest,
							new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
					}
					catch
					{
						return null;
					}
				}));
		}
    }
}
