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
        private readonly ILocalStorageService _localStorageService;

        public TokenHandler(IAuthenticationService authenticationService, IWebAssemblyHostEnvironment hostEnvironment, IJSRuntime jsRuntime, NavigationManager navigationManager, ILocalStorageService localStorageService)
        {
            _authenticationService = authenticationService;
            _refreshUri = new($"{hostEnvironment.BaseAddress}api/identity/refresh");
            _jsRuntime = jsRuntime;
            _navigationManager = navigationManager;
            _localStorageService = localStorageService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);

            if (request.RequestUri != _refreshUri
                && response.Headers.Any(h => h.Key.Equals("IS-ACCESS-TOKEN-EXPIRED", StringComparison.OrdinalIgnoreCase)
                && h.Value.Any(v => v.Equals("true", StringComparison.OrdinalIgnoreCase))))
            {
                HttpResponseMessage refreshResponse = await _authenticationService.RefreshAsync();

                if (refreshResponse.Headers.Any(h => h.Key.Equals("IS-REFRESH-TOKEN-EXPIRED", StringComparison.OrdinalIgnoreCase)
                    && h.Value.Any(v => v.Equals("true", StringComparison.OrdinalIgnoreCase))))
                {
                    await _jsRuntime.InvokeVoidAsync("Alert", "토큰이 만료되 재 로그인 후 이용해 주시기 바랍니다");
                    await _localStorageService.RemoveAsync("claims");

                    _navigationManager.NavigateTo("/", true);
                }

                response = await base.SendAsync(request, cancellationToken);
            }
            return response;
        }
    }
}
