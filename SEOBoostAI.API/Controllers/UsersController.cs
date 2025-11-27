using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.API.ViewModels.RequestModels;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // GET: api/<UsersController>
        [HttpGet]
        public async Task<IEnumerable<User>> Get()
        {
            return await _userService.GetUsersAsync();
        }

        [HttpGet("filter")]
        public async Task<IActionResult> Get([FromQuery]UserRequestModel userRequestModel)
        {
            try
            {
                var result = await _userService.GetUsersWithPaginateAsync(userRequestModel.CurrentPage, userRequestModel.PageSize, userRequestModel.Role, userRequestModel.IsBanned, userRequestModel.IsDeleted);
                return Ok(new ResultModel<PaginationResult<List<User>>>
                {
                    Success = true,
                    Message = "Users retrieved successfully.",
                    Data = result
                });
            } 
            catch (Exception ex)
            {
                return BadRequest(new ResultModel<PaginationResult<List<User>>>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        // GET api/<UsersController>/5
        [HttpGet("{id}")]
        public async Task<User> Get(int id)
        {
            return await _userService.GetUserByIdAsync(id);
        }

        // POST api/<UsersController>
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] User user)
        {
            await _userService.CreateAsync(user);
            return Ok(user);
        }

        [HttpPut("update-role/{id}")]
        public async Task<IActionResult> Put(int id)
        {
            try
            {
                var result = await _userService.UpdateUserToStaff(id);
                return Ok(new ResultModel<User>
                {
                    Success = true,
                    Message = "User role updated to Staff successfully.",
                    Data = result
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ResultModel<User>
                {
                    Success = false,
                    Message = ex.Message,
                    Data = null
                });
            }
        }

        // PUT api/<UsersController>
        [HttpPut]
        public async Task<IActionResult> Put([FromBody] User user)
        {
            await _userService.UpdateAsync(user);
            return Ok(user);
        }

        // DELETE api/<UsersController>/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _userService.DeleteAsync(id);
            return Ok();
        }
    }
}
