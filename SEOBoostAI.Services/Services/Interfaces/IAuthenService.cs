using SEOBoostAI.Repository.ModelExtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services.Interfaces
{
    public interface IAuthenService
    {
        /// <summary>
        /// Authenticates a user using a Google credential and returns an authentication token or session identifier.
        /// </summary>
        /// <param name="credential">The Google credential string obtained from the client.</param>
        /// <returns>A task that resolves to an authentication token or session identifier.</returns>
        public Task<ResultModel> LoginWithGoogle(string credential);
        /// <summary>
        /// Asynchronously logs out a user by invalidating the specified refresh token.
        /// </summary>
        /// <param name="refreshToken">The refresh token to be invalidated.</param>
        /// <param name="userId">The ID of the user to log out.</param>
        /// <returns>A task that resolves to true if logout is successful; otherwise, false.</returns>
        public Task<bool> LogOut(string refreshToken, int userId);
    }
}
