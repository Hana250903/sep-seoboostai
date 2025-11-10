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

    public class RelatedQueriesRepository : GenericRepository<RelatedQuery>, IRelatedQueriesRepository
    {
        public RelatedQueriesRepository(SEP_SEOBoostAIContext conent) : base(conent) { }
    }

}
