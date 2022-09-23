using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using TodoList.Client.Svcs.Interfaces;

namespace TodoList.Client
{
    public class TokenHandler : DelegatingHandler
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly Uri _refreshUri;

        public TokenHandler(IAuthenticationService authenticationService, IWebAssemblyHostEnvironment hostEnvironment)
        {
            _authenticationService = authenticationService;
            _refreshUri = new($"{hostEnvironment.BaseAddress}api/identity/refresh");
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await base.SendAsync(request, cancellationToken);
            if (request.RequestUri != _refreshUri
                && response.Headers.Any(h => h.Key.Equals("IS-ACCESS-TOKEN-EXPIRED", StringComparison.OrdinalIgnoreCase)
                && h.Value.Any(v => v.Equals("true", StringComparison.OrdinalIgnoreCase))))
            {
                await _authenticationService.RefreshAsync();
                response = await base.SendAsync(request, cancellationToken);
            }
            var debugstring = await response.Content.ReadAsStringAsync();
            return response;
        }
    }
}
