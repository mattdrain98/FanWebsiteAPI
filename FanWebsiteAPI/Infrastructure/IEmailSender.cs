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
            if (string.IsNullOrWhiteSpace(to))
                throw new ArgumentException("Recipient address is required.", nameof(to));

            var fromText = _config["Email:From"];
            if (string.IsNullOrWhiteSpace(fromText))
                throw new InvalidOperationException("Configuration key 'Email:From' is missing or empty.");

            if (!MailboxAddress.TryParse(fromText, out var fromAddress))
                throw new FormatException($"Invalid email address in configuration 'Email:From': '{fromText}'.");

            if (!MailboxAddress.TryParse(to, out var toAddress))
                throw new ArgumentException($"Invalid recipient email address: '{to}'", nameof(to));

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Dismino", fromAddress.Address));
            message.To.Add(toAddress);
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();

            var host = _config["Email:Host"] ?? throw new InvalidOperationException("Configuration key 'Email:Host' is missing.");
            var portText = _config["Email:Port"];
            if (!int.TryParse(portText, out var port))
                throw new InvalidOperationException("Configuration key 'Email:Port' is missing or not an integer.");
            var username = _config["Email:Username"] ?? throw new InvalidOperationException("Configuration key 'Email:Username' is missing.");
            var password = _config["Email:Password"] ?? throw new InvalidOperationException("Configuration key 'Email:Password' is missing.");

            await smtp.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(username, password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
    }
}
