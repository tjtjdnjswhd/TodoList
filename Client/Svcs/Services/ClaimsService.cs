using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using TodoList.Client.Svcs.Interfaces;
using TodoList.Shared.Data.Dtos;

namespace TodoList.Client.Svcs.Services
{
    public class ClaimsService : IClaimsService
    {
        private readonly ILocalStorageService _localStorageService;

        public ClaimsService(ILocalStorageService localStorageService)
        {
            _localStorageService = localStorageService;
        }

        public async Task<ClaimsIdentity?> GetClaimsIdentityOrNullAsync()
        {
            IEnumerable<ClaimDto>? claimsDtos = await _localStorageService.GetAsync<IEnumerable<ClaimDto>>("claims");
            if (claimsDtos == null)
            {
                return null;
            }

            IEnumerable<Claim>? claims = claimsDtos.Select(c => new Claim(c.Type, c.Value));
            ClaimsIdentity? identity = new(claims, "Bearer", JwtRegisteredClaimNames.Sub, ClaimTypes.Role);
            return identity;
        }

        public async Task SetClaimsAsync(IEnumerable<ClaimDto> claims)
        {
            await _localStorageService.SetAsync("claims", claims);
        }

        public async Task RemoveClaimsAsync()
        {
            await _localStorageService.RemoveAsync("claims");
        }
    }
}
