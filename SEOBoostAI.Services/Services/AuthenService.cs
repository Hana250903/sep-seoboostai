using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.Models;
using SEOBoostAI.Repository.Repositories;
using SEOBoostAI.Repository.Repositories.Interfaces;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using SEOBoostAI.Service.Ultils;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class AuthenService : IAuthenService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWalletRepositoriy _walletRepositoriy;
        private readonly IConfiguration _configuration;

        public AuthenService(IUserRepository userRepository, IUnitOfWork unitOfWork, IWalletRepositoriy walletRepositoriy, IConfiguration configuration)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _walletRepositoriy = walletRepositoriy;
            _configuration = configuration;
        }
        public async Task<ResultModel> LoginWithGoogle(string credential)
        {
            string clientId = _configuration["GoogleCredential:ClientId"];

            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string> { clientId }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);

            if (payload == null)
            {
                throw new Exception("Credential không hợp lệ");
            }

            var existUser = await _userRepository.GetUserByEmailAsync(payload.Email);

            //nếu có user
            if (existUser != null)
            {
                var accessToken = await GenerateAccessToken(existUser);
                var refreshToken = GenerateRefreshToken(existUser.Email);

                existUser.RefreshToken = refreshToken;

                await _userRepository.UpdateAsync(existUser);

                // Check if the user has an active subscription
                //var isExpired = await _userAccountSubscriptionRepository.IsExpired(existUser.Id);
                //if (isExpired)
                //{
                //    // If the subscription is expired, you can handle it here (e.g., notify the user, update UI, etc.)
                //    // For now, we just return the tokens.
                //    return new ResultModel()
                //    {
                //        Success = true,
                //        Message = "Login successfully, but user account subscription expired",
                //        AccessToken = accessToken,
                //        RefreshToken = refreshToken
                //    };
                //}

                //return new ResultModel()
                //{
                //    Success = true,
                //    Message = "Login successfully",
                //    AccessToken = accessToken,
                //    RefreshToken = refreshToken
                //};

            }
            else
            {
                try
                {
                    //User newUser = new User()
                    //{
                    //    Username = payload.Email.Split('@')[0].Trim(),
                    //    FullName = payload.Name,
                    //    Email = payload.Email,
                    //    Role = "User",
                    //    Avatar = payload.Picture,
                    //    Password = "".Trim(),
                    //    CreatedAt = DateTime.UtcNow,
                    //    GoogleId = payload.JwtId
                    //};

                    //    var accessToken = await GenerateAccessToken(newUser);
                    //    var refreshToken = GenerateRefreshToken(newUser.Email);

                    //    newUser.RefreshToken = refreshToken;

                    //    var result = await _userRepository.CreateAsync(newUser);

                    //    if (result > 0)
                    //    {
                    //        var user = await _userRepository.GetUserByEmailAsync(newUser.Email);

                    //        Wallet wallet = new Wallet()
                    //        {
                    //            Currency = 0.0M,
                    //            CreatedAt = DateTime.UtcNow,
                    //            UserId = user.Id
                    //        };

                    //        var walletResult = await _walletRepository.CreateAsync(wallet);
                    //        if (walletResult > 0)
                    //        {
                    //            UserAccountSubscription userAccountSubscription = new UserAccountSubscription()
                    //            {
                    //                UserId = user.Id,
                    //                AccountTypeId = 1,
                    //                StartDate = DateTime.UtcNow,
                    //                IsActive = true
                    //            };
                    //            var accountSubscriptionResult = await _userAccountSubscriptionRepository.CreateAsync(userAccountSubscription);
                    //            if (accountSubscriptionResult <= 0)
                    //            {
                    //                throw new Exception("Failed to create user account subscription");
                    //            }
                    //        }

                    //        return new ResultModel()
                    //        {
                    //            Success = true,
                    //            Message = "Login successfully",
                    //            AccessToken = accessToken,
                    //            RefreshToken = refreshToken
                    //        };
                    //    }

                    //    throw new Exception("");
                }
                catch
                {
                    throw;
                }
            }
            throw new NotImplementedException();
        }

        public Task<bool> LogOut(string refreshToken, int userId)
        {
            throw new NotImplementedException();
        }

        private async Task<string> GenerateAccessToken(User user)
        {
            var authClaims = new List<Claim>();

            authClaims.Add(new Claim("email", user.Email.ToString()));
            authClaims.Add(new Claim("fullname", user.FullName.ToString()));
            authClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            //authClaims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
            authClaims.Add(new Claim("user_ID", user.UserID.ToString()));
            authClaims.Add(new Claim("role", user.Role.ToString()));
            var accessToken = GenerateJWTToken.CreateToken(authClaims, _configuration, DateTime.UtcNow);
            return new JwtSecurityTokenHandler().WriteToken(accessToken);
        }

        private string GenerateRefreshToken(string email)
        {
            var claims = new List<Claim>();

            claims.Add(new Claim("email", email.ToString()));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

            var refreshToken = GenerateJWTToken.CreateRefreshToken(claims, _configuration, DateTime.UtcNow);
            return new JwtSecurityTokenHandler().WriteToken(refreshToken).ToString();
        }
    }
}
