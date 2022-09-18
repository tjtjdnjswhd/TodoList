namespace TodoList.Client.Svcs.Interfaces
{
    public interface ILocalStorageService
    {
        public Task<string?> GetAsync(string key);
        public Task<T?> GetAsync<T>(string key);
        public Task SetAsync(string key, string value);
        public Task SetAsync<T>(string key, T value);
        public Task RemoveAsync(string key);
    }
}
