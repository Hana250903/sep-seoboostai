using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Ultils;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthensController : ControllerBase
    {
        private readonly IAuthenService _authenService;
        private readonly ICurrentUserService _currentUserService;

        public AuthensController(IAuthenService authenService, ICurrentUserService currentUserService)
        {
            _authenService = authenService;
            _currentUserService = currentUserService;
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
                var user = _currentUserService.GetUserId();

                var result = await _authenService.LogOut(refreshToken, user);

                return Ok(result);

            }
            catch (Exception ex)
            {
                return BadRequest(ex);
            }

        }
    }
}
