using SEOBoostAI.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IGeminiAiKeywordService
    {

        Task<TrendParameters> ExtractKeywordsFromQuestionAsync(string originalQuestion);

    }
}
