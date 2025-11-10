using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Repository.ModelExtensions
{
    public class ElementFinding
    {
        public string TagName { get; set; }
        public string InnerHtml { get; set; }
        public string OuterHtml { get; set; }
    }

    public class ElementRequest
    {
        public int ElementID { get; set; }
        public string TagName { get; set; }
        public string InnerHtml { get; set; }
        public string OuterHtml { get; set; }
    }

    public class AiElementAnalysis
    {
        public int ElementID { get; set; }
        public string Description { get; set; }
        public string AIRecommendation { get; set; }
    }
}
