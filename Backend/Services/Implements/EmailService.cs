using Backend.Services.Interfaces;
using System.Net;
using System.Net.Mail;

namespace Backend.Services.Implements
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
            var portString = _configuration["EmailSettings:Port"] ?? "587";
            int smptPort = int.TryParse(portString, out var parsedPort) ? parsedPort : 587;
            var senderEmail = _configuration["EmailSettings:SenderEmail"];
            var senderPassword = _configuration["EmailSettings:SenderPassword"];

            var isPlaceholder = string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword)
                || (senderEmail?.Contains("YOUR_GMAIL_HERE", StringComparison.OrdinalIgnoreCase) == true)
                || (senderPassword?.Contains("YOUR_APP_PASSWORD_HERE", StringComparison.OrdinalIgnoreCase) == true);

            if (isPlaceholder)
            {
                Console.WriteLine($"[DEV MODE] Email to {toEmail}: {subject}");
                Console.WriteLine($"Message: {htmlMessage}");
                return;
            }

            var message = new MailMessage
            {
                From = new MailAddress(senderEmail!, "Math Test Creator"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            using var client = new SmtpClient(smtpServer, smptPort)
            {
                Credentials = new NetworkCredential(senderEmail, senderPassword),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
        }
    }
}
