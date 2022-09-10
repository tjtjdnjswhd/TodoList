using TodoList.Shared.Data.Models;

namespace TodoList.Shared.Svcs.Interfaces
{
    public interface IUserService
    {
        public Task<User?> GetUserByIdOrNullAsync(Guid id);
        public Task<User?> GetUserByEmailOrNullAsync(string email);
        public Task<User?> GetUserByNameOrNullAsync(string name);
        public Task<bool> IsNameExistAsync(string name);
        public Task<bool> IsEmailExistAsync(string email);
        public Task<bool> CanLoginAsync(string email, string password);
        public Task<Guid?> SignupAsync(string email, string password, string name);
        public Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword);
        public Task<bool> ChangeNameAsync(Guid id, string newName);
        public Task<bool> ChangeEmailAsync(Guid id, string newEmail);
    }
}