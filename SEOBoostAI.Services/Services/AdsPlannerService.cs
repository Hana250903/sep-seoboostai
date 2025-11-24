using SEOBoostAI.Service.DTOs;
using SEOBoostAI.Service.Services.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging; // Nên thêm Logger để debug nếu cần

namespace SEOBoostAI.Service.Services
{
    public class AdsPlannerService : IAdsPlannerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISystemConfigService _systemConfigService; // <-- THÊM CÁI NÀY
        private readonly string _apiUrl;

        public AdsPlannerService(IHttpClientFactory httpClientFactory,
                                 ISystemConfigService systemConfigService) // <-- TIÊM VÀO ĐÂY
        {
            _httpClientFactory = httpClientFactory;
            _systemConfigService = systemConfigService;

            // Lấy URL từ DB. Nếu không có thì dùng giá trị mặc định.
            // Key: "giaadsapi" (như bạn yêu cầu)
            // Default Value: URL hiện tại của bạn
            _apiUrl = _systemConfigService.GetValue<string>("giaadsapi","");

        }

        public async Task<List<AdsPlannerItemDto>> GetAdsDataAsync(List<string> keywords)
        {
            // 1. Chuẩn bị HttpClient
            var httpClient = _httpClientFactory.CreateClient();

            // Tăng timeout lên xíu vì API này xử lý hơi lâu (Queue)
            httpClient.Timeout = TimeSpan.FromSeconds(60);

            // 2. Tạo body request: { "keywords": ["cà phê", "phở"] }
            var requestBody = new { keywords = keywords };

            try
            {
                // 3. Gọi API (Sử dụng _apiUrl lấy từ DB)
                var response = await httpClient.PostAsJsonAsync(_apiUrl, requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    // Log lỗi hoặc ném exception tùy bạn
                    return new List<AdsPlannerItemDto>(); // Trả về rỗng nếu lỗi
                }

                // 4. Đọc kết quả
                var result = await response.Content.ReadFromJsonAsync<AdsPlannerResponseDto>();

                if (result != null && result.Status == "success")
                {
                    return result.Data;
                }

                return new List<AdsPlannerItemDto>();
            }
            catch (Exception)
            {
                // Gặp lỗi mạng hoặc timeout thì trả về list rỗng để không làm sập app
                return new List<AdsPlannerItemDto>();
            }
        }
    }
}