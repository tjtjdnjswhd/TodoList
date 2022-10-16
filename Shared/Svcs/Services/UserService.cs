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
            User? user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
            _logger.LogDebug("Return user. user id: {userId}", user?.Id ?? null);
            return user;
        }

        public async Task<User?> GetUserByEmailOrNullAsync(string email)
        {
            User? user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
            _logger.LogDebug("Return user. user id: {userId}", user?.Id ?? null);
            return user;
        }

        public async Task<User?> GetUserByNameOrNullAsync(string name)
        {
            User? user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Name == name);
            _logger.LogDebug("Return user. user id: {userId}", user?.Id ?? null);
            return user;
        }

        [UnsupportedOSPlatform("browser")]
        public async Task<bool> CanLoginAsync(LoginInfo loginInfo)
        {
            User? user = await GetUserByEmailOrNullAsync(loginInfo.Email);
            if (user == null)
            {
                _logger.LogDebug("User not found. user email: {userEmail}", loginInfo.Email);
                return false;
            }

            string hashBase64 = Convert.ToBase64String(PasswordHashHelper.HashPassword(loginInfo.Password, Convert.FromBase64String(user.SaltBase64), _hashSettings));
            bool result = user.PasswordHashBase64 == hashBase64;
            _logger.LogTrace("Password is {message}. user id: {userId}", result ? "match" : "not match", user.Id);
            return result;
        }

        [UnsupportedOSPlatform("browser")]
        public async Task<Guid?> SignupAsync(SignupInfo signupInfo)
        {
            if (await IsEmailExistAsync(signupInfo.Email) || await IsNameExistAsync(signupInfo.Name))
            {
                _logger.LogDebug("Sign up fail. info: {@info}", signupInfo);
                return null;
            }

            byte[] salt = PasswordHashHelper.GetSalt(_hashSettings.SaltLength);
            byte[] hash = PasswordHashHelper.HashPassword(signupInfo.Password, salt, _hashSettings);

            string saltBase64 = Convert.ToBase64String(salt);
            string hashBase64 = Convert.ToBase64String(hash);

            User user = new(signupInfo.Email, signupInfo.Name, false, hashBase64, saltBase64, "User");
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();

            _logger.LogInformation("User signup. user id: {userId}", user.Id);
            return user.Id;
        }

        public async Task VerifyEmailAsync(string email)
        {
            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                _logger.LogInformation("User email verified. user id: {userId}", user.Id);
                user.IsEmailVerified = true;
                await _dbContext.SaveChangesAsync();
            }
            _logger.LogDebug("User not found. email: {email}", email);
        }

        [UnsupportedOSPlatform("browser")]
        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                _logger.LogDebug("User not found. user id: {userId}", id);
                return false;
            }

            byte[] salt = Convert.FromBase64String(user.SaltBase64);
            byte[] oldHash = PasswordHashHelper.HashPassword(oldPassword, salt, _hashSettings);

            if (!Convert.FromBase64String(user.PasswordHashBase64).SequenceEqual(oldHash))
            {
                _logger.LogInformation("Password is not match. user id: {userId}", id);
                return false;
            }

            byte[] newSalt = PasswordHashHelper.GetSalt(_hashSettings.SaltLength);
            byte[] newHash = PasswordHashHelper.HashPassword(newPassword, newSalt, _hashSettings);

            string newHashBase64 = Convert.ToBase64String(newHash);
            string newSaltBase64 = Convert.ToBase64String(newSalt);

            user.PasswordHashBase64 = newHashBase64;
            user.SaltBase64 = newSaltBase64;
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Password and salt changed. user id: {userId}", id);
            return true;
        }

        public async Task<bool> ChangeNameAsync(Guid id, string newName)
        {
            User? user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                _logger.LogDebug("User not found. user id: {userId}", id);
                return false;
            }

            _logger.LogInformation("User name changed. old name: {oldName}, new name: {newName}", user.Name, newName);
            user.Name = newName;
            await _dbContext.SaveChangesAsync();

            return true;
        }
    }
}
