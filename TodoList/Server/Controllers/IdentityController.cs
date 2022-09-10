using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
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
        private readonly ILogger<IdentityController> _logger;

        public IdentityController(IUserService userService, IJwtService jwtService, IEmailService emailService, IVerifyCodeService verifyCodeService, IDistributedCache cache, ILogger<IdentityController> logger)
        {
            _userService = userService;
            _jwtService = jwtService;
            _emailService = emailService;
            _verifyCodeService = verifyCodeService;
            _cache = cache;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> RefreshAsync([FromHeader(Name = "refresh_token")] string refreshToken, [FromHeader(Name = "access_token")] string accessToken)
        {
            string? expiredAccessToken = await _cache.GetStringAsync(refreshToken);
            string errorMessage;
            if (expiredAccessToken != accessToken)
            {
                errorMessage = "Access token is not match";
            }
            else
            {
                User? user = await _jwtService.GetUserFromAccessTokenOrNullAsync(accessToken);
                if (user == null)
                {
                    errorMessage = "Wrong access token";
                }
                else
                {
                    AuthorizeToken token = _jwtService.GenerateToken(user!, TokenExpiration);
                    await _cache.SetStringAsync(token.RefreshToken, token.AccessToken);
                    Response.Headers.Add("access_token", token.AccessToken);
                    Response.Headers.Add("refresh_token", token.RefreshToken);

                    return Ok();
                }
            }

            return BadRequest(errorMessage);
        }

        [HttpPost]
        public async Task<IActionResult> LoginAsync([FromForm] string email, [FromForm] string password)
        {
            string errorMessage;
            if (!await _userService.IsEmailExistAsync(email))
            {
                errorMessage = "Not exist email";
            }
            else if (!await _userService.CanLoginAsync(email, password))
            {
                errorMessage = "Wrong password";
            }
            else
            {
                User? user = await _userService.GetUserByEmailOrNullAsync(email);
                AuthorizeToken token = _jwtService.GenerateToken(user!, TokenExpiration);
                await _cache.SetStringAsync(token.RefreshToken, token.AccessToken);
                Response.Headers.Add("access_token", token.AccessToken);
                Response.Headers.Add("refresh_token", token.RefreshToken);

                return Ok();
            }

            return BadRequest(errorMessage);
        }

        [HttpPost]
        public async Task<IActionResult> SignupAsync([FromForm] string email, [FromForm] string password, [FromForm] string name)
        {
            string errorMessage;
            if (await _userService.IsEmailExistAsync(email))
            {
                errorMessage = "Duplicate email";
            }
            else if (await _userService.IsNameExistAsync(name))
            {
                errorMessage = "Duplicate name";
            }
            else
            {
                Guid? id = await _userService.SignupAsync(email, password, name);
                return Accepted(id);
            }
            return BadRequest(errorMessage);
        }

        [HttpPost]
        public async Task<IActionResult> LogoutAsync([FromHeader(Name = "access_token")] string accessToken, [FromHeader(Name = "refresh_token")] string refreshToken)
        {
            string token = await _cache.GetStringAsync(refreshToken);
            if (token == accessToken)
            {
                await _cache.RemoveAsync(refreshToken);
                return Accepted();
            }
            return BadRequest(new { errorMessage = "Access token is not match" });
        }

        [HttpPost]
        public async Task<IActionResult> SendVerifyMail([FromForm] string email)
        {
            string code = _verifyCodeService.GetVerifyCode(VERIFY_CODE_LENGTH);
            await _verifyCodeService.SetVerifyCodeAsync(email, code);

            string verifyUrl = Url.Action("VerifyCode", "Identity")!;
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
        public IActionResult VerifyCode([FromForm] string email, [FromForm] string code)
        {
            bool isVerified = _verifyCodeService.TryVerify(email, code);
            if (isVerified)
            {
                return Ok();
            }
            else
            {
                return BadRequest(new { errorMessage = "Fail to verify email" });
            }
        }
    }
}
