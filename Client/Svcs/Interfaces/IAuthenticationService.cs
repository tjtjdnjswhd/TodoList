using TodoList.Shared.Models;

namespace TodoList.Client.Svcs.Interfaces
{
    public interface IAuthenticationService
    {
        Task<EErrorCode> LoginAsync(LoginInfo loginInfo);
        Task<EErrorCode> SignupAsync(SignupInfo signupInfo);
        Task LogoutAsync();
        Task<HttpResponseMessage> RefreshAsync();
        Task<bool> IsEmailExistAsync(string email);
        Task<bool> IsNameExistAsync(string name);
        Task<bool> VerifyEmailAsync(string email, string code);
    }
}
