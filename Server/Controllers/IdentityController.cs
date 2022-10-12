using AutoMapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using System.Security.Claims;
using System.Text;

using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;
using TodoList.Shared.Settings;
using TodoList.Shared.Svcs.Interfaces;

namespace TodoList.Server.Controllers
{
    [ApiController]
    [Produces("application/json")]
    [Route("api/[controller]/[action]")]
    public sealed class IdentityController : ControllerBase
    {
        private static readonly int VERIFY_CODE_LENGTH = 16;

        private readonly IUserService _userService;
        private readonly IJwtService _jwtService;
        private readonly IEmailService _emailService;
        private readonly IVerifyCodeService _verifyCodeService;
        private readonly IDistributedCache _cache;
        private readonly IMapper _mapper;
        private readonly ILogger<IdentityController> _logger;
        private readonly AuthorizeTokenSetting _authorizeTokenSetting;

        public IdentityController(IUserService userService,
                                  IJwtService jwtService,
                                  IEmailService emailService,
                                  IVerifyCodeService verifyCodeService,
                                  IDistributedCache cache,
                                  IMapper mapper,
                                  IOptions<AuthorizeTokenSetting> tokenSetting,
                                  ILogger<IdentityController> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _emailService = emailService;
            _verifyCodeService = verifyCodeService;
            _cache = cache;
            _mapper = mapper;
            _logger = logger;
            _authorizeTokenSetting = tokenSetting.Value;
        }

        /// <summary>
        /// 같은 이메일이 있는지 확인해 반환합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> IsEmailExistAsync(string email)
        {
            return Ok(new Response<bool>(EErrorCode.NoError, await _userService.IsEmailExistAsync(email)));
        }

        /// <summary>
        /// 같은 이름이 있는지 확인해 반환합니다..
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> IsNameExistAsync(string name)
        {
            return Ok(new Response<bool>(EErrorCode.NoError, await _userService.IsNameExistAsync(name)));
        }

        /// <summary>
        /// 토큰을 업데이트합니다.
        /// </summary>
        /// <returns>
        /// access token, refresh token의 값이 맞는지 확인하고, 맞다면 token 값을 업데이트합니다.
        /// refresh token이 만료됬다면 설정된 <seealso cref="AuthorizeTokenSetting.IsRefreshTokenExpiredHeader"/>를 응답 헤더에 추가합니다.
        /// </returns>
        [HttpPost]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RefreshAsync()
        {
            AuthorizeToken? cookieToken = GetCookieTokenOrNull();
            if (cookieToken == null)
            {
                Response.Headers.Add(_authorizeTokenSetting.IsRefreshTokenExpiredHeader, "true");
                return NotFound(new Response(EErrorCode.RefreshTokenExpired));
            }

            string? expiredAccessToken = await _cache.GetStringAsync(cookieToken.RefreshToken);

            if (expiredAccessToken != cookieToken.AccessToken)
            {
                return BadRequest(new Response(EErrorCode.AccessTokenNotMatch));
            }
            else
            {
                User? user = await _jwtService.GetUserFromAccessTokenOrNullAsync(cookieToken.AccessToken);
                if (user == null)
                {
                    return BadRequest(new Response(EErrorCode.WrongAccessToken));
                }
                else
                {
                    AuthorizeToken token = _jwtService.GenerateToken(user!, DateTimeOffset.Now.Add(_authorizeTokenSetting.AccessTokenExpiration));
                    await SetCookieTokenAsync(token);

                    return Ok(new Response(EErrorCode.NoError));
                }
            }
        }

        /// <summary>
        /// 로그인 성공 시 access token, refresh token을 추가합니다.
        /// </summary>
        /// <param name="loginInfo"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(Response<IEnumerable<ClaimDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LoginAsync([FromBody] LoginInfo loginInfo)
        {
            EErrorCode errorCode = EErrorCode.NoError;
            if (!await _userService.IsEmailExistAsync(loginInfo.Email))
            {
                errorCode |= EErrorCode.EmailNotExist;
            }

            if (!await _userService.MatchPassword(loginInfo))
            {
                errorCode |= EErrorCode.WrongPassword;
            }

            if (errorCode != EErrorCode.NoError)
            {
                return NotFound(new Response(errorCode));
            }

            User? user = await _userService.GetUserByEmailOrNullAsync(loginInfo.Email);

            if (!user!.IsEmailVerified)
            {
                return BadRequest(new Response(errorCode));
            }

            AuthorizeToken token = _jwtService.GenerateToken(user!, DateTimeOffset.Now.Add(_authorizeTokenSetting.AccessTokenExpiration));
            IEnumerable<Claim>? claims = _jwtService.GetClaimsByTokenOrNull(token.AccessToken);
            if (claims is null)
            {
                return BadRequest(new Response(EErrorCode.WrongAccessToken));
            }
            IEnumerable<ClaimDto> claimDtos = _mapper.Map<IEnumerable<Claim>, IEnumerable<ClaimDto>>(claims);
            await SetCookieTokenAsync(token);

            return Ok(new Response<IEnumerable<ClaimDto>>(EErrorCode.NoError, claimDtos));
        }

        /// <summary>
        /// 회원가입
        /// </summary>
        /// <param name="signupInfo"></param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(Response), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SignupAsync([FromBody] SignupInfo signupInfo)
        {
            EErrorCode errorCode = EErrorCode.NoError;
            errorCode |= await _userService.IsEmailExistAsync(signupInfo.Email) ? EErrorCode.EmailDuplicate : EErrorCode.NoError;
            errorCode |= await _userService.IsNameExistAsync(signupInfo.Name) ? EErrorCode.UserNameDuplicate : EErrorCode.NoError;

            if (errorCode != EErrorCode.NoError)
            {
                return BadRequest(new Response(errorCode));
            }

            await _userService.SignupAsync(signupInfo);
            await SendVerifyMailAsync(signupInfo.Email, signupInfo.EmailVerifyUrl);
            return Accepted(new Response(EErrorCode.NoError));
        }

        /// <summary>
        /// access token, refresh token을 파기합니다
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ExpireRefreshTokenAsync()
        {
            AuthorizeToken? cookieToken = GetCookieTokenOrNull();
            if (cookieToken == null)
            {
                return NotFound(new Response(EErrorCode.RefreshTokenExpired));
            }

            string token = await _cache.GetStringAsync(cookieToken.RefreshToken);
            if (token == cookieToken.AccessToken)
            {
                await _cache.RemoveAsync(cookieToken.RefreshToken);
                Response.Cookies.Delete(_authorizeTokenSetting.AccessTokenKey);
                Response.Cookies.Delete(_authorizeTokenSetting.RefreshTokenKey);

                return Ok(new Response(EErrorCode.NoError));
            }

            return BadRequest(new Response(EErrorCode.AccessTokenNotMatch));
        }

        /// <summary>
        /// 이메일 인증 결과를 반환합니다
        /// </summary>
        /// <param name="email"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> VerifyEmailAsync(string email, string code)
        {
            code = code.Replace(' ', '+');
            bool isVerified = _verifyCodeService.IsVerifyCodeMatch(email, code);
            if (isVerified)
            {
                await _userService.VerifyEmailAsync(email);
                await _verifyCodeService.RemoveVerifyCodeAsync(email);
                return Ok(new Response(EErrorCode.NoError));
            }
            else
            {
                return BadRequest(new Response(EErrorCode.EmailVerifyFail));
            }
        }

        private async Task SendVerifyMailAsync(string email, string url)
        {
            string code = _verifyCodeService.GetVerifyCode(VERIFY_CODE_LENGTH);
            await _verifyCodeService.SetVerifyCodeAsync(email, code);
            StringBuilder sb = new(System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "EmailTemplate.html")));
            sb.Replace("{ACTION}", url).Replace("{METHOD}", "get").Replace("{EMAIL}", email).Replace("{CODE}", code);

            string body = sb.ToString();

            MailRequest mailRequest = new()
            {
                Body = body,
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
                Subject = "TodoList 인증 메일",
                To = email
            };

            await _emailService.SendEmailAsync(mailRequest);
        }

        private AuthorizeToken? GetCookieTokenOrNull()
        {
            if (Request.Cookies.TryGetValue(_authorizeTokenSetting.AccessTokenKey, out string? accessToken) && Request.Cookies.TryGetValue(_authorizeTokenSetting.RefreshTokenKey, out string? refreshToken))
            {
                return new AuthorizeToken(accessToken, refreshToken);
            }

            return null;
        }

        private async Task SetCookieTokenAsync(AuthorizeToken token)
        {
            await _cache.SetStringAsync(token.RefreshToken, token.AccessToken, new DistributedCacheEntryOptions()
            {
                SlidingExpiration = _authorizeTokenSetting.RefreshTokenExpiration
            });

            CookieOptions cookieOptions = new()
            {
                HttpOnly = true,
                Secure = true,
                MaxAge = _authorizeTokenSetting.RefreshTokenExpiration,
            };

            Response.Cookies.Append(_authorizeTokenSetting.AccessTokenKey, token.AccessToken, cookieOptions);
            Response.Cookies.Append(_authorizeTokenSetting.RefreshTokenKey, token.RefreshToken, cookieOptions);
        }
    }
}
