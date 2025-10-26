using SEOBoostAI.Repository.ModelExtensions;
using SEOBoostAI.Repository.UnitOfWork;
using SEOBoostAI.Service.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEOBoostAI.Service.Services
{
    public class AuthenService : IAuthenService
    {
        private readonly IUserService _userService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthenService(IUserService userService, IUnitOfWork unitOfWork)
        {
            _userService = userService;
            _unitOfWork = unitOfWork;
        }
        public Task<ResultModel> LoginWithGoogle(string credential)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogOut(string refreshToken, int userId)
        {
            throw new NotImplementedException();
        }
    }
}
