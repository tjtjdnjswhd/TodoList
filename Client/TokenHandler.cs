using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;

using TodoList.Client.Svcs.Interfaces;

namespace TodoList.Client
{
    public class TokenHandler : DelegatingHandler
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly Uri _refreshUri;
        private readonly IJSRuntime _jsRuntime;
        private readonly NavigationManager _navigationManager;

        public TokenHandler(IAuthenticationService authenticationService, IWebAssemblyHostEnvironment hostEnvironment, IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            _authenticationService = authenticationService;
            _refreshUri = new($"{hostEnvironment.BaseAddress}api/identity/refresh");
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (request.RequestUri != _refreshUri
                && response.Headers.TryGetValues("IS-ACCESS-TOKEN-EXPIRED", out IEnumerable<string>? values)
                && values.Any(v => v.Equals("true", StringComparison.OrdinalIgnoreCase)))
            {
                HttpResponseMessage refreshResponse = await _authenticationService.RefreshAsync();

                if (refreshResponse.Headers.TryGetValues("IS-REFRESH-TOKEN-EXPIRED", out values)
                    && values.Any(v => v.Equals("true", StringComparison.OrdinalIgnoreCase)))
                {
                    await _jsRuntime.InvokeVoidAsync("Alert", "토큰이 만료되 재 로그인 후 이용해 주시기 바랍니다");
                    await _authenticationService.LogoutAsync();

                    _navigationManager.NavigateTo("/login", true);
                }
                else
                {
                    response = await base.SendAsync(request, cancellationToken);
                }
            }

            return response;
        }
    }
}
