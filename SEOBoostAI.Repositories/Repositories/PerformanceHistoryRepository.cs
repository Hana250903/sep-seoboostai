using SEOBoostAI.Repository.GenericRepository;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.Repositories
{
    public class PerformanceHistoryRepository : GenericRepository<PerformanceHistory>, IPerformanceHistoryRepository
    {
        public PerformanceHistoryRepository(SEP_SEOBoostAIContext context) : base(context)
        {
        }

        
    }
}
