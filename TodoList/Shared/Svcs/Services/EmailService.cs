using Microsoft.Extensions.Options;

using System.Net;
using System.Net.Mail;
using System.Runtime.Versioning;

using TodoList.Shared.Models;
using TodoList.Shared.Settings;
using TodoList.Shared.Svcs.Interfaces;

namespace TodoList.Shared.Svcs.Services
{
    [UnsupportedOSPlatform("browser")]
    public sealed class EmailService : IEmailService
    {
        private readonly MailSetting _setting;

        public EmailService(IOptions<MailSetting> setting)
        {
            _setting = setting.Value;
        }

        public async Task SendEmailAsync(MailRequest request)
        {
            MailMessage mailMessage = new(_setting.From, request.To, request.Subject, request.Body)
            {
                IsBodyHtml = request.IsBodyHtml,
                BodyEncoding = request.BodyEncoding
            };

            using SmtpClient smtpClient = new(_setting.Host, _setting.Port);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(_setting.From, _setting.Password);
            smtpClient.EnableSsl = _setting.EnableSsl;

            await smtpClient.SendMailAsync(mailMessage);
        }
    }
}
