#nullable disable

using System.Text;

namespace TodoList.Shared.Models
{
    public class MailRequest
    {
        public MailRequest(string to, string subject, string body, bool isBodyHtml, Encoding bodyEncoding)
        {
            To = to;
            Subject = subject;
            Body = body;
            IsBodyHtml = isBodyHtml;
            BodyEncoding = bodyEncoding;
        }

        public string To { get; }
        public string Subject { get; }
        public string Body { get; }
        public bool IsBodyHtml { get; }
        public Encoding BodyEncoding { get; }
    }
}
