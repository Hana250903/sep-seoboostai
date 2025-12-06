using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/admin-settings")]
    [ApiController]
    [Authorize(Roles = "Admin")]
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
            return Ok(new ResultModel<Dictionary<string, string>>
            {
                Data = settings,
                Message = "Lấy tất cả cài đặt hệ thống thành công.",
                Success = true
            });
        }

        [HttpGet("{featureId}")]
        public async Task<IActionResult> GetSettingsByFeatureID(int featureId)
        {
            var settings = await _systemConfigService.GetAllSettingsByFeatureIDAsync(featureId);
            return Ok(new ResultModel<List<SystemSetting>>
            {
                Data = settings,
                Message = $"Lấy cài đặt hệ thống cho FeatureID {featureId} thành công.",
                Success = true
            });
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
                await _systemConfigService.UpdateValueAsync(request.Key, request.Value, request.FeatureID);
                return Ok(new { message = $"Đã cập nhật '{request.Key}' thành công." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Đã xảy ra lỗi máy chủ khi cập nhật.");
            }
        }
    }
}
