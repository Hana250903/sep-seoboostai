using SEOBoostAI.Service.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface ISerpApiService
    {
        // Chúng ta sẽ tạo 5 hàm, mỗi hàm cho 1 loại data_type
        // (Lưu ý: Bạn sẽ cần tạo các DTO Response tương ứng cho 4 hàm còn lại)

        Task<SerpInterestOverTimeResponse> GetInterestOverTimeAsync(TrendParameters parameters);

        Task<SerpInterestByRegionResponse> GetInterestByRegionAsync(TrendParameters parameters);
        Task<SerpRelatedTopicsResponse> GetRelatedTopicsAsync(TrendParameters parameters);
        Task<SerpRelatedQueriesResponse> GetRelatedQueriesAsync(TrendParameters parameters);
        Task<SerpRegionComparisonResponse> GetComparedBreakdownByRegionAsync(TrendParameters parameters);
    }
}
