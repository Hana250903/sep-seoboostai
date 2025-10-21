using Microsoft.AspNetCore.Mvc;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Service.Services;
using SEOBoostAI.Service.Services.Interfaces;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SEOBoostAI.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        [HttpGet("{currentPage}/{pageSize}")]
        public async Task<PaginationResult<List<User>>> Get(int currentPage, int pageSize)
        {
            return await _userService.GetUsersWithPaginateAsync(currentPage, pageSize);
        }

        // GET api/<UsersController>/5
        [HttpGet("{id}")]
        public async Task<User> Get(int id)
        {
            return await _userService.GetUserByIdAsync(id);
        }

        // POST api/<UsersController>
        [HttpPost]
        public async Task<int> Post([FromBody] User user)
        {
            return await _userService.CreateAsync(user);
        }

        // PUT api/<UsersController>
        [HttpPut]
        public async Task<int> Put([FromBody] User user)
        {
            return await _userService.UpdateAsync(user);
        }

        // DELETE api/<UsersController>/5
        [HttpDelete("{id}")]
        public async Task<bool> Delete(int id)
        {
            return await _userService.DeleteAsync(id);
        }
    }
}
