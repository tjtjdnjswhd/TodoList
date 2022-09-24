#nullable disable

namespace TodoList.Shared.Models
{
    public class Response
    {
        public bool IsSuccess { get; set; } = false;
        public EErrorCode ErrorCode { get; set; } = EErrorCode.Default;
    }

    public sealed class Response<T> : Response
    {
        public T Data { get; set; }
    }
}
