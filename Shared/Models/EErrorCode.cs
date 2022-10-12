using System.Text.Json.Serialization;

namespace TodoList.Shared.Models
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EErrorCode
    {
        NoError = 0,
        AccessTokenNotMatch = 1,
        WrongAccessToken = 2,
        RefreshTokenExpired = 4,
        EmailNotExist = 8,
        WrongPassword = 16,
        EmailDuplicate = 32,
        UserNameDuplicate = 64,
        EmailVerifyFail = 128,
        TodoItemNotFound = 256,
        EmailNotVerified = 512
    }
}
