using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;
using TodoList.Shared.Settings;
using TodoList.Shared.Svcs.Interfaces;

using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace TodoList.Shared.Svcs.Services
{
    public sealed class JwtService : IJwtService
    {
        private readonly IUserService _userService;
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<JwtService> _logger;

        public JwtService(IUserService userService, IOptions<JwtSettings> jwtSettings, ILogger<JwtService> logger)
        {
            _userService = userService;
            _jwtSettings = jwtSettings.Value;
            _logger = logger;
        }

        public AuthorizeToken GenerateToken(User user, DateTimeOffset absoluteExpiration)
        {
            SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
            SigningCredentials credentials = new(securityKey, _jwtSettings.SecurityAlgorithmName);
            Claim[] claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Name),
                new Claim(ClaimTypes.Role, user.RoleName),
                new Claim(JwtRegisteredClaimNames.Jti, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            JwtSecurityToken securifyToken = new(issuer: _jwtSettings.Issuer, audience: _jwtSettings.Audience, claims: claims, expires: absoluteExpiration.UtcDateTime, signingCredentials: credentials);
            _logger.LogDebug("Security token created. settings: {@settings}, claims = {@claims}, absoluteExpiration: {absoluteExpiration}", _jwtSettings, claims, absoluteExpiration);

            string accessToken = new JwtSecurityTokenHandler().WriteToken(securifyToken);
            string refreshToken = GetRefreshToken();
            AuthorizeToken authorizeToken = new(accessToken, refreshToken);

            _logger.LogDebug("Token generated. token: {@token}", authorizeToken);
            return authorizeToken;
        }

        public IEnumerable<Claim>? GetClaimsByTokenOrNull(string accessToken)
        {
            JwtSecurityTokenHandler tokenHandler = new();
            if (tokenHandler.CanReadToken(accessToken))
            {
                IEnumerable<Claim> claims = tokenHandler.ReadJwtToken(accessToken).Claims;
                _logger.LogDebug("Return claim. access token: {accessToken}, claims: {@claims}", accessToken, claims);
                return claims;
            }
            _logger.LogDebug("Read access token fail. access token: {accessToken}", accessToken);
            return null;
        }

        public string GetRefreshToken()
        {
            byte[] randomNumber = new byte[32];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public async Task<User?> GetUserFromAccessTokenOrNullAsync(string accessToken)
        {
            JwtSecurityTokenHandler tokenHandler = new();
            if (tokenHandler.CanReadToken(accessToken) && Guid.TryParse(tokenHandler.ReadJwtToken(accessToken).Id, out Guid id))
            {
                User? user = await _userService.GetUserByIdOrNullAsync(id);
                _logger.LogDebug("Return user. userId: {userId}, access token: {accessToken}", id, accessToken);
                return user;
            }
            _logger.LogDebug("Read access token fail. access token: {accessToken}", accessToken);
            return null;
        }
    }
}
