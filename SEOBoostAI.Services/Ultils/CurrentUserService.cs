using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Ultils
{
    public interface ICurrentUserService
    {
        /// <summary>
        /// Retrieves the current user's unique identifier from the HTTP context claims.
        /// </summary>
        /// <returns>The user's ID as an integer.</returns>
        /// <exception cref="Exception">Thrown if the user is not authenticated or the claim is missing.</exception>
        int GetUserId();
        /// <summary>
        /// Retrieves the email address of the current authenticated user from the HTTP context claims.
        /// </summary>
        /// <returns>The user's email address, or null if the email claim is not present.</returns>
        String getUserEmail();
        /// <summary>
        /// Asynchronously retrieves the current authenticated user's full account information.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, containing the current user's <see cref="User"/> object.</returns>
        Task<User> GetCurrentAccountAsync();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserRepository _userRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="CurrentUserService"/> class with the specified HTTP context accessor.
        /// </summary>
        /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
        /// <param name="actionContextAccessor">Unused parameter for potential future use.</param>
        public CurrentUserService(IHttpContextAccessor httpContextAccessor, IActionContextAccessor actionContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Retrieves the current user's ID from the HTTP context claims.
        /// </summary>
        /// <returns>The user ID as an integer.</returns>
        /// <exception cref="Exception">Thrown if the user is not authenticated or the user ID claim is missing.</exception>
        public int GetUserId()
        {
            try
            {
                return int.Parse(_httpContextAccessor.HttpContext.User.FindFirst("user_ID")?.Value);
            }
            catch
            {
                throw new Exception("Login Before USE!!!!");
            }
        }
        /// <summary>
        /// Retrieves the email address of the current authenticated user from the HTTP context claims.
        /// </summary>
        /// <returns>The user's email address if present; otherwise, null.</returns>
        public String getUserEmail()
        {
            return _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value;
        }

        /// <summary>
        /// Asynchronously retrieves the current user's account information from the repository based on the authenticated user's ID.
        /// </summary>
        /// <returns>The <see cref="User"/> object for the current user, or null if not found.</returns>
        public async Task<User?> GetCurrentAccountAsync()
        {
            int userId = GetUserId();
            var account = await _userRepository.GetByIdAsync(userId);
            return account;

            //var user = _httpContextAccessor.HttpContext.User.Identity;
            //if(user == null)
            //{
            //    throw new Exception("Account is not found in the database.");
            //}
            //return (User)user;
        }

    }
}
