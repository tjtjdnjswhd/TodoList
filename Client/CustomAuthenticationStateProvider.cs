using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using TodoList.Client.Svcs.Interfaces;
using TodoList.Shared.Data.Dtos;

namespace TodoList.Client
{
    public sealed class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState EMPTY_STATE = new(new());

        private readonly IJSRuntime _jsRuntime;
        private readonly ILocalStorageService _localStorageService;

        public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, ILocalStorageService localStorageService)
        {
            _jsRuntime = jsRuntime;
            _localStorageService = localStorageService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? expiration = await _jsRuntime.InvokeAsync<string>("GetCookie", "accessTokenExpiration");
            if (expiration == null)
            {
                NotifyAuthenticationStateChanged(Task.FromResult(EMPTY_STATE));
                return EMPTY_STATE;
            }

            IEnumerable<ClaimDto>? claimsDtos = await _localStorageService.GetAsync<IEnumerable<ClaimDto>>("claims");
            IEnumerable<Claim>? claims = claimsDtos?.Select(c => new Claim(c.Type, c.Value));

            if (claims == null)
            {
                NotifyAuthenticationStateChanged(Task.FromResult(EMPTY_STATE));
                return EMPTY_STATE;
            }

            ClaimsIdentity identity = new(claims, "Bearer", JwtRegisteredClaimNames.Sub, ClaimTypes.Role);
            ClaimsPrincipal claimsPrincipal = new(identity);
            AuthenticationState state = new(claimsPrincipal);
            NotifyAuthenticationStateChanged(Task.FromResult(state));
            return state;
        }
    }
}
