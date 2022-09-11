using System.Security.Cryptography.X509Certificates;

using TodoList.Shared.Models;

namespace TodoList.Shared.Svcs.Interfaces
{
    public interface IEmailService
    {
        public Task SendEmailAsync(MailRequest request);
    }
}
