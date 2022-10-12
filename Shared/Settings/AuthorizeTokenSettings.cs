#nullable disable

namespace TodoList.Shared.Settings
{
    public class AuthorizeTokenSetting
    {
        public TimeSpan AccessTokenExpiration { get; set; }
        public TimeSpan RefreshTokenExpiration { get; set; }
        public string AccessTokenKey { get; set; }
        public string RefreshTokenKey { get; set; }
        public string IsAccessTokenExpiredHeader { get; set; }
        public string IsRefreshTokenExpiredHeader { get; set; }
    }
}
