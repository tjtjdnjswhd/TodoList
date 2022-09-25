using Microsoft.AspNetCore.Components.Authorization;

using System.Security.Claims;

using TodoList.Client.Svcs.Interfaces;

namespace TodoList.Client
{
    public sealed class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState EMPTY_STATE = new(new());

        private readonly IClaimsService _claimsService;

        public CustomAuthenticationStateProvider(IClaimsService claimsService)
        {
            _claimsService = claimsService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            ClaimsIdentity? identity = await _claimsService.GetClaimsIdentityOrNullAsync();

            if (identity == null)
            {
                NotifyAuthenticationStateChanged(Task.FromResult(EMPTY_STATE));
                return EMPTY_STATE;
            }

            ClaimsPrincipal claimsPrincipal = new(identity);
            AuthenticationState state = new(claimsPrincipal);
            NotifyAuthenticationStateChanged(Task.FromResult(state));
            return state;
        }
    }
}
