#nullable disable

namespace TodoList.Shared.Models
{
    public class Response
    {
        public bool IsSuccess { get; set; } = false;
        public string Message { get; set; }
    }

    public class Response<T> : Response
    {
        public T Data { get; set; }
    }
}
