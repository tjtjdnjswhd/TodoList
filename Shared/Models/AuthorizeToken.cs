#nullable disable

namespace TodoList.Shared.Models
{
    public sealed class AuthorizeToken
    {
        public AuthorizeToken()
        {

        }

        public AuthorizeToken(string accessToken, string refreshToken)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
        }

        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
