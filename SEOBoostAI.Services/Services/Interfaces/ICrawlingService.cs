using HtmlAgilityPack;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface ICrawlingService
    {
        Task<HtmlDocument> GetHtmlDocumentAsync(string url);
        List<ElementFinding> CheckLCP(HtmlDocument htmlDoc);
        List<ElementFinding> CheckCLS(HtmlDocument htmlDoc);
        List<ElementFinding> CheckFCP(HtmlDocument htmlDoc);
        List<ElementFinding> FindThirdPartyScripts(HtmlDocument htmlDoc, string originalUrl);
    }
}
