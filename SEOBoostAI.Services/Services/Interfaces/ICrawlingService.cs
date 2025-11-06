using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface ICrawlingService
    {
        /// <summary>
        /// Tải nội dung HTML từ một URL và parse thành đối tượng HtmlDocument
        /// </summary>
        /// <param name="url">URL của trang web</param>
        /// <returns>Đối tượng HtmlDocument của HtmlAgilityPack</returns>
        Task<HtmlDocument> GetHtmlDocumentAsync(string url);
    }
}
