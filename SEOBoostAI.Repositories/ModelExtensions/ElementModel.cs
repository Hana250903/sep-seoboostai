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

    //public class ElementRequest
    //{
    //    public int ElementID { get; set; }
    //    public string TagName { get; set; }
    //    public string InnerHtml { get; set; }
    //    public string OuterHtml { get; set; }
    //}

    public class ElementRequest
    {
        public int ElementID { get; set; }      // Để map kết quả về
        public string TagName { get; set; }
        public string Context { get; set; }     // Ví dụ: "LCP Candidate", "Script src=..."
        public Dictionary<string, string> Attributes { get; set; } = new(); // Chỉ chứa width, height, alt, loading...
    }

    public class AiElementAnalysis
    {
        public int ElementID { get; set; }
        public bool HasSuggestion { get; set; }
        public bool Important { get; set; }
        public string Description { get; set; }
        public string AIRecommendation { get; set; }
    }

    public class ElementViewModel
    {
        public string TagName { get; set; }
        public string InnerText { get; set; }
        public string OuterHTML { get; set; }
        public bool Important { get; set; }
        public bool HasSuggestion { get; set; }
        public string AIRecommendation { get; set; }
        public string Description { get; set; }
    }
}
