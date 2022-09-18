using Microsoft.JSInterop;

using System.Security.Claims;

using TodoList.Client.Svcs.Interfaces;
using TodoList.Shared.Data.Dtos;

namespace TodoList.Client.Svcs.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly IJSRuntime _jsRuntime;

        public AuthenticationService(ILocalStorageService localStorageService, IJSRuntime jsRuntime)
        {
            _localStorageService = localStorageService;
            _jsRuntime = jsRuntime;
        }

        public async Task<bool> IsClaimExpiredAsync()
        {
            return (await _jsRuntime.InvokeAsync<string>("GetCookie", "expiration")) == null;
        }

        public async Task<IEnumerable<Claim>?> GetClaimsOrNullAsync()
        {
            IEnumerable<ClaimDto>? claimsDtos = await _localStorageService.GetAsync<IEnumerable<ClaimDto>>("claims");
            IEnumerable<Claim>? claims = claimsDtos?.Select(c => new Claim(c.Type, c.Value));
            return claims;
        }

        public async Task SetClaimsAsync(IEnumerable<ClaimDto> claimDtos)
        {
            await _localStorageService.SetAsync("claims", claimDtos);
        }

        public async Task RemoveClaimsAsync()
        {
            await _localStorageService.RemoveAsync("claims");
        }
    }
}
