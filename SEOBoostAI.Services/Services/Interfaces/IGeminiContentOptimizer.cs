using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.ModelExtensions.GeminiAIModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
	public interface IGeminiContentOptimizer
	{
		Task<AiOptimizationResponse> OptimizeContentAsync(OptimizeRequestDto request);
	}
}
