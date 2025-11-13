using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
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
        private readonly IWalletRepository _walletRepository;
        private readonly IConfiguration _configuration;
        private readonly IUserMonthlyFreeQuotaService _userMonthlyFreeQuotaService;
        private readonly IFeatureRepository _featureRepository;

        public AuthenService(IUserRepository userRepository, IUnitOfWork unitOfWork, IWalletRepository walletRepository, IConfiguration configuration, 
            IUserMonthlyFreeQuotaService userMonthlyFreeQuotaService, IFeatureRepository featureRepository)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
            _walletRepository = walletRepository;
            _configuration = configuration;
            _userMonthlyFreeQuotaService = userMonthlyFreeQuotaService;
            _featureRepository = featureRepository;
        }
        public async Task<ResultModel> LoginWithGoogle(string credential)
        {
            // Validate configuration
            string clientId = _configuration["GoogleCredential:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                throw new InvalidOperationException("Google client ID is not configured.");
            }

            // Validate Google credential
            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string> { clientId }
            };
            var payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);
            if (payload == null || string.IsNullOrWhiteSpace(payload.Email))
            {
                throw new Exception("Credential không hợp lệ"); // keep original language if needed
            }

            string googleId = payload.Subject ?? payload.JwtId;

            var existUser = await _userRepository.GetUserByEmailAsync(payload.Email);

            if (existUser != null)
            {
                var accessToken = await GenerateAccessToken(existUser);
                var refreshToken = GenerateRefreshToken(existUser.Email);

                existUser.RefreshToken = refreshToken;

                await _userRepository.UpdateAsync(existUser);
                var saveResult = await _unitOfWork.SaveChangesAsync();
                if (saveResult <= 0)
                {
                    throw new Exception("Failed to update user refresh token.");
                }

                await _userMonthlyFreeQuotaService.UpdateMonthQuotaAsync(existUser.UserID);

                return new ResultModel()
                {
                    Success = true,
                    Message = "Login successfully",
                    AccessToken = accessToken,
                    RefreshToken = refreshToken
                };
            }
            else
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    User newUser = new User()
                    {
                        UserName = payload.Email.Split('@')[0].Trim(),
                        FullName = payload.Name,
                        Email = payload.Email,
                        Role = "User",
                        Avatar = payload.Picture,
                        Password = "".Trim(),
                        CreatedAt = DateTime.UtcNow,
                        GoogleID = googleId
                    };

                    // Create user (first save to get the generated UserID)
                    await _userRepository.CreateAsync(newUser);
                    var createResult = await _unitOfWork.SaveChangesAsync();
                    if (createResult <= 0)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw new Exception("Failed to create user.");
                    }

                    // Generate tokens now that UserID is assigned
                    var accessToken = await GenerateAccessToken(newUser);
                    var refreshToken = GenerateRefreshToken(newUser.Email);

                    // Store refresh token
                    newUser.RefreshToken = refreshToken;
                    await _userRepository.UpdateAsync(newUser);
                    var updateResult = await _unitOfWork.SaveChangesAsync();
                    if (updateResult <= 0)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw new Exception("Failed to store refresh token for new user.");
                    }

                    // Create wallet
                    Wallet wallet = new Wallet()
                    {
                        Currency = 0.0M,
                        CreatedAt = DateTime.UtcNow,
                        UserID = newUser.UserID
                    };

                    await _walletRepository.CreateAsync(wallet);
                    var walletResult = await _unitOfWork.SaveChangesAsync();
                    if (walletResult <= 0)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw new Exception("Failed to create user wallet.");
                    }

                    // Create user monthly free quota entries
                    var userMonthlyFreeQuotaResult = await _userMonthlyFreeQuotaService.CreateQuotaAsync(newUser.UserID);
                    if (userMonthlyFreeQuotaResult <= 0)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                        throw new Exception("Failed to create user account subscription.");
                    }

                    await _unitOfWork.CommitTransactionAsync();

                    return new ResultModel()
                    {
                        Success = true,
                        Message = "Login successfully",
                        AccessToken = accessToken,
                        RefreshToken = refreshToken
                    };
                }
                catch (DbUpdateException dbEx)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    // Likely unique constraint (username/email) -- surface clearer message
                    throw new Exception("Database update failed (possible unique constraint).", dbEx);
                }
                catch
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    throw;
                }
            }
        }

        public async Task<bool> LogOut(string refreshToken, int userId)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:SecretKey"]));
            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = authSigningKey,
                ValidateIssuer = true,
                ValidIssuer = _configuration["JWT:ValidIssuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["JWT:ValidAudience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
            try
            {
                SecurityToken validatedToken;
                var principal = handler.ValidateToken(refreshToken, validationParameters, out validatedToken);
                var email = principal.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value;
                if (email != null)
                {
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (email == user.Email)
                    {
                        var existUser = await _userRepository.GetUserByEmailAsync(email);

                        if (existUser != null)
                        {
                            existUser.RefreshToken = null;
                            await _userRepository.UpdateAsync(existUser);

                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                throw ex;
            }
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
