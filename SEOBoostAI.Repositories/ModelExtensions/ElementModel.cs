using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
    public class AiElementAnalysis
    {
        public int ElementID { get; set; }
        public bool HasSuggestion { get; set; }
        public string Description { get; set; }
        public string AIRecommendation { get; set; }
    }

    public class ElementViewModel
    {
        public string AuditId { get; set; }
        public string Title { get; set; }
        public string ExtractedEvidenceJson { get; set; }
        public bool HasSuggestion { get; set; }
        public string AIRecommendation { get; set; }
        public string Description { get; set; }
    }
}
