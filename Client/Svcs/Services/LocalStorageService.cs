using Microsoft.JSInterop;

using Newtonsoft.Json;

using TodoList.Client.Svcs.Interfaces;

namespace TodoList.Client.Svcs.Services
{
    public class LocalStorageService : ILocalStorageService
    {
        private readonly IJSRuntime _jsRuntime;

        public LocalStorageService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<string?> GetAsync(string key)
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", key);
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            string? json = await GetAsync(key);
            return JsonConvert.DeserializeObject<T>(json ?? string.Empty);
        }

        public async Task SetAsync(string key, string value)
        {
            await SetAsync<string>(key, value);
        }

        public async Task SetAsync<T>(string key, T value)
        {
            string json = JsonConvert.SerializeObject(value);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", key, json);
        }

        public async Task RemoveAsync(string key)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", key);
        }
    }
}
