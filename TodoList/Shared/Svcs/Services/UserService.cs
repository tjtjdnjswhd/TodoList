using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using System.Runtime.Versioning;

using TodoList.Shared.Data;
using TodoList.Shared.Data.Models;
using TodoList.Shared.Svcs.Interfaces;
using TodoList.Shared.Utils;

namespace TodoList.Shared.Svcs.Services
{
    public sealed class UserService : IUserService
    {
        private static readonly int SALT_LENGTH = 8;
        private static readonly int HASH_ITERATIONS = 10000;
        private static readonly int HASH_LENGTH = 32;
        private static readonly string PEPPER = "Acx6ZkMZ";

        private TodoListDbContext _dbContext;
        private ILogger<UserService> _logger;

        public UserService(TodoListDbContext dbContext, ILogger<UserService> logger)
        {
            _dbContext = dbContext;
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
            return await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);
        }
        public async Task<User?> GetUserByEmailOrNullAsync(string email)
        {
            return await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User?> GetUserByNameOrNullAsync(string name)
        {
            return await _dbContext.Users.SingleOrDefaultAsync(u => u.Name == name);
        }

        [UnsupportedOSPlatform("browser")]
        public async Task<bool> CanLoginAsync(string email, string password)
        {
            User? user = await GetUserByEmailOrNullAsync(email);
            if (user == null)
            {
                return false;
            }
            string hashBase64 = Convert.ToBase64String(PasswordHasher.HashPassword(password, Convert.FromBase64String(user.SaltBase64), PEPPER, HASH_ITERATIONS, HASH_LENGTH));
            return user.PasswordHashBase64 == hashBase64;
        }

        [UnsupportedOSPlatform("browser")]
        public async Task<Guid?> SignupAsync(string email, string password, string name)
        {
            if (await IsEmailExistAsync(email) || await IsNameExistAsync(name))
            {
                return null;
            }

            byte[] salt = PasswordHasher.GetSalt(SALT_LENGTH);
            byte[] hash = PasswordHasher.HashPassword(password, salt, PEPPER, HASH_ITERATIONS, HASH_LENGTH);

            string saltBase64 = Convert.ToBase64String(salt);
            string hashBase64 = Convert.ToBase64String(hash);

            User user = new(email, name, false, hashBase64, saltBase64, "User");
            _dbContext.Users.Add(user);
            _dbContext.SaveChanges();
            return _dbContext.Users.Single(u => u.Email == email).Id;
        }

        [UnsupportedOSPlatform("browser")]
        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword)
        {
            User? user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return false;
            }

            byte[] salt = Convert.FromBase64String(user.SaltBase64);
            byte[] oldHash = PasswordHasher.HashPassword(oldPassword, salt, PEPPER, HASH_ITERATIONS, HASH_LENGTH);

            if (!Convert.FromBase64String(user.PasswordHashBase64).SequenceEqual(oldHash))
            {
                return false;
            }

            byte[] newHash = PasswordHasher.HashPassword(newPassword, salt, PEPPER, HASH_ITERATIONS, HASH_LENGTH);
            string newHashBase64 = Convert.ToBase64String(newHash);
            user.PasswordHashBase64 = newHashBase64;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ChangeNameAsync(Guid id, string newName)
        {
            User? user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return false;
            }
            user.Name = newName;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ChangeEmailAsync(Guid id, string newEmail)
        {
            User? user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                return false;
            }

            user.Email = newEmail;
            user.IsEmailVerified = false;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
