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

        /// <summary>
        /// Lấy danh sách tất cả các Gemini Key đang hoạt động.
        /// </summary>
        /// <remarks>
        /// API này trả về danh sách toàn bộ key (thường dùng cho trang quản trị).
        /// </remarks>
        /// <returns>Danh sách các đối tượng GeminiKey</returns>
        /// <response code="200">Lấy dữ liệu thành công</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GeminiKey>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<GeminiKey>>> GetAllKeys()
        {
            var keys = await _geminiKeyService.GetAllActiveKeysAsync();
            return Ok(keys);
        }

        /// <summary>
        /// Lấy chi tiết thông tin của một Gemini Key theo ID.
        /// </summary>
        /// <param name="id">ID của Key cần lấy</param>
        /// <returns>Đối tượng GeminiKey chi tiết</returns>
        /// <response code="200">Tìm thấy key</response>
        /// <response code="404">Không tìm thấy key với ID cung cấp</response>
        /// <response code="400">Lỗi không mong muốn</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GeminiKey), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
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

        // POST: api/gemini-keys
        [HttpPost]
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

        /// <summary>
        /// Cập nhật thông tin một Gemini Key hiện có.
        /// </summary>
        /// <remarks>
        /// Lưu ý: ID trên URL phải khớp với ID trong body. Các trường không gửi sẽ bị ghi đè (tùy logic service).
        /// </remarks>
        /// <param name="id">ID của Key cần sửa</param>
        /// <param name="geminiKey">Object chứa thông tin cập nhật</param>
        /// <returns>Object đã cập nhật</returns>
        /// <response code="200">Cập nhật thành công</response>
        /// <response code="400">ID không khớp hoặc dữ liệu lỗi</response>
        /// <response code="404">Không tìm thấy Key để sửa</response>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(GeminiKey), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Xóa một Gemini Key khỏi hệ thống.
        /// </summary>
        /// <param name="id">ID của Key cần xóa</param>
        /// <returns>Thông báo kết quả</returns>
        /// <response code="200">Xóa thành công</response>
        /// <response code="404">Không tìm thấy Key</response>
        /// <response code="400">Lỗi server</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Bật/Tắt trạng thái hoạt động (IsActive) của Key.
        /// </summary>
        /// <remarks>
        /// Dùng để tạm dừng sử dụng một Key mà không cần xóa nó.
        /// </remarks>
        /// <param name="id">ID của Key</param>
        /// <returns>Thông báo thành công</returns>
        /// <response code="200">Đã đổi trạng thái thành công</response>
        /// <response code="404">Không tìm thấy Key</response>
        [HttpPatch("{id}/toggle-active")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Lấy thống kê sử dụng của các Key.
        /// </summary>
        /// <remarks>
        /// Trả về tổng quan số lượng request, token đã dùng của hệ thống.
        /// </remarks>
        /// <returns>Object thống kê</returns>
        /// <response code="200">Thành công</response>
        [HttpGet("usage-stats")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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

        /// <summary>
        /// Reset bộ đếm sử dụng (Requests/Tokens) của một Key về 0.
        /// </summary>
        /// <remarks>
        /// Dùng để reset thủ công hạn mức trong ngày của Key nếu cần thiết.
        /// </remarks>
        /// <param name="id">ID của Key</param>
        /// <returns>Thông báo thành công</returns>
        /// <response code="200">Reset thành công</response>
        /// <response code="404">Không tìm thấy Key</response>
        [HttpPost("{id}/reset-usage")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
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
