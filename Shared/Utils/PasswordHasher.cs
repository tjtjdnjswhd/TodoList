using System.Runtime.Versioning;
using System.Security.Cryptography;

using TodoList.Shared.Settings;

namespace TodoList.Shared.Utils
{
    public static class PasswordHasher
    {
        public static byte[] GetSalt(int length)
        {
            byte[] value = RandomNumberGenerator.GetBytes(length);
            return value;
        }

        [UnsupportedOSPlatform("browser")]
        public static byte[] HashPassword(string password, byte[] salt, PasswordHashSettings settings)
        {
            using Rfc2898DeriveBytes rfc2898DeriveBytes = new(password + settings.Pepper, salt, settings.HashIterations, HashAlgorithmName.SHA512);
            byte[] hash = rfc2898DeriveBytes.GetBytes(settings.HashLength);
            return hash;
        }
    }
}
