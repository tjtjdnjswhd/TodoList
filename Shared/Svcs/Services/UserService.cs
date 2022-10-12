using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using System.Runtime.Versioning;

using TodoList.Shared.Data;
using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;
using TodoList.Shared.Settings;
using TodoList.Shared.Svcs.Interfaces;
using TodoList.Shared.Utils;

namespace TodoList.Shared.Svcs.Services
{
    public sealed class UserService : IUserService
    {
        private readonly TodoListDbContext _dbContext;
        private readonly PasswordHashSettings _hashSettings;
        private readonly ILogger<UserService> _logger;

        public UserService(TodoListDbContext dbContext, IOptions<PasswordHashSettings> passwordHashSettings, ILogger<UserService> logger)
        {
            _dbContext = dbContext;
            _hashSettings = passwordHashSettings.Value;
            _logger = logger;
        }

        public async Task<bool> IsEmailExistAsync(string email)
        {
            return await _dbContext.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsNameExistAsync(string name)
        {
            return await _dbContext.Users.AnyAsync(u => u.Name == name);
        }

        public async Task<User?> GetUserByIdOrNullAsync(Guid id)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserByEmailOrNullAsync(string email)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByNameOrNullAsync(string name)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(u => u.Name == name);
        }

        [UnsupportedOSPlatform("browser")]
        public async Task<bool> MatchPassword(LoginInfo loginInfo)
        {
            User? user = await GetUserByEmailOrNullAsync(loginInfo.Email);
            if (user == null)
            {
                return false;
            }

            string hashBase64 = Convert.ToBase64String(PasswordHasher.HashPassword(loginInfo.Password, Convert.FromBase64String(user.SaltBase64), _hashSettings));
            return user.PasswordHashBase64 == hashBase64;
        }

        [UnsupportedOSPlatform("browser")]
        public async Task<Guid?> SignupAsync(SignupInfo signupInfo)
        {
            if (await IsEmailExistAsync(signupInfo.Email) || await IsNameExistAsync(signupInfo.Name))
            {
                return null;
            }

            byte[] salt = PasswordHasher.GetSalt(_hashSettings.SaltLength);
            byte[] hash = PasswordHasher.HashPassword(signupInfo.Password, salt, _hashSettings);

            string saltBase64 = Convert.ToBase64String(salt);
            string hashBase64 = Convert.ToBase64String(hash);

            User user = new(signupInfo.Email, signupInfo.Name, false, hashBase64, saltBase64, "User");
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();

            return _dbContext.Users.Single(u => u.Email == signupInfo.Email).Id;
        }

        public async Task VerifyEmailAsync(string email)
        {
            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                user.IsEmailVerified = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        [UnsupportedOSPlatform("browser")]
        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(user.SaltBase64);
            byte[] oldHash = PasswordHasher.HashPassword(oldPassword, salt, _hashSettings);

            if (!Convert.FromBase64String(user.PasswordHashBase64).SequenceEqual(oldHash))
            {
                return false;
            }

            byte[] newHash = PasswordHasher.HashPassword(newPassword, salt, _hashSettings);
            string newHashBase64 = Convert.ToBase64String(newHash);
            user.PasswordHashBase64 = newHashBase64;
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangeNameAsync(Guid id, string newName)
        {
            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return false;
            }

            user.Name = newName;
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}
