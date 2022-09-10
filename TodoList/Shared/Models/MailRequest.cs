#nullable disable

using System.Text;

namespace TodoList.Shared.Models
{
    public sealed class MailRequest
    {
        public string To { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public bool IsBodyHtml { get; set; }
        public Encoding BodyEncoding { get; set; }
    }
}
