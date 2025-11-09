using SEOBoostAI.Repository.ModelExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IPageSpeedService
    {
        Task<PageSpeedResponse> GetPageSpeedAsync(string url, string strategy = "desktop");
    }
}
