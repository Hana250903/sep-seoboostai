using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
    public class AnalysisResultViewModel
    {
        public List<Element> Elements { get; set; }
        public MetaDataAnalysis MetaDataAnalysis { get; set; }
    }
}
