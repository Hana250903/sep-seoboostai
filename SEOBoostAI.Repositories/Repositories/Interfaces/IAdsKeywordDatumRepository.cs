using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories.Interfaces
{

    public interface IAdsKeywordDatumRepository : IGenericRepository<AdsKeywordDatum>
    {
        Task UpdateAiEvaluationAsync(int requestId, string keyword, bool isPotential, string message);
    }

}
