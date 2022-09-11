namespace TodoList.Shared.Svcs.Interfaces
{
    public interface IVerifyCodeService
    {
        public string GetVerifyCode(int length);
        public Task SetVerifyCodeAsync(string key, string code);
        public bool IsVerifyCodeMatch(string key, string code);
        public Task RemoveVerifyCodeAsync(string key);
    }
}
