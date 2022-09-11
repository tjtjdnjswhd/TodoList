using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Text;

using TodoList.Shared.Svcs.Interfaces;

namespace TodoList.Shared.Svcs.Services
{
    public sealed class VerifyCodeService : IVerifyCodeService
    {
        private readonly IDistributedCache _cache;
        private readonly DistributedCacheEntryOptions _options;

        public VerifyCodeService(IDistributedCache cache, IOptions<DistributedCacheEntryOptions> options)
        {
            _cache = cache;
            _options = options.Value;
        }

        public string GetVerifyCode(int length)
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(length));
        }

        public Task SetVerifyCodeAsync(string key, string code)
        {
            return _cache.SetStringAsync(key, code, _options);
        }

        public Task RemoveVerifyCodeAsync(string key)
        {
            return _cache.RemoveAsync(key);
        }

        public bool IsVerifyCodeMatch(string key, string code)
        {
            string? value = _cache.GetString(key);
            return value != null && value == code;
        }
    }
}
