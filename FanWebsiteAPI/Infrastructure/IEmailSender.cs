using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FanWebsiteAPI.Infrastructure
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
    public class EmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public EmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(_config["Email:From"]));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_config["Email:Host"],
                int.Parse(_config["Email:Port"]), SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_config["Email:Username"],
                _config["Email:Password"]);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
