using AutoMapper;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

using System.Text;

using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;
using TodoList.Shared.Svcs.Interfaces;

namespace TodoList.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class IdentityController : ControllerBase
    {
        private static readonly int VERIFY_CODE_LENGTH = 16;

        private static DateTimeOffset TokenExpiration => DateTimeOffset.Now.AddMinutes(30);

        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IVerifyCodeService _verifyCodeService;
        private readonly IDistributedCache _cache;
        private readonly IMapper _mapper;
        private readonly ILogger<IdentityController> _logger;

        public IdentityController(IUserService userService, IJwtService jwtService, IEmailService emailService, IVerifyCodeService verifyCodeService, IDistributedCache cache, IMapper mapper, ILogger<IdentityController> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _emailService = emailService;
            _verifyCodeService = verifyCodeService;
            _cache = cache;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> RefreshAsync([FromBody] AuthorizeToken authorizeToken)
        {
            string? expiredAccessToken = await _cache.GetStringAsync(authorizeToken.RefreshToken);
            string errorMessage;
            if (expiredAccessToken != authorizeToken.AccessToken)
            {
                errorMessage = "Access token is not match";
            }
            else
            {
                User? user = await _jwtService.GetUserFromAccessTokenOrNullAsync(authorizeToken.AccessToken);
                if (user == null)
                {
                    errorMessage = "Wrong access token";
                }
                else
                {
                    AuthorizeToken token = _jwtService.GenerateToken(user!, TokenExpiration);
                    await _cache.SetStringAsync(token.RefreshToken, token.AccessToken);

                    return Ok(new Response<AuthorizeToken>()
                    {
                        Data = token,
                        IsSuccess = true
                    });
                }
            }

            return BadRequest(new Response()
            {
                Message = errorMessage
            });
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            string accessToken = Request.Headers.Authorization.First(a => a.StartsWith(JwtBearerDefaults.AuthenticationScheme)).Remove(0, JwtBearerDefaults.AuthenticationScheme.Length + 1);
            User? user = await _jwtService.GetUserFromAccessTokenOrNullAsync(accessToken);
            if (user == null)
            {
                return BadRequest(new Response()
                {
                    IsSuccess = false,
                    Message = "Wrong access token"
                });
            }

            UserInfo userInfo = _mapper.Map<User, UserInfo>(user);

            return Ok(new Response<UserInfo>()
            {
                Data = userInfo,
                IsSuccess = true
            });
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsync([FromBody] LoginInfo loginInfo)
        {
            string errorMessage;
            if (!await _userService.IsEmailExistAsync(loginInfo.Email))
            {
                errorMessage = "Not exist email";
            }
            else if (!await _userService.MatchPassword(loginInfo))
            {
                errorMessage = "Wrong password";
            }
            else
            {
                User? user = await _userService.GetUserByEmailOrNullAsync(loginInfo.Email);
                AuthorizeToken token = _jwtService.GenerateToken(user!, TokenExpiration);
                await _cache.SetStringAsync(token.RefreshToken, token.AccessToken);

                return Ok(new Response<AuthorizeToken>()
                {
                    Data = token,
                    IsSuccess = true
                });
            }

            return BadRequest(new Response()
            {
                Message = errorMessage
            });
        }

        [HttpPost]
        public async Task<IActionResult> SignupAsync(SignupInfo signupInfo)
        {
            string errorMessage;
            if (await _userService.IsEmailExistAsync(signupInfo.Email))
            {
                errorMessage = "Duplicate email";
            }
            else if (await _userService.IsNameExistAsync(signupInfo.Name))
            {
                errorMessage = "Duplicate name";
            }
            else
            {
                Guid? id = await _userService.SignupAsync(signupInfo);
                if (id == null)
                {
                    return BadRequest();
                }

                Response<Guid> response = new()
                {
                    Data = id.Value,
                    IsSuccess = true
                };

                return Accepted(response);
            }

            return BadRequest(new Response()
            {
                Message = errorMessage
            });
        }

        [HttpPost]
        public async Task<IActionResult> ExpireRefreshTokenAsync([FromBody] AuthorizeToken authorizeToken)
        {
            string token = await _cache.GetStringAsync(authorizeToken.RefreshToken);
            if (token == authorizeToken.AccessToken)
            {
                await _cache.RemoveAsync(authorizeToken.RefreshToken);
                return Ok(new Response()
                {
                    IsSuccess = true,
                    Message = "Refresh token expired"
                });
            }

            return BadRequest(new Response()
            {
                Message = "Access token is not match"
            });
        }

        [HttpPost]
        public async Task<IActionResult> SendVerifyMail([FromForm] string email)
        {
            string code = _verifyCodeService.GetVerifyCode(VERIFY_CODE_LENGTH);
            await _verifyCodeService.SetVerifyCodeAsync(email, code);

            string verifyUrl = Url.ActionLink("VerifyCode", "Identity")!;
            string body =
                $@"<p>
                     <form action=""{verifyUrl}"" method=""post"">
                       <input type=""hidden"" value=""{email}"" name=""email""/>
                       <button value=""{code}"" name=""code"" type=""submit"">인증하려면 클릭하세요</button>
                     </form>
                   </p>";

            MailRequest mailRequest = new()
            {
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
                Subject = "인증",
                To = email
            };

            await _emailService.SendEmailAsync(mailRequest);
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> VerifyCodeAsync([FromForm] string email, [FromForm] string code)
        {
            bool isVerified = _verifyCodeService.IsVerifyCodeMatch(email, code);
            if (isVerified)
            {
                await _userService.VerifyEmailAsync(email);
                return Ok();
            }
            else
            {
                return BadRequest(new Response()
                {
                    Message = "Fail to verify email"
                });
            }
        }
    }
}
