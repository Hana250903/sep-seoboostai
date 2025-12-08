using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SEOBoostAI.API.Controllers
{
    [Route("api/gemini-keys")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class GeminiKeysController : ControllerBase
    {
        private readonly IGeminiKeyService _geminiKeyService;

        public GeminiKeysController(IGeminiKeyService geminiKeyService)
        {
            _geminiKeyService = geminiKeyService;
        }

        // GET: api/gemini-keys
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GeminiKey>>> GetAllKeys()
        {
            var keys = await _geminiKeyService.GetAllActiveKeysAsync();
            return Ok(keys);
        }

        // GET: api/gemini-keys/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<GeminiKey>> GetKeyById(int id)
        {
            try
            {
                var key = await _geminiKeyService.GetKeyByIdAsync(id);

                if (key == null)
                {
                    return NotFound(new { message = "Gemini key không tồn tại" });
                }

                return Ok(key);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Tạo mới một Gemini Key.
        /// </summary>
        /// <remarks>
        /// Dùng để thêm một API Key mới vào hệ thống. 
        /// 
        /// Mẫu request body:
        /// 
        ///     POST /api/GeminiKey
        ///     {
        ///        "apiKey": "AIzaSyD...",
        ///        "keyName": "Key cho Marketing",
        ///        "rpmLimit": 60
        ///     }
        ///     
        /// </remarks>
        /// <param name="geminiKey">Đối tượng GeminiKey cần tạo</param>
        /// <returns>Đối tượng GeminiKey vừa được tạo kèm ID</returns>
        /// <response code="201">Tạo thành công</response>
        /// <response code="400">Dữ liệu không hợp lệ hoặc lỗi server</response>
        [HttpPost]
        [Produces("application/json")] // Chỉ định format trả về
        [ProducesResponseType(typeof(GeminiKey), StatusCodes.Status201Created)] // Mô tả code 201
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)] // Mô tả code 400
        public async Task<ActionResult<GeminiKey>> CreateKey([FromBody] GeminiKey geminiKey)
        {
            try
            {
                var createdKey = await _geminiKeyService.CreateKeyAsync(geminiKey);
                return CreatedAtAction(nameof(GetKeyById), new { id = createdKey.Id }, createdKey);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/gemini-keys/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateKey(int id, [FromBody] GeminiKey geminiKey)
        {
            if (id != geminiKey.Id)
            {
                return BadRequest(new { message = "ID không khớp" });
            }

            try
            {
                await _geminiKeyService.UpdateKeyAsync(geminiKey);
                return Ok(geminiKey);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // DELETE: api/gemini-keys/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteKey(int id)
        {
            try
            {
                await _geminiKeyService.DeleteKeyAsync(id);
                return Ok(new { message = "Đã xóa Gemini key thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH: api/gemini-keys/{id}/toggle-active
        [HttpPatch("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            try
            {
                await _geminiKeyService.ToggleActiveAsync(id);
                return Ok(new { message = "Đã toggle trạng thái key thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET: api/gemini-keys/usage-stats
        [HttpGet("usage-stats")]
        public async Task<ActionResult> GetUsageStats()
        {
            try
            {
                var stats = await _geminiKeyService.GetUsageStatsAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // POST: api/gemini-keys/{id}/reset-usage
        [HttpPost("{id}/reset-usage")]
        public async Task<IActionResult> ResetUsage(int id)
        {
            try
            {
                await _geminiKeyService.ResetUsageAsync(id);
                return Ok(new { message = "Đã reset usage counters thành công" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
