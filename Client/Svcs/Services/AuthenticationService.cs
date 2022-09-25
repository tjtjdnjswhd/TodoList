using Microsoft.AspNetCore.Components.Authorization;

using System.Net.Http.Json;

using TodoList.Client.Svcs.Interfaces;
using TodoList.Shared.Data.Dtos;
using TodoList.Shared.Models;

namespace TodoList.Client.Svcs.Services
{
    public sealed class AuthenticationService : IAuthenticationService
    {
        private readonly IClaimsService _claimsService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AuthenticationStateProvider _stateProvider;

        public AuthenticationService(IClaimsService claimsService, IHttpClientFactory httpClientFactory, AuthenticationStateProvider stateProvider)
        {
            _claimsService = claimsService;
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
                await _claimsService.SetClaimsAsync(claimsContent!.Data);
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
            await _claimsService.RemoveClaimsAsync();
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
    }
}
