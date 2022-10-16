using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;

namespace TodoList.Shared.Svcs.Interfaces
{
    public interface IUserService
    {
        public Task<User?> GetUserByIdOrNullAsync(Guid id);
        public Task<User?> GetUserByEmailOrNullAsync(string email);
        public Task<User?> GetUserByNameOrNullAsync(string name);
        public Task<bool> IsNameExistAsync(string name);
        public Task<bool> IsEmailExistAsync(string email);
        public Task<bool> CanLoginAsync(LoginInfo loginInfo);
        public Task<Guid?> SignupAsync(SignupInfo signupInfo);
        public Task VerifyEmailAsync(string email);
        public Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
        public Task<bool> ChangeNameAsync(Guid id, string newName);
    }
}