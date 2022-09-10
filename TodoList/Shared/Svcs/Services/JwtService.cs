using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;
using TodoList.Shared.Svcs.Interfaces;

using JwtRegisteredClaimNames = System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames;

namespace TodoList.Shared.Svcs.Services
{
    public sealed class JwtService : IJwtService
    {
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;

        public JwtService(IConfiguration configuration, IUserService userService)
        {
            _configuration = configuration;
            _userService = userService;
        }

        public AuthorizeToken GenerateToken(User user, DateTimeOffset absoluteExpiration)
        {
            SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]));
            SigningCredentials credentials = new(securityKey, SecurityAlgorithms.HmacSha256);
            Claim[] claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Name),
                new Claim(ClaimTypes.Role, user.RoleName),
                new Claim(JwtRegisteredClaimNames.Jti, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            JwtSecurityToken token = new(issuer: _configuration["Jwt:Issuer"], audience: _configuration["Jwt:Audience"], claims: claims, expires: absoluteExpiration.UtcDateTime, signingCredentials: credentials);
            string accessToken = new JwtSecurityTokenHandler().WriteToken(token);
            string refreshToken = GetRefreshToken();
            return new AuthorizeToken(accessToken, refreshToken);
        }

        public string GetRefreshToken()
        {
            byte[] randomNumber = new byte[32];
            using RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public string GetRefreshTokenKey(Guid userId)
        {
            return $"user{userId}_refreshToken";
        }

        public async Task<User?> GetUserFromAccessTokenOrNullAsync(string accessToken)
        {
            byte[] key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]);
            TokenValidationParameters parameters = new()
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            JwtSecurityTokenHandler handler = new();
            ClaimsPrincipal principal = handler.ValidateToken(accessToken, parameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            string userId = principal.Claims.Single(c => c.Type == JwtRegisteredClaimNames.Jti).Value;
            if (!Guid.TryParse(userId, out Guid id))
            {
                return null;
            }

            User? user = await _userService.GetUserByIdOrNullAsync(id);
            return user;
        }
    }
}
