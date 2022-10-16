using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<VerifyCodeService> _logger;

        public VerifyCodeService(IDistributedCache cache, IOptions<VerifyCodeSettings> settings, ILogger<VerifyCodeService> logger)
        {
            _cache = cache;
            _settings = settings.Value;
            _logger = logger;
        }

        public string GetVerifyCode(int length)
        {
            string code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(length));
            _logger.LogDebug("Verify code generated. code: {code}", code);
            return code;
        }

        public Task SetVerifyCodeAsync(string key, string code)
        {
            _logger.LogDebug("Verify code set to cache. key: {key}, code: {code}", key, code);
            return _cache.SetStringAsync(key, code, new DistributedCacheEntryOptions()
            {
                SlidingExpiration = _settings.SlidingExpiration,
            });
        }

        public Task RemoveVerifyCodeAsync(string key)
        {
            _logger.LogDebug("Verify code deleted. key: {key}", key);
            return _cache.RemoveAsync(key);
        }

        public bool IsVerifyCodeMatch(string key, string code)
        {
            string? value = _cache.GetString(key);
            return value != null && value == code;
        }
    }
}
