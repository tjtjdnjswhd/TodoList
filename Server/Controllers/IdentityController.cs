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
    public sealed class IdentityController : ControllerBase
    {
        private static readonly int VERIFY_CODE_LENGTH = 16;
        private static readonly int REFRESH_TOKEN_EXPIRATION_DAYS = 30;
        private static readonly string ACCESS_TOKEN = "accessToken";
        private static readonly string REFRESH_TOKEN = "refreshToken";
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

        [HttpGet]
        public async Task<IActionResult> IsEmailExistAsync(string email)
        {
            return Ok(new Response<bool>()
            {
                Data = await _userService.IsEmailExistAsync(email),
                IsSuccess = true
            });
        }

        [HttpGet]
        public async Task<IActionResult> IsNameExistAsync(string name)
        {
            return Ok(new Response<bool>()
            {
                Data = await _userService.IsNameExistAsync(name),
                IsSuccess = true
            });
        }

        [HttpPost]
        public async Task<IActionResult> RefreshAsync()
        {
            AuthorizeToken? cookieToken = GetCookieTokenOrNull();
            if (cookieToken == null)
            {
                Response.Headers.Add("IS-REFRESH-TOKEN-EXPIRED", "true");
                return NotFound(new Response()
                {
                    ErrorCode = EErrorCode.RefreshTokenExpired,
                    IsSuccess = false
                });
            }

            string? expiredAccessToken = await _cache.GetStringAsync(cookieToken.RefreshToken);
            EErrorCode errorCode = EErrorCode.Default;

            if (expiredAccessToken != cookieToken.AccessToken)
            {
                errorCode |= EErrorCode.AccessTokenNotMatch;
            }
            else
            {
                User? user = await _jwtService.GetUserFromAccessTokenOrNullAsync(cookieToken.AccessToken);
                if (user == null)
                {
                    errorCode |= EErrorCode.WrongAccessToken;
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
                ErrorCode = errorCode,
                IsSuccess = false
            });
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsync([FromBody] LoginInfo loginInfo)
        {
            EErrorCode errorCode = EErrorCode.Default;
            if (!await _userService.IsEmailExistAsync(loginInfo.Email))
            {
                errorCode |= EErrorCode.EmailNotExist;
            }

            if (!await _userService.MatchPassword(loginInfo))
            {
                errorCode |= EErrorCode.EmailNotExist;
            }

            if (errorCode != EErrorCode.Default)
            {
                return NotFound(new Response()
                {
                    IsSuccess = false,
                    ErrorCode = errorCode
                });
            }

            User? user = await _userService.GetUserByEmailOrNullAsync(loginInfo.Email);
            AuthorizeToken token = _jwtService.GenerateToken(user!, AccessTokenExpiration);
            await SetCookieTokenAsync(token);

            return Ok(new Response()
            {
                IsSuccess = true
            });
        }

        [HttpGet]
        [Authorize]
        public IActionResult GetClaims()
        {
            AuthorizeToken cookieToken = GetCookieTokenOrNull()!;
            IEnumerable<Claim>? claims = _jwtService.GetClaimsByTokenOrNull(cookieToken.AccessToken);
            if (claims == null)
            {
                return BadRequest(new Response()
                {
                    IsSuccess = false,
                    ErrorCode = EErrorCode.WrongAccessToken
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
            EErrorCode errorCode = EErrorCode.Default;
            if (await _userService.IsEmailExistAsync(signupInfo.Email))
            {
                errorCode |= EErrorCode.EmailDuplicate;
            }

            if (await _userService.IsNameExistAsync(signupInfo.Name))
            {
                errorCode |= EErrorCode.NameDuplicate;
            }

            if (errorCode != EErrorCode.Default)
            {
                return BadRequest(new Response()
                {
                    ErrorCode = errorCode
                });
            }

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

        [HttpPost]
        public async Task<IActionResult> ExpireRefreshTokenAsync()
        {
            AuthorizeToken? cookieToken = GetCookieTokenOrNull();
            if (cookieToken == null)
            {
                return NotFound(new Response()
                {
                    IsSuccess = false,
                    ErrorCode = EErrorCode.RefreshTokenExpired
                });
            }

            string token = await _cache.GetStringAsync(cookieToken.RefreshToken);
            if (token == cookieToken.AccessToken)
            {
                await _cache.RemoveAsync(cookieToken.RefreshToken);
                Response.Cookies.Delete(ACCESS_TOKEN);
                Response.Cookies.Delete(REFRESH_TOKEN);
                Response.Cookies.Delete("accessTokenExpiration");

                return Ok(new Response()
                {
                    IsSuccess = true,
                });
            }

            return BadRequest(new Response()
            {
                ErrorCode = EErrorCode.AccessTokenNotMatch
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
                    ErrorCode = EErrorCode.EmailVerifyFail
                });
            }
        }

        private AuthorizeToken? GetCookieTokenOrNull()
        {
            if (Request.Cookies.TryGetValue(ACCESS_TOKEN, out string? accessToken) && Request.Cookies.TryGetValue(REFRESH_TOKEN, out string? refreshToken))
            {
                return new AuthorizeToken(accessToken, refreshToken);
            }

            return null;
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
                MaxAge = expirationDays,
            };

            Response.Cookies.Append(ACCESS_TOKEN, token.AccessToken, cookieOptions);
            Response.Cookies.Append(REFRESH_TOKEN, token.RefreshToken, cookieOptions);
        }
    }
}
