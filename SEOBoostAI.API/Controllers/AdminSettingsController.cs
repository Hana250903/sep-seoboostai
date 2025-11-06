using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/admin-settings")]
    [ApiController]
    public class AdminSettingsController : ControllerBase
    {
        private readonly ISystemConfigService _systemConfigService;

        public AdminSettingsController(ISystemConfigService systemConfigService)
        {
            _systemConfigService = systemConfigService;
        }

        // GET: api/<AdminSettingsController>
        [HttpGet]
        public IActionResult GetAllSettings()
        {
            // Hàm này đọc TRỰC TIẾP từ cache (Singleton)
            var settings = _systemConfigService.GetAllSettings();

            // Trả về một đối tượng JSON chứa tất cả cài đặt
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSetting([FromBody] UpdateSettingRequest request)
        {
            // Kiểm tra xem dữ liệu gửi lên có hợp lệ không (dựa trên DTO)
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Gọi service, nó sẽ tự động cập nhật cả DB và Cache
                await _systemConfigService.UpdateValueAsync(request.Key, request.Value);

                // Trả về thông báo thành công
                return Ok(new { message = $"Đã cập nhật '{request.Key}' thành công." });
            }
            catch (Exception ex)
            {
                // (Nên log lỗi ex ra file/console)
                return StatusCode(500, "Đã xảy ra lỗi máy chủ khi cập nhật.");
            }
        }
    }
}
