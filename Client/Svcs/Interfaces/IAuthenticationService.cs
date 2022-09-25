using TodoList.Shared.Models;

namespace TodoList.Client.Svcs.Interfaces
{
    public interface IAuthenticationService
    {
        Task<bool> LoginAsync(LoginInfo loginInfo);
        Task<bool> SignupAsync(SignupInfo signupInfo);
        Task LogoutAsync();
        Task<HttpResponseMessage> RefreshAsync();
        Task<bool> IsEmailExistAsync(string email);
        Task<bool> IsNameExistAsync(string name);
    }
}
