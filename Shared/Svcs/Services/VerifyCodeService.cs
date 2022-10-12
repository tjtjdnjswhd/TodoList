using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using System.Security.Cryptography;
using System.Text;

using TodoList.Shared.Settings;
using TodoList.Shared.Svcs.Interfaces;

namespace TodoList.Shared.Svcs.Services
{
    public sealed class VerifyCodeService : IVerifyCodeService
    {
        private readonly IDistributedCache _cache;
        private readonly VerifyCodeSettings _settings;

        public VerifyCodeService(IDistributedCache cache, IOptions<VerifyCodeSettings> settings)
        {
            _cache = cache;
            _settings = settings.Value;
        }

        public string GetVerifyCode(int length)
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(length));
        }

        public Task SetVerifyCodeAsync(string key, string code)
        {
            return _cache.SetStringAsync(key, code, new DistributedCacheEntryOptions()
            {
                SlidingExpiration = _settings.SlidingExpiration,
            });
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
