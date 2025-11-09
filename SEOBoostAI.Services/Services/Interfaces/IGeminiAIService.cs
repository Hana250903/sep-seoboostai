using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IGeminiAIService
    {
        Task<AiAssessment> SuggestionAnalysisPerformance(string metrics);
        Task<List<AiElementAnalysis>> SuggestionElement(List<ElementRequest> elements);
    }
}
