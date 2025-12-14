using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Utils;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/authen")]
    [ApiController]
    public class AuthensController : ControllerBase
    {
        private readonly IAuthenService _authenService;

        public AuthensController(IAuthenService authenService)
        {
            _authenService = authenService;
        }

        [HttpPost("login-member")]
        public async Task<IActionResult> LoginWithMember([FromBody] string credential)
        {
            try
            {
                var result = await _authenService.LoginWithMember(credential);
                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("login-staff")]
        public async Task<IActionResult> LoginWithStaff([FromBody] string credential)
        {
            try
            {
                var result = await _authenService.LoginWithStaff(credential);
                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("login-admin")]
        public async Task<IActionResult> LoginWithAdmin([FromBody] string credential)
        {
            try
            {
                var result = await _authenService.LoginWithAdmin(credential);
                if (result.Success)
                {
                    return Ok(result);
                }
                return BadRequest(result);
            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }
        }

        [HttpPost("log-out")]
        public async Task<IActionResult> LogOut(string refreshToken)
        {
            try
            {
                var userIdString = User.FindFirstValue("user_ID");
                if (!int.TryParse(userIdString, out int userId))
                {
                    return Unauthorized();
                }

                var result = await _authenService.LogOut(refreshToken, userId);

                return Ok(result);

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
    }
}
