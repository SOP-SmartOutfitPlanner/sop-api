using AutoMapper;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SOPServer.Repository.Commons;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Enums;
using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.BusinessModels.AuthenModels;
using SOPServer.Service.BusinessModels.EmailModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
//using SOPServer.Service.BusinessModels.EmailModels;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.Utils;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Service.Services.Implements
{
    public class UserService : IUserService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        private readonly IMailService _mailService;
        private readonly IOtpService _otpService;
        private readonly IRedisService _redisService;

        public UserService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IConfiguration configuration,
            IMailService mailService,
            IOtpService otpService, IRedisService redisService) 
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _mailService = mailService;
            _otpService = otpService;
            _redisService = redisService;
        }

        public async Task<BaseResponseModel> GetUserById(int id)
        {
            var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = MessageConstants.USER_NOT_EXIST
                };
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_USER_BY_EMAIL_SUCCESS,
                Data = _mapper.Map<UserModel>(existingUser)
            };
        }

        public async Task<BaseResponseModel> LoginWithGoogleOAuth(string credential)
        {
            string clientId = _configuration["GoogleCredential:ClientId"];

            if (string.IsNullOrEmpty(clientId))
            {
                throw new BadRequestException(MessageConstants.TOKEN_NOT_VALID);
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings()
            {
                Audience = new List<string> { clientId }
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);
            }
            catch (Exception)
            {
                throw new BadRequestException(MessageConstants.TOKEN_NOT_VALID);
            }

            if (payload == null)
            {
                throw new BadRequestException(MessageConstants.TOKEN_NOT_VALID);
            }

            var existUser = await _unitOfWork.UserRepository.GetUserByEmailAsync(payload.Email);

            if (existUser != null)
            {
                if (existUser.IsDeleted)
                {
                    throw new ForbiddenException(MessageConstants.USER_FORBIDDEN);
                }

                if(!existUser.IsLoginWithGoogle)
                {
                    throw new BadRequestException(MessageConstants.USER_MUST_LOGIN_WITH_PASSWORD);
                }

                var authResult = await IssueAndCacheTokensAsync(existUser);


                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.LOGIN_SUCCESS_MESSAGE,
                    Data = authResult
                };
            }
            else
            {
                var newUser = new User
                {
                    Email = payload.Email,
                    DisplayName = payload.Name,
                    AvtUrl = payload.Picture,
                    Role = Role.USER,
                    IsVerifiedEmail = true,
                    IsFirstTime = true,
                    IsLoginWithGoogle = true
                };
                await _unitOfWork.UserRepository.AddAsync(newUser);
                _unitOfWork.Save();

                await _mailService.SendEmailAsync(new MailRequest
                {
                    ToEmail = newUser.Email,
                    Subject = MessageConstants.WELCOME_EMAIL_SUBJECT,
                    Body = EmailUtils.WelcomeEmail(newUser.DisplayName ?? newUser.Email)
                });

                var authResult = await IssueAndCacheTokensAsync(newUser);

                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status200OK,
                    Message = MessageConstants.LOGIN_SUCCESS_MESSAGE,
                    Data = authResult
                };
            }
        }

        public async Task<BaseResponseModel> RefreshToken(string jwtToken)
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
                var principal = handler.ValidateToken(jwtToken, validationParameters, out validatedToken);
                var email = principal.Claims.FirstOrDefault(x => x.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress").Value;
                if (email != null)
                {
                    var existUser = await _unitOfWork.UserRepository.GetUserByEmailAsync(email);
                    if (existUser != null)
                    {
                        var authResult = await IssueAndCacheTokensAsync(existUser);

                        return new BaseResponseModel
                        {
                            StatusCode = StatusCodes.Status200OK,
                            Message = MessageConstants.LOGIN_SUCCESS_MESSAGE,
                            Data = authResult
                        };
                    }
                }
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = MessageConstants.USER_NOT_EXIST
                };
            }
            catch
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status401Unauthorized,
                    Message = MessageConstants.TOKEN_NOT_VALID
                };
            }
        }

        public async Task<BaseResponseModel> UpdateUser(UpdateUserModel user)
        {
            var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(user.Id);
            if (existingUser == null)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = MessageConstants.USER_NOT_EXIST
                };
            }

            _mapper.Map(user, existingUser);

            _unitOfWork.UserRepository.UpdateAsync(existingUser);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_UPDATE_SUCCESS
            };

        }

        public async Task<BaseResponseModel> GetUsers(PaginationParameter paginationParameter)
        {
            var users = await _unitOfWork.UserRepository.ToPaginationIncludeAsync(
                paginationParameter,
                filter: x => !x.IsDeleted && x.Role != Role.ADMIN,
                //include: query => query.Include(x => x.Role),
                orderBy: query => query.OrderByDescending(x => x.CreatedDate)
            );

            var userList = _mapper.Map<Pagination<UserModel>>(users);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_USER_SUCCESS,
                Data = new ModelPaging
                {
                    Data = userList,
                    MetaData = new
                    {
                        userList.TotalCount,
                        userList.PageSize,
                        userList.CurrentPage,
                        userList.TotalPages,
                        userList.HasNext,
                        userList.HasPrevious
                    }
                }
            };
        }

        public async Task<BaseResponseModel> DeleteUser(int id)
        {
            var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(id);
            if (existingUser == null)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = MessageConstants.USER_NOT_EXIST
                };
            }

            _unitOfWork.UserRepository.SoftDeleteAsync(existingUser);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_DELETE_SUCCESS
            };
        }

        public async Task<BaseResponseModel> UpdateUserAddress(UpdateUserAddressModel userAddress)
        {
            var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(userAddress.Id);
            if (existingUser == null)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = MessageConstants.USER_NOT_EXIST
                };
            }

            // Update only the address fields
            //existingUser.Province = userAddress.Province;
            //existingUser.District = userAddress.District;
            //existingUser.Ward = userAddress.Ward;
            //existingUser.AddressBonus = userAddress.AddressBonus;

            _unitOfWork.UserRepository.UpdateAsync(existingUser);
            await _unitOfWork.SaveAsync();

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_ADDRESS_UPDATE_SUCCESS
            };
        }

        public async Task<BaseResponseModel> LoginWithEmailAndPassword(LoginRequestModel model)
        {
            var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(model.Email);

            if(user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (user.IsLoginWithGoogle)
            {
                throw new BadRequestException(MessageConstants.USER_MUST_LOGIN_WITH_GOOGLE);
            }

            if(!PasswordUtils.VerifyPassword(model.Password, user.PasswordHash))
            {
                throw new UnauthorizedException(MessageConstants.EMAIL_OR_PASSWORD_INCORRECT);
            }

            if (user.IsDeleted)
            {
                throw new ForbiddenException(MessageConstants.USER_FORBIDDEN);
            }

            if(!user.IsVerifiedEmail) 
            {
                throw new BadRequestException(MessageConstants.USER_NOT_VERIFY);
            }

            var authResult = await IssueAndCacheTokensAsync(user);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.LOGIN_SUCCESS_MESSAGE,
                Data = authResult
            };
        }

        public async Task<BaseResponseModel> RegisterUser(RegisterRequestModel model)
        {
            var existingUser = await _unitOfWork.UserRepository.GetUserByEmailAsync(model.Email);

            if (existingUser != null)
            {
                throw new BadRequestException(MessageConstants.EMAIL_EXISTED);
            }

            if (model.Password != model.ConfirmPassword)
            {
                throw new BadRequestException(MessageConstants.PASSWORD_DOES_NOT_MATCH);
            }

            var passwordHash = PasswordUtils.HashPassword(model.Password);

            var newUser = new User
            {
                Email = model.Email,
                DisplayName = model.DisplayName,
                PasswordHash = passwordHash,
                Role = Role.USER,
                IsVerifiedEmail = false,
                IsFirstTime = true,
                IsLoginWithGoogle = false
            };

            await _unitOfWork.UserRepository.AddAsync(newUser);
            await _unitOfWork.SaveAsync();

            await _otpService.SendOtpAsync(model.Email, model.DisplayName);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status201Created,
                Message = MessageConstants.REGISTER_SUCCESS_VI,
                Data = new
                {
                    Email = model.Email,
                    Message = MessageConstants.OTP_SENT_VI
                }
            };
        }

        public async Task<BaseResponseModel> ResendOtp(string email)
        {
            var existingUser = await _unitOfWork.UserRepository.GetUserByEmailAsync(email);

            if (existingUser == null)
            {
                throw new BadRequestException(MessageConstants.USER_NOT_EXIST);
            }
            else if (existingUser.IsVerifiedEmail == true)
            {
                throw new BadRequestException(MessageConstants.USER_ALREADY_VERIFY);
            }
            return await _otpService.SendOtpAsync(email, existingUser.DisplayName);
        }

        public async Task<BaseResponseModel> VerifyOtp(VerifyOtpRequestModel model)
        {
            var existingUser = await _unitOfWork.UserRepository.GetUserByEmailAsync(model.Email);
            if (existingUser == null) throw new BadRequestException(MessageConstants.USER_NOT_EXIST);
            if (existingUser.IsVerifiedEmail == true) throw new BadRequestException(MessageConstants.USER_ALREADY_VERIFY);

            var otpResult = await _otpService.VerifyOtpAsync(model.Email, model.Otp);

            existingUser.IsVerifiedEmail = true;
            _unitOfWork.UserRepository.UpdateAsync(existingUser);
            await _unitOfWork.SaveAsync();

            await _mailService.SendEmailAsync(new MailRequest
            {
                ToEmail = existingUser.Email,
                Subject = MessageConstants.WELCOME_EMAIL_SUBJECT,
                Body = EmailUtils.WelcomeEmail(existingUser.DisplayName ?? existingUser.Email)
            });

            return otpResult;
        }

        public async Task<BaseResponseModel> LogoutCurrentAsync(ClaimsPrincipal principal)
        {
            var userIdStr = principal.FindFirst("UserId")?.Value;
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || string.IsNullOrEmpty(jti))
                throw new BadRequestException(MessageConstants.INVALID_TOKEN_CLAIMS);

            var userId = long.Parse(userIdStr);

            await _redisService.RemoveAsync(RedisKeyConstants.GetAccessTokenKey(userId, jti));
            await _redisService.RemoveAsync(RedisKeyConstants.GetRefreshTokenKey(userId, jti));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status202Accepted,
                Message = MessageConstants.LOGGED_OUT
            };
        }



        private async Task<AuthenResultModel> IssueAndCacheTokensAsync(User user)
        {
            var tokenId = Guid.NewGuid().ToString("N");

            var accessToken = AuthenTokenUtils.GenerateAccessToken(user, user.Role, _configuration, tokenId);
            var refreshToken = AuthenTokenUtils.GenerateRefreshToken(user, _configuration, tokenId);

            var accessKey = RedisKeyConstants.GetAccessTokenKey(user.Id, tokenId);
            var refreshKey = RedisKeyConstants.GetRefreshTokenKey(user.Id, tokenId);

            await _redisService.SetAsync(accessKey, accessToken, TimeSpan.FromHours(1));
            await _redisService.SetAsync(refreshKey, refreshToken, TimeSpan.FromDays(7));

            return new AuthenResultModel
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }


    }
}
