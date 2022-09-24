using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.JSInterop;

using System.Net.Http.Json;
using System.Security.Claims;

using TodoList.Client.Svcs.Interfaces;
using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Models;

namespace TodoList.Client.Svcs.Services
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private readonly ILocalStorageService _localStorageService;
        private readonly IJSRuntime _jsRuntime;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AuthenticationStateProvider _stateProvider;

        public AuthenticationService(ILocalStorageService localStorageService, IJSRuntime jsRuntime, IHttpClientFactory httpClientFactory, AuthenticationStateProvider stateProvider)
        {
            _localStorageService = localStorageService;
            _jsRuntime = jsRuntime;
            _httpClientFactory = httpClientFactory;
            _stateProvider = stateProvider;
        }

        public async Task<bool> LoginAsync(LoginInfo loginInfo)
        {
            var httpClient = _httpClientFactory.CreateClient();
            HttpResponseMessage responseMessage = await httpClient.PostAsJsonAsync("api/identity/login", loginInfo);
            if (!responseMessage.IsSuccessStatusCode)
            {
                return false;
            }

            HttpResponseMessage claimsResponse = await httpClient.GetAsync("api/identity/getclaims");
            claimsResponse.EnsureSuccessStatusCode();
            Response<IEnumerable<ClaimDto>>? claimsContent = await claimsResponse.Content.ReadFromJsonAsync<Response<IEnumerable<ClaimDto>>>();

            if (!claimsContent?.IsSuccess ?? true || claimsContent.Data == null)
            {
                await LogoutAsync();
                return false;
            }
            else
            {
                await _localStorageService.SetAsync("claims", claimsContent!.Data);
            }

            await _stateProvider.GetAuthenticationStateAsync();

            return true;
        }

        public async Task<bool> SignupAsync(SignupInfo signupInfo)
        {
            var httpClient = _httpClientFactory.CreateClient();
            HttpResponseMessage responseMessage = await httpClient.PostAsJsonAsync("api/identity/signup", signupInfo);
            if (!responseMessage.IsSuccessStatusCode)
            {
                return false;
            }

            Response<Guid>? response = await responseMessage.Content.ReadFromJsonAsync<Response<Guid>>();
            return response?.IsSuccess ?? false;
        }

        public async Task LogoutAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            await httpClient.PostAsync("api/identity/expirerefreshtoken", null);
            await _localStorageService.RemoveAsync("claims");
            await _stateProvider.GetAuthenticationStateAsync();
        }

        public async Task<HttpResponseMessage> RefreshAsync()
        {
            var httpClient = _httpClientFactory.CreateClient();
            return await httpClient.PostAsync("api/identity/refresh", null);
        }

        public async Task<bool> IsEmailExistAsync(string email)
        {
            var httpClient = _httpClientFactory.CreateClient();
            Response<bool>? response = await httpClient.GetFromJsonAsync<Response<bool>>($"api/identity/isemailexist?email={email}");
            return response?.Data ?? true;
        }

        public async Task<bool> IsNameExistAsync(string name)
        {
            var httpClient = _httpClientFactory.CreateClient();
            Response<bool>? response = await httpClient.GetFromJsonAsync<Response<bool>>($"api/identity/isnameexist?name={name}");
            return response?.Data ?? true;
        }

        public async Task<Guid?> GetUserIdOrNull()
        {
            IEnumerable<Claim>? claims = await GetClaimsOrNullAsync();
            string? id = claims?.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
            if (Guid.TryParse(id, out Guid userId))
            {
                return userId;
            }
            return null;
        }

        public async Task<IEnumerable<Claim>?> GetClaimsOrNullAsync()
        {
            IEnumerable<ClaimDto>? claimsDtos = await _localStorageService.GetAsync<IEnumerable<ClaimDto>>("claims");
            IEnumerable<Claim>? claims = claimsDtos?.Select(c => new Claim(c.Type, c.Value));
            return claims;
        }

        public async Task<bool> IsClaimExpiredAsync()
        {
            return (await _jsRuntime.InvokeAsync<string>("GetCookie", "accessTokenExpiration")) == null;
        }

        public async Task<bool> IsAccessTokenExpiredAsync()
        {
            string? accessTokenExpiration = await _jsRuntime.InvokeAsync<string>("GetCookie", "accessTokenExpiration");
            if (DateTimeOffset.TryParse(accessTokenExpiration, out DateTimeOffset expires))
            {
                return expires <= DateTimeOffset.Now;
            }
            return true;
        }
    }
}
