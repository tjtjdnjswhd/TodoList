using System.Runtime.Versioning;
using System.Security.Cryptography;

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
        public static byte[] HashPassword(string password, byte[] salt, string pepper, int iterations, int length)
        {
            using Rfc2898DeriveBytes rfc2898DeriveBytes = new(password + pepper, salt, iterations, HashAlgorithmName.SHA512);
            byte[] hash = rfc2898DeriveBytes.GetBytes(length);
            return hash;
        }
    }
}
