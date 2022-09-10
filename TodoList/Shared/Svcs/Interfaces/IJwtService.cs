using TodoList.Shared.Data.Models;
using TodoList.Shared.Models;

namespace TodoList.Shared.Svcs.Interfaces
{
    public interface IJwtService
    {
        public AuthorizeToken GenerateToken(User user, DateTimeOffset absoluteExpiration);
        public string GetRefreshToken();
        public string GetRefreshTokenKey(Guid userId);
        public Task<User?> GetUserFromAccessTokenOrNullAsync(string accessToken);
    }
}
