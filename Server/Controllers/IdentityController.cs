using AutoMapper;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

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
            bool isEmailExist = await _userService.IsEmailExistAsync(email);
            _logger.LogTrace("{email} is {message}", email, isEmailExist ? "exist" : "not exist");
            return Ok(new Response<bool>(EErrorCode.NoError, isEmailExist));
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
            bool isNameExist = await _userService.IsNameExistAsync(name);
            _logger.LogTrace("{name} is {message}", name, isNameExist ? "exist" : "not exist");
            return Ok(new Response<bool>(EErrorCode.NoError, isNameExist));
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
                _logger.LogInformation("Refresh fail. refresh token already expired");
                return NotFound(new Response(EErrorCode.RefreshTokenExpired));
            }

            // refresh token에 대응되는 access token
            string? cachedAccessToken = await _cache.GetStringAsync(cookieToken.RefreshToken);

            // refresh token을 조작하지 않았다면 반드시 값이 일치해야 함
            if (cachedAccessToken != cookieToken.AccessToken)
            {
                _logger.LogInformation("Refresh fail. cached access token is not match. cached access token: {cachedAccessToken}, cookie token: {@cookieToken}", cachedAccessToken, cookieToken);
                return BadRequest(new Response(EErrorCode.CachedAccessTokenNotMatch));
            }
            else
            {
                User? user = await _jwtService.GetUserByTokenOrNullAsync(cookieToken.AccessToken);
                if (user == null)
                {
                    _logger.LogInformation("Refresh fail. wrong access token. access token: {accessToken}", cookieToken.AccessToken);
                    return BadRequest(new Response(EErrorCode.WrongAccessToken));
                }
                else
                {
                    AuthorizeToken token = _jwtService.GenerateToken(user!, DateTimeOffset.Now.Add(_authorizeTokenSetting.AccessTokenExpiration));
                    await SetCookieTokenAsync(token);
                    _logger.LogInformation("Refresh success. old access token: {oldAccessToken}", cachedAccessToken);
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
            errorCode |= await _userService.IsEmailExistAsync(loginInfo.Email) ? EErrorCode.NoError : EErrorCode.EmailNotExist;
            errorCode |= await _userService.CanLoginAsync(loginInfo) ? EErrorCode.NoError : EErrorCode.WrongPassword;

            if (errorCode != EErrorCode.NoError)
            {
                _logger.LogInformation("Login fail. info: {@info}, error code: {errorCode}", loginInfo, errorCode);
                return NotFound(new Response(errorCode));
            }

            User? user = await _userService.GetUserByEmailOrNullAsync(loginInfo.Email);

            if (!user!.IsEmailVerified)
            {
                _logger.LogInformation("Login fail. info: {@info}, error code: {errorCode}", loginInfo, EErrorCode.EmailNotVerified);
                return BadRequest(new Response(EErrorCode.EmailNotVerified));
            }

            AuthorizeToken token = _jwtService.GenerateToken(user!, DateTimeOffset.Now.Add(_authorizeTokenSetting.AccessTokenExpiration));
            IEnumerable<Claim> claims = _jwtService.GetClaimsByTokenOrNull(token.AccessToken)!;
            await SetCookieTokenAsync(token);

            IEnumerable<ClaimDto> claimDtos = _mapper.Map<IEnumerable<Claim>, IEnumerable<ClaimDto>>(claims);
            _logger.LogInformation("Login success. info: {@info}", loginInfo);
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
                _logger.LogInformation("Sign up fail. info: {@info}, error code: {errorCode}", signupInfo, errorCode);
                return BadRequest(new Response(errorCode));
            }

            await _userService.SignupAsync(signupInfo);
            await SendVerifyMailAsync(signupInfo.Email, signupInfo.EmailVerifyUrl);
            _logger.LogInformation("Sign up success. info: {@info}", signupInfo);
            return Accepted(new Response(EErrorCode.NoError));
        }

        /// <summary>
        /// 비밀번호를 변경합니다.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="newPassword"></param>
        /// <param name="newPasswordCheck"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> ChangePassword(string password, string newPassword, string newPasswordCheck)
        {
            if (newPassword != newPasswordCheck)
            {
                ModelState.AddModelError("", "password not match");
                return UnprocessableEntity(ModelState);
            }

            string email = User.FindFirstValue(JwtRegisteredClaimNames.Email);
            if (!await _userService.CanLoginAsync(new LoginInfo()
            {
                Email = email,
                Password = password
            }))
            {
                return BadRequest(new Response(EErrorCode.WrongPassword));
            }

            Guid id = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Jti));

            bool result = await _userService.ChangePasswordAsync(id, password, newPassword);
            if (result)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// 이름을 변경합니다.
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(Response), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ChangeName(string newName)
        {
            Guid id = Guid.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Jti));
            if (await _userService.IsNameExistAsync(newName))
            {
                return BadRequest(new Response(EErrorCode.UserNameDuplicate));
            }
            bool result = await _userService.ChangeNameAsync(id, newName);

            if (result)
            {
                return Ok();
            }
            else
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// 토큰을 파기합니다
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
                _logger.LogInformation("Expire fail. refresh token already expired");
                return NotFound(new Response(EErrorCode.RefreshTokenExpired));
            }

            string cachedAccessToken = await _cache.GetStringAsync(cookieToken.RefreshToken);
            if (cachedAccessToken != cookieToken.AccessToken)
            {
                _logger.LogInformation("Expire fail. cached access token is not match. cached access token: {cachedAccessToken}", cachedAccessToken);
                return BadRequest(new Response(EErrorCode.CachedAccessTokenNotMatch));
            }

            //cache, cookie의 token 삭제
            await _cache.RemoveAsync(cookieToken.RefreshToken);
            Response.Cookies.Delete(_authorizeTokenSetting.AccessTokenKey);
            Response.Cookies.Delete(_authorizeTokenSetting.RefreshTokenKey);
            _logger.LogInformation("Cached token, cookie token deleted");

            return Ok(new Response(EErrorCode.NoError));
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
            //query string으로 넘어온 공백 처리
            code = code.Replace(' ', '+');

            bool isVerified = _verifyCodeService.IsVerifyCodeMatch(email, code);
            if (isVerified)
            {
                await _userService.VerifyEmailAsync(email);
                await _verifyCodeService.RemoveVerifyCodeAsync(email);
                _logger.LogInformation("Email verify success. email: {email}", email);
                return Ok(new Response(EErrorCode.NoError));
            }
            else
            {
                _logger.LogInformation("Email verify fail. email: {email}", email);
                return BadRequest(new Response(EErrorCode.EmailVerifyFail));
            }
        }

        /// <summary>
        /// 인증 메일을 전송합니다.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="url">form의 action url</param>
        /// <returns></returns>
        private async Task SendVerifyMailAsync(string email, string url)
        {
            string code = _verifyCodeService.GetVerifyCode(VERIFY_CODE_LENGTH);
            await _verifyCodeService.SetVerifyCodeAsync(email, code);
            StringBuilder sb = new(System.IO.File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "EmailTemplate.html")));
            sb.Replace("{ACTION}", url).Replace("{METHOD}", "get").Replace("{EMAIL}", email).Replace("{CODE}", code);

            string body = sb.ToString();

            MailRequest mailRequest = new(email, "TodoList 인증 메일", body, true, Encoding.UTF8);

            try
            {
                await _emailService.SendEmailAsync(mailRequest);
            }
            catch
            {
                _logger.LogError("Verify mail send fail. email: {email}, request: {@request}", email, mailRequest);
                throw;
            }
            _logger.LogInformation("Verify mail send success. email: {email}, request: {@request}", email, mailRequest);
        }

        /// <summary>
        /// Cookie의 access token, refresh token을 반환합니다.
        /// </summary>
        /// <returns>access token, refresh token 둘 다 있을 경우 <see cref="AuthorizeToken"/>. 아니라면 null</returns>
        private AuthorizeToken? GetCookieTokenOrNull()
        {
            if (Request.Cookies.TryGetValue(_authorizeTokenSetting.AccessTokenKey, out string? accessToken) && Request.Cookies.TryGetValue(_authorizeTokenSetting.RefreshTokenKey, out string? refreshToken))
            {
                AuthorizeToken token = new(accessToken, refreshToken);
                _logger.LogTrace("Access token and refresh token exist. token: {@token}", token);
                return token;
            }

            _logger.LogTrace("Access token or refresh token not exist");
            return null;
        }

        /// <summary>
        /// <paramref name="token"/>을 cookie에 추가합니다.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
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
                SameSite = SameSiteMode.Strict
            };

            Response.Cookies.Append(_authorizeTokenSetting.AccessTokenKey, token.AccessToken, cookieOptions);
            Response.Cookies.Append(_authorizeTokenSetting.RefreshTokenKey, token.RefreshToken, cookieOptions);
            _logger.LogInformation("Set cookie token. token: {@token}, cookie options: {@cookieOptions}", token, cookieOptions);
        }
    }
}
