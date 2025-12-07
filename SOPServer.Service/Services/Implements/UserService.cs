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
using SOPServer.Service.BusinessModels.OnboardingModels;
using SOPServer.Service.BusinessModels.ResultModels;
using SOPServer.Service.BusinessModels.UserModels;
using SOPServer.Service.BusinessModels.FirebaseModels;
using SOPServer.Service.Constants;
using SOPServer.Service.Exceptions;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Service.Utils;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

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
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IMinioService _minioService;

        public UserService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IConfiguration configuration,
            IMailService mailService,
            IOtpService otpService,
            IRedisService redisService,
            IEmailTemplateService emailTemplateService,
            IMinioService minioService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _configuration = configuration;
            _mailService = mailService;
            _otpService = otpService;
            _redisService = redisService;
            _emailTemplateService = emailTemplateService;
            _minioService = minioService;
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

                if (!existUser.IsLoginWithGoogle)
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
                await _unitOfWork.SaveAsync();

                // send welcome email without awaiting (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    var welcomeEmailBody = await _emailTemplateService.GenerateWelcomeEmailAsync(new WelcomeEmailTemplateModel
                    {
                        DisplayName = newUser.DisplayName ?? newUser.Email
                    }).ConfigureAwait(false);

                    await _mailService.SendEmailAsync(new MailRequest
                    {
                        ToEmail = newUser.Email,
                        Subject = MessageConstants.WELCOME_EMAIL_SUBJECT,
                        Body = welcomeEmailBody
                    }).ConfigureAwait(false);
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
                include: query => query
                    .Include(u => u.Job)
                    .Include(u => u.UserStyles.Where(us => !us.IsDeleted))
                        .ThenInclude(us => us.Style),
                orderBy: query => query.OrderByDescending(x => x.CreatedDate)
            );

            var userList = _mapper.Map<Pagination<UserListModel>>(users);

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

        public async Task<BaseResponseModel> SoftDeleteUserAsync(long userId)
        {
            var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(userId);

            if (existingUser == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (existingUser.IsDeleted)
            {
                throw new BadRequestException(MessageConstants.USER_ALREADY_DELETED);
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

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (user.IsLoginWithGoogle)
            {
                throw new BadRequestException(MessageConstants.USER_MUST_LOGIN_WITH_GOOGLE);
            }

            if (!PasswordUtils.VerifyPassword(model.Password, user.PasswordHash))
            {
                throw new UnauthorizedException(MessageConstants.EMAIL_OR_PASSWORD_INCORRECT);
            }

            if (user.IsDeleted)
            {
                throw new ForbiddenException(MessageConstants.USER_FORBIDDEN);
            }

            if (!user.IsVerifiedEmail)
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

            _ = Task.Run(async () =>
            {
                var welcomeEmailBody = await _emailTemplateService.GenerateWelcomeEmailAsync(new WelcomeEmailTemplateModel
                {
                    DisplayName = existingUser.DisplayName ?? existingUser.Email
                }).ConfigureAwait(false);

                await _mailService.SendEmailAsync(new MailRequest
                {
                    ToEmail = existingUser.Email,
                    Subject = MessageConstants.WELCOME_EMAIL_SUBJECT,
                    Body = welcomeEmailBody
                }).ConfigureAwait(false);
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

            var tokenValidityInMinutes = long.Parse(_configuration["JWT:TokenValidityInMinutes"]);
            var refreshTokenValidityInDays = long.Parse(_configuration["JWT:RefreshTokenValidityInDays"]);

            await _redisService.SetAsync(accessKey, accessToken, TimeSpan.FromMinutes(tokenValidityInMinutes));
            await _redisService.SetAsync(refreshKey, refreshToken, TimeSpan.FromDays(refreshTokenValidityInDays));

            return new AuthenResultModel
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<BaseResponseModel> SubmitOnboardingAsync(long userId, OnboardingRequestModel requestModel)
        {
            var existingUser = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (existingUser == null)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = MessageConstants.USER_NOT_EXIST
                };
            }

            if (existingUser.IsFirstTime == false)
            {
                return new BaseResponseModel
                {
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = MessageConstants.ONBOARDING_ALREADY_COMPLETED_MSG,
                };
            }

            _mapper.Map(requestModel, existingUser);
            existingUser.IsFirstTime = false;

            // Handle Job - prioritize OtherJob over JobId
            if (!string.IsNullOrWhiteSpace(requestModel.OtherJob))
            {
                // If OtherJob is provided, create new job and use it
                var newJob = new Job
                {
                    Name = requestModel.OtherJob,
                    Description = "User-defined job",
                    CreatedBy = CreatedBy.USER
                };
                await _unitOfWork.JobRepository.AddAsync(newJob);
                await _unitOfWork.SaveAsync();
                existingUser.JobId = newJob.Id;
            }
            // else JobId from requestModel is already mapped via AutoMapper

            // Handle Styles
            existingUser.UserStyles.Clear();

            // Add styles from StyleIds
            if (requestModel.StyleIds != null && requestModel.StyleIds.Any())
            {
                foreach (var styleId in requestModel.StyleIds)
                {
                    existingUser.UserStyles.Add(new UserStyle
                    {
                        UserId = userId,
                        StyleId = styleId
                    });
                }
            }

            // Handle OtherStyles - create new styles and add to UserStyles
            if (requestModel.OtherStyles != null && requestModel.OtherStyles.Any())
            {
                foreach (var otherStyleName in requestModel.OtherStyles)
                {
                    if (!string.IsNullOrWhiteSpace(otherStyleName))
                    {
                        var newStyle = new Style
                        {
                            Name = otherStyleName,
                            Description = "User-defined style",
                            CreatedBy = CreatedBy.USER
                        };
                        await _unitOfWork.StyleRepository.AddAsync(newStyle);
                        await _unitOfWork.SaveAsync();

                        existingUser.UserStyles.Add(new UserStyle
                        {
                            UserId = userId,
                            StyleId = newStyle.Id
                        });
                    }
                }
            }

            _unitOfWork.UserRepository.UpdateAsync(existingUser);
            await _unitOfWork.SaveAsync();
            var userProfile = _mapper.Map<UserProfileModel>(existingUser);


            return new BaseResponseModel
            {
                StatusCode = 200,
                Message = MessageConstants.ONBOARDING_SUCCESS,
                Data = userProfile,

            };
        }

        public async Task<BaseResponseModel> InitiatePasswordResetAsync(string email)
        {
            //rate limit
            var attemptKey = RedisKeyConstants.GetResetPasswordAttemptKey(email);
            var attempts = await _redisService.IncrementAsync(
                attemptKey,
                TimeSpan.FromMinutes(15)
            );

            if (attempts > 5)
            {
                throw new BadRequestException(MessageConstants.OTP_TOO_MANY_ATTEMPTS);
            }

            var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(email);

            //check user
            if (user != null && !user.IsDeleted)
            {
                if (user.IsLoginWithGoogle)
                {
                    return new BaseResponseModel
                    {
                        StatusCode = StatusCodes.Status200OK,
                        Message = MessageConstants.RESET_PASSWORD_REQUEST_SENT
                    };
                }

                //otp
                await _otpService.SendOtpAsync(email, user.DisplayName);
            }

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.RESET_PASSWORD_REQUEST_SENT
            };
        }

        public async Task<BaseResponseModel> VerifyResetOtpAsync(VerifyResetOtpRequestModel model)
        {
            //verify otp
            var otpKey = RedisKeyConstants.GetOtpKey(model.Email);
            var storedOtp = await _redisService.GetAsync<string>(otpKey);

            if (string.IsNullOrEmpty(storedOtp))
            {
                throw new BadRequestException(MessageConstants.OTP_INVALID);
            }

            if (storedOtp != model.Otp)
            {
                throw new BadRequestException(MessageConstants.OTP_INVALID);
            }

            // check user
            var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(model.Email);
            if (user == null || user.IsDeleted)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (user.IsLoginWithGoogle)
            {
                throw new BadRequestException(MessageConstants.USER_MUST_USE_GOOGLE_LOGIN);
            }

            // tokennnnnn
            var resetToken = Guid.NewGuid().ToString("N");
            var resetTokenKey = RedisKeyConstants.GetResetTokenKey(model.Email);

            //save to redis
            await _redisService.SetAsync(
                resetTokenKey,
                resetToken,
                TimeSpan.FromMinutes(15)
            );

            //delete otp
            await _redisService.RemoveAsync(otpKey);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.RESET_PASSWORD_OTP_VERIFIED,
                Data = new
                {
                    ResetToken = resetToken,
                    ExpiryMinutes = 15
                }
            };
        }

        public async Task<BaseResponseModel> ResetPasswordAsync(ResetPasswordRequestModel model)
        {
            // password = password ??
            if (model.NewPassword != model.ConfirmPassword)
            {
                throw new BadRequestException(MessageConstants.PASSWORD_DOES_NOT_MATCH);
            }

            //verify token
            var resetTokenKey = RedisKeyConstants.GetResetTokenKey(model.Email);
            var storedResetToken = await _redisService.GetAsync<string>(resetTokenKey);

            if (string.IsNullOrEmpty(storedResetToken))
            {
                throw new BadRequestException(MessageConstants.RESET_TOKEN_INVALID);
            }

            if (storedResetToken != model.ResetToken)
            {
                throw new BadRequestException(MessageConstants.RESET_TOKEN_INVALID);
            }

            // preventing reuse token
            var usedTokenKey = RedisKeyConstants.GetUsedResetTokenKey(model.ResetToken);
            var isUsed = await _redisService.ExistsAsync(usedTokenKey);
            if (isUsed)
            {
                throw new BadRequestException(MessageConstants.RESET_TOKEN_ALREADY_USED);
            }

            //get that person !!!
            var user = await _unitOfWork.UserRepository.GetUserByEmailAsync(model.Email);
            if (user == null || user.IsDeleted)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (user.IsLoginWithGoogle)
            {
                throw new BadRequestException(MessageConstants.USER_MUST_USE_GOOGLE_LOGIN);
            }

            // update
            var newPasswordHash = PasswordUtils.HashPassword(model.NewPassword);
            user.PasswordHash = newPasswordHash;
            _unitOfWork.UserRepository.UpdateAsync(user);
            await _unitOfWork.SaveAsync();

            //mark used 
            await _redisService.SetAsync(
                usedTokenKey,
                true,
                TimeSpan.FromHours(24)
            );

            await _redisService.RemoveAsync(resetTokenKey);

            var resetTime = DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm 'UTC'");
            var emailBody = await _emailTemplateService.GeneratePasswordResetSuccessEmailAsync(new PasswordResetSuccessEmailTemplateModel
            {
                DisplayName = user.DisplayName ?? user.Email,
                Email = user.Email,
                ResetTime = resetTime
            });

            await _mailService.SendEmailAsync(new MailRequest
            {
                ToEmail = user.Email,
                Subject = MessageConstants.PASSWORD_RESET_SUBJECT_MAIL,
                Body = emailBody
            });

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.RESET_PASSWORD_SUCCESS
            };
        }

        public async Task<BaseResponseModel> GetUserProfileByIdAsync(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetUserProfileByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var userProfile = _mapper.Map<UserProfileModel>(user);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_USER_PROFILE_SUCCESS,
                Data = userProfile
            };
        }

        public async Task<BaseResponseModel> GetUserByIdAsync(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetUserProfileByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            var userPublic = _mapper.Map<UserPublicModel>(user);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_USER_BY_ID_SUCCESS,
                Data = userPublic
            };
        }

        public async Task<UserCharacteristicModel> GetUserCharacteristic(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdIncludeAsync(
                userId,
                include: x => x.Include(u => u.UserStyles.Where(us => !us.IsDeleted))
                    .ThenInclude(y => y.Style)
            );

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            return _mapper.Map<UserCharacteristicModel>(user);
        }

        public async Task<BaseResponseModel> UpdateProfile(long userId, UpdateProfileModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdIncludeAsync(
                userId,
                include: query => query
                    .Include(u => u.Job)
                    .Include(u => u.UserStyles.Where(us => !us.IsDeleted))
                        .ThenInclude(us => us.Style)
            );

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Update basic fields
            if (!string.IsNullOrWhiteSpace(model.DisplayName))
            {
                user.DisplayName = model.DisplayName;
            }

            if (model.Dob.HasValue)
            {
                user.Dob = model.Dob.Value;
            }

            if (model.Gender.HasValue)
            {
                user.Gender = model.Gender.Value;
            }

            if (model.PreferedColor != null)
            {
                user.PreferedColor = model.PreferedColor;
            }

            if (model.AvoidedColor != null)
            {
                user.AvoidedColor = model.AvoidedColor;
            }

            if (model.Location != null)
            {
                user.Location = model.Location;
            }

            if (model.Bio != null)
            {
                user.Bio = model.Bio;
            }

            // Update avatar URL if provided
            if (model.AvtUrl != null)
            {
                user.AvtUrl = model.AvtUrl;
            }

            // Handle Job - prioritize OtherJob over JobId
            if (!string.IsNullOrWhiteSpace(model.OtherJob))
            {
                // If OtherJob is provided, create new job and use it
                var newJob = new Job
                {
                    Name = model.OtherJob,
                    Description = "User-defined job",
                    CreatedBy = CreatedBy.USER
                };
                await _unitOfWork.JobRepository.AddAsync(newJob);
                await _unitOfWork.SaveAsync();
                user.JobId = newJob.Id;
            }
            else if (model.JobId.HasValue)
            {
                var job = await _unitOfWork.JobRepository.GetByIdAsync(model.JobId.Value);
                if (job == null)
                {
                    throw new NotFoundException(MessageConstants.JOB_NOT_EXIST);
                }
                user.JobId = model.JobId.Value;
            }

            // Update styles if StyleIds or OtherStyles are provided
            if ((model.StyleIds != null && model.StyleIds.Any()) || (model.OtherStyles != null && model.OtherStyles.Any()))
            {
                // Soft delete existing active styles
                var existingUserStyles = user.UserStyles.Where(us => !us.IsDeleted).ToList();
                foreach (var userStyle in existingUserStyles)
                {
                    userStyle.IsDeleted = true;
                }

                // Add styles from StyleIds
                if (model.StyleIds != null && model.StyleIds.Any())
                {
                    foreach (var styleId in model.StyleIds)
                    {
                        var style = await _unitOfWork.StyleRepository.GetByIdAsync(styleId);
                        if (style == null)
                        {
                            throw new NotFoundException($"{MessageConstants.STYLE_NOT_EXIST}: {styleId}");
                        }

                        user.UserStyles.Add(new UserStyle
                        {
                            UserId = userId,
                            StyleId = styleId
                        });
                    }
                }

                // Handle OtherStyles - create new styles and add to UserStyles
                if (model.OtherStyles != null && model.OtherStyles.Any())
                {
                    foreach (var otherStyleName in model.OtherStyles)
                    {
                        if (!string.IsNullOrWhiteSpace(otherStyleName))
                        {
                            var newStyle = new Style
                            {
                                Name = otherStyleName,
                                Description = "User-defined style",
                                CreatedBy = CreatedBy.USER
                            };
                            await _unitOfWork.StyleRepository.AddAsync(newStyle);
                            await _unitOfWork.SaveAsync();

                            user.UserStyles.Add(new UserStyle
                            {
                                UserId = userId,
                                StyleId = newStyle.Id
                            });
                        }
                    }
                }
            }

            _unitOfWork.UserRepository.UpdateAsync(user);
            await _unitOfWork.SaveAsync();

            // Reload user with updated data
            var updatedUser = await _unitOfWork.UserRepository.GetUserProfileByIdAsync(userId);
            var userProfile = _mapper.Map<UserProfileModel>(updatedUser);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_PROFILE_UPDATE_SUCCESS,
                Data = userProfile
            };
        }

        public async Task<BaseResponseModel> UpdateAvatar(long userId, IFormFile file)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            // Delete old avatar if exists
            if (!string.IsNullOrWhiteSpace(user.AvtUrl))
            {
                try
                {
                    await _minioService.DeleteImageByURLAsync(user.AvtUrl);
                }
                catch
                {
                    // Continue even if old avatar deletion fails
                }
            }

            // Upload new avatar to MinIO
            var uploadResult = await _minioService.UploadImageAsync(file);

            if (uploadResult?.Data is ImageUploadResult imageResult)
            {
                user.AvtUrl = imageResult.DownloadUrl;
            }
            else
            {
                throw new BadRequestException("Failed to upload avatar");
            }

            _unitOfWork.UserRepository.UpdateAsync(user);
            await _unitOfWork.SaveAsync();

            // Return updated profile
            var updatedUser = await _unitOfWork.UserRepository.GetUserProfileByIdAsync(userId);
            var userProfile = _mapper.Map<UserProfileModel>(updatedUser);

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.USER_AVATAR_UPDATE_SUCCESS,
                Data = userProfile
            };
        }

        public async Task<BaseResponseModel> GetStylistProfileByUserIdAsync(long userId, long? currentUserId = null)
        {
            // Get user and verify is STYLIST
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException(MessageConstants.STYLIST_PROFILE_NOT_FOUND);
            }

            // Check if user is a stylist
            if (user.Role != Role.STYLIST)
            {
                throw new NotFoundException(MessageConstants.STYLIST_PROFILE_NOT_FOUND);
            }

            // Get published collections for this stylist
            var collections = await _unitOfWork.CollectionRepository.GetQueryable()
                .Include(c => c.LikeCollections.Where(lc => !lc.IsDeleted))
                .Include(c => c.SaveCollections.Where(sc => !sc.IsDeleted))
                .Where(c => c.UserId == userId && c.IsPublished && !c.IsDeleted)
                .ToListAsync();

            // Calculate statistics
            var publishedCollectionsCount = collections.Count;
            var totalLikes = collections.Sum(c => c.LikeCollections.Count);
            var totalSaves = collections.Sum(c => c.SaveCollections.Count);

            // Check if current user is following this stylist
            var isFollowed = false;
            if (currentUserId.HasValue && currentUserId.Value != 0)
            {
                isFollowed = await _unitOfWork.FollowerRepository.IsFollowing(currentUserId.Value, userId);
            }

            // Build response model
            var stylistProfile = new StylistProfileModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email,
                AvatarUrl = user.AvtUrl,
                Location = user.Location,
                Bio = user.Bio,
                Dob = user.Dob,
                JobId = user.JobId,
                JobName = user.Job?.Name,
                PublishedCollectionsCount = publishedCollectionsCount,
                TotalCollectionsLikes = totalLikes,
                TotalCollectionsSaves = totalSaves,
                IsFollowed = isFollowed
            };

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.GET_STYLIST_PROFILE_SUCCESS,
                Data = stylistProfile
            };
        }

        public async Task<BaseResponseModel> ChangePasswordAsync(long userId, ChangePasswordModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (user.IsLoginWithGoogle)
            {
                throw new BadRequestException(MessageConstants.CHANGE_PASSWORD_NOT_ALLOWED_FOR_GOOGLE);
            }

            // Verify current password
            if (!PasswordUtils.VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                throw new BadRequestException(MessageConstants.CURRENT_PASSWORD_INCORRECT);
            }

            // Validate new password matches confirm password
            if (model.NewPassword != model.ConfirmPassword)
            {
                throw new BadRequestException(MessageConstants.PASSWORD_DOES_NOT_MATCH);
            }

            // Update password
            var newPasswordHash = PasswordUtils.HashPassword(model.NewPassword);
            user.PasswordHash = newPasswordHash;
            _unitOfWork.UserRepository.UpdateAsync(user);
            await _unitOfWork.SaveAsync();

            // Send confirmation email
            var changeTime = DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm 'UTC'");
            var emailBody = await _emailTemplateService.GeneratePasswordChangedEmailAsync(new PasswordChangedEmailTemplateModel
            {
                DisplayName = user.DisplayName ?? user.Email,
                Email = user.Email,
                ChangedTime = changeTime
            });

            await _mailService.SendEmailAsync(new MailRequest
            {
                ToEmail = user.Email,
                Subject = MessageConstants.PASSWORD_CHANGE_SUBJECT_MAIL,
                Body = emailBody
            });

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.CHANGE_PASSWORD_SUCCESS
            };
        }

        public async Task<BaseResponseModel> InitiateChangePasswordWithOtpAsync(long userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (user.IsLoginWithGoogle)
            {
                throw new BadRequestException(MessageConstants.CHANGE_PASSWORD_NOT_ALLOWED_FOR_GOOGLE);
            }

            // Rate limiting
            var attemptKey = RedisKeyConstants.GetChangePasswordAttemptKey(userId);
            var attempts = await _redisService.IncrementAsync(attemptKey, TimeSpan.FromMinutes(15));

            if (attempts > 5)
            {
                throw new BadRequestException(MessageConstants.OTP_TOO_MANY_ATTEMPTS);
            }

            // Generate OTP
            var otp = GenerateOtp();
            var otpKey = RedisKeyConstants.GetOtpKey(user.Email);
            
            await _redisService.SetAsync(
                otpKey,
                otp,
                TimeSpan.FromMinutes(5)
            );

            // Send OTP email with password change context
            var emailBody = await _emailTemplateService.GenerateOtpPasswordChangeEmailAsync(new OtpPasswordChangeEmailTemplateModel
            {
                DisplayName = user.DisplayName ?? user.Email,
                Otp = otp,
                ExpiryMinutes = 5
            });

            await _mailService.SendEmailAsync(new MailRequest
            {
                ToEmail = user.Email,
                Subject = "SOP - Password Change Verification Code",
                Body = emailBody
            });

            // Store OTP reference for this user's change password request
            var changePasswordOtpKey = RedisKeyConstants.GetChangePasswordOtpKey(userId);
            await _redisService.SetAsync(changePasswordOtpKey, user.Email, TimeSpan.FromMinutes(5));

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.CHANGE_PASSWORD_OTP_SENT,
                Data = new
                {
                    Email = MaskEmail(user.Email),
                    ExpiryMinutes = 5
                }
            };
        }

        public async Task<BaseResponseModel> ChangePasswordWithOtpAsync(long userId, ChangePasswordWithOtpModel model)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);

            if (user == null)
            {
                throw new NotFoundException(MessageConstants.USER_NOT_EXIST);
            }

            if (user.IsLoginWithGoogle)
            {
                throw new BadRequestException(MessageConstants.CHANGE_PASSWORD_NOT_ALLOWED_FOR_GOOGLE);
            }

            // Verify OTP
            var otpKey = RedisKeyConstants.GetOtpKey(user.Email);
            var storedOtp = await _redisService.GetAsync<string>(otpKey);

            if (string.IsNullOrEmpty(storedOtp) || storedOtp != model.Otp)
            {
                throw new BadRequestException(MessageConstants.CHANGE_PASSWORD_OTP_INVALID);
            }

            // Validate passwords match
            if (model.NewPassword != model.ConfirmPassword)
            {
                throw new BadRequestException(MessageConstants.PASSWORD_DOES_NOT_MATCH);
            }

            // Update password
            var newPasswordHash = PasswordUtils.HashPassword(model.NewPassword);
            user.PasswordHash = newPasswordHash;
            _unitOfWork.UserRepository.UpdateAsync(user);
            await _unitOfWork.SaveAsync();

            // Clean up OTP and change password request
            await _redisService.RemoveAsync(otpKey);
            await _redisService.RemoveAsync(RedisKeyConstants.GetChangePasswordOtpKey(userId));

            // Send confirmation email
            var changeTime = DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm 'UTC'");
            var emailBody = await _emailTemplateService.GeneratePasswordChangedEmailAsync(new PasswordChangedEmailTemplateModel
            {
                DisplayName = user.DisplayName ?? user.Email,
                Email = user.Email,
                ChangedTime = changeTime
            });

            await _mailService.SendEmailAsync(new MailRequest
            {
                ToEmail = user.Email,
                Subject = MessageConstants.PASSWORD_CHANGE_SUBJECT_MAIL,
                Body = emailBody
            });

            return new BaseResponseModel
            {
                StatusCode = StatusCodes.Status200OK,
                Message = MessageConstants.CHANGE_PASSWORD_SUCCESS
            };
        }

        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return email;

            var parts = email.Split('@');
            var localPart = parts[0];
            var domain = parts[1];

            if (localPart.Length <= 2)
                return $"{localPart[0]}***@{domain}";

            return $"{localPart.Substring(0, 2)}***@{domain}";
        }

        private string GenerateOtp()
        {
            var randomNumber = new byte[4];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }
            var number = BitConverter.ToUInt32(randomNumber, 0);
            return (number % 1000000).ToString("D6");
        }
    }
}
