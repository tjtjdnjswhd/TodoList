using Microsoft.AspNetCore.Components.Authorization;

using System.Security.Claims;

using TodoList.Client.Svcs.Interfaces;

namespace TodoList.Client
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private static readonly AuthenticationState EMPTY_STATE = new(new());

        private readonly IAuthenticationService _authenticationService;

        public CustomAuthenticationStateProvider(IAuthenticationService authenticationService)
        {
            _authenticationService = authenticationService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            if (await _authenticationService.IsClaimExpiredAsync())
            {
                await _authenticationService.RemoveClaimsAsync();
                return EMPTY_STATE;
            }

            IEnumerable<Claim>? claims = await _authenticationService.GetClaimsOrNullAsync();
            if (claims == null)
            {
                return EMPTY_STATE;
            }

            ClaimsIdentity identity = new(claims, "application");
            ClaimsPrincipal claimsPrincipal = new(identity);
            AuthenticationState state = new(claimsPrincipal);
            return state;
        }
    }
}
