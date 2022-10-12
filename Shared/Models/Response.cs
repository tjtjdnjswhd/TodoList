#nullable disable

namespace TodoList.Shared.Models
{
    public class Response
    {
        public Response(EErrorCode errorCode)
        {
            ErrorCode = errorCode;
        }

        public bool IsSuccess => ErrorCode == EErrorCode.NoError;
        public EErrorCode ErrorCode { get; }
    }

    public sealed class Response<T> : Response
    {
        public Response(EErrorCode errorCode, T data) : base(errorCode)
        {
            Data = data;
        }

        public T Data { get; }
    }
}
