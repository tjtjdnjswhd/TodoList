using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;

using System.Security.Claims;
using System.Text;

using TodoList.Shared.Data.Dtos;
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
        private static readonly int REFRESH_TOKEN_EXPIRATION_DAYS = 30;
        private static DateTimeOffset AccessTokenExpiration => DateTimeOffset.Now.AddMinutes(30);

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
        public async Task<IActionResult> RefreshAsync()
        {
            AuthorizeToken cookieToken = GetCookieToken();

            string? expiredAccessToken = await _cache.GetStringAsync(cookieToken.RefreshToken);
            string errorMessage;

            if (expiredAccessToken != cookieToken.AccessToken)
            {
                errorMessage = "Access token is not match";
            }
            else
            {
                User? user = await _jwtService.GetUserFromAccessTokenOrNullAsync(cookieToken.AccessToken);
                if (user == null)
                {
                    errorMessage = "Wrong access token";
                }
                else
                {
                    AuthorizeToken token = _jwtService.GenerateToken(user!, AccessTokenExpiration);
                    await SetCookieTokenAsync(token);

                    return Ok(new Response()
                    {
                        IsSuccess = true
                    });
                }
            }

            return BadRequest(new Response()
            {
                Message = errorMessage
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
                AuthorizeToken token = _jwtService.GenerateToken(user!, AccessTokenExpiration);
                await SetCookieTokenAsync(token);

                return Ok(new Response()
                {
                    IsSuccess = true
                });
            }

            return BadRequest(new Response()
            {
                IsSuccess = false,
                Message = errorMessage
            });
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetClaims()
        {
            AuthorizeToken cookieToken = GetCookieToken();
            IEnumerable<Claim>? claims = _jwtService.GetClaimsByTokenOrNull(cookieToken.AccessToken);
            if (claims == null)
            {
                return BadRequest(new Response()
                {
                    IsSuccess = false,
                    Message = "Wrong access token"
                });
            }

            IEnumerable<ClaimDto> claimDtos = _mapper.Map<IEnumerable<Claim>, IEnumerable<ClaimDto>>(claims);

            return Ok(new Response<IEnumerable<ClaimDto>>()
            {
                IsSuccess = true,
                Data = claimDtos
            });
        }

        [HttpPost]
        public async Task<IActionResult> SignupAsync([FromBody] SignupInfo signupInfo)
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
        [Authorize]
        public async Task<IActionResult> ExpireRefreshTokenAsync()
        {
            AuthorizeToken cookieToken = GetCookieToken();

            string token = await _cache.GetStringAsync(cookieToken.RefreshToken);
            if (token == cookieToken.AccessToken)
            {
                await _cache.RemoveAsync(cookieToken.RefreshToken);
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

        private AuthorizeToken GetCookieToken()
        {
            string accessToken = Request.Cookies["accessToken"] ?? string.Empty;
            string refreshToken = Request.Cookies["refreshToken"] ?? string.Empty;
            return new AuthorizeToken(accessToken, refreshToken);
        }

        private async Task SetCookieTokenAsync(AuthorizeToken token)
        {
            TimeSpan expirationDays = TimeSpan.FromDays(REFRESH_TOKEN_EXPIRATION_DAYS);

            await _cache.SetStringAsync(token.RefreshToken, token.AccessToken, new DistributedCacheEntryOptions()
            {
                SlidingExpiration = expirationDays
            });

            CookieOptions cookieOptions = new()
            {
                HttpOnly = true,
                Secure = true,
                MaxAge = expirationDays
            };

            Response.Cookies.Append("accessToken", token.AccessToken, cookieOptions);
            Response.Cookies.Append("refreshToken", token.RefreshToken, cookieOptions);
            Response.Cookies.Append("expiration", "a", new CookieOptions()
            {
                Secure = true,
                MaxAge = expirationDays,
            });
        }
    }
}
