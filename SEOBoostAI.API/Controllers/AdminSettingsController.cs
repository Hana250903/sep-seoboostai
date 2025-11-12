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

        [HttpGet]
        public IActionResult GetAllSettings()
        {
            var settings = _systemConfigService.GetAllSettings();
            return Ok(settings);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateSetting([FromBody] UpdateSettingRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _systemConfigService.UpdateValueAsync(request.Key, request.Value);
                return Ok(new { message = $"Đã cập nhật '{request.Key}' thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Đã xảy ra lỗi máy chủ khi cập nhật.");
            }
        }
    }
}
