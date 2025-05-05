using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using FormBuilder.Models;

namespace FormBuilder.Services
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public string FromAddress { get; set; }
        public string FromName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool EnableSsl { get; set; }
    }

    public interface IEmailService
    {
        Task SendRegistrationEmailAsync(ApplicationUser user);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;

        public EmailService(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendRegistrationEmailAsync(ApplicationUser user)
        {
            try
            {
                using (var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort))
                {
                    client.EnableSsl = _settings.EnableSsl;
                    client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

                    var message = new MailMessage
                    {
                        From = new MailAddress(_settings.FromAddress, _settings.FromName),
                        Subject = "Welcome to FormBuilder",
                        Body = $@"
                            <h1>Welcome to FormBuilder!</h1>
                            <p>Thank you for registering with email: {user.Email}</p>
                            <p>You can now create and fill forms on our platform.</p>
                            <p>Best regards,<br/>FormBuilder Team</p>",
                        IsBodyHtml = true
                    };

                    message.To.Add(user.Email);

                    await client.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                // В реальном приложении следует добавить логгирование
                Console.WriteLine($"Error sending email: {ex.Message}");
            }
        }
    }
}