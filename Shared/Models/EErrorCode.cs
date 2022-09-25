namespace TodoList.Shared.Models
{
    [Flags]
    public enum EErrorCode
    {
        Default = 0,
        AccessTokenNotMatch = 1,
        WrongAccessToken = 2,
        RefreshTokenExpired = 4,
        EmailNotExist = 8,
        WrongPassword = 16,
        EmailDuplicate = 32,
        NameDuplicate = 64,
        EmailVerifyFail = 128,
        TodoItemNotFound = 256,
        EmailNotVerified = 512
    }
}
