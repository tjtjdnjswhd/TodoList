using Microsoft.Extensions.Logging;
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
        private readonly MailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<MailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(MailRequest request)
        {
            MailMessage mailMessage = new(_settings.From, request.To, request.Subject, request.Body)
            {
                IsBodyHtml = request.IsBodyHtml,
                BodyEncoding = request.BodyEncoding
            };

            using SmtpClient smtpClient = new(_settings.Host, _settings.Port);
            smtpClient.UseDefaultCredentials = false;
            smtpClient.Credentials = new NetworkCredential(_settings.From, _settings.Password);
            smtpClient.EnableSsl = _settings.EnableSsl;

            _logger.LogDebug("Start send email. settings: {@settings}, request: {@request}", _settings, request);
            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Mail send fail. settings: {@setting}, request: {@request}", _settings, request);
                throw;
            }
            _logger.LogInformation("Mail send success. settings: {@setting}, request: {@request}", _settings, request);
        }
    }
}
