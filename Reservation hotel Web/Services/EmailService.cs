using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;

namespace Reservation_hotel.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient(_configuration["EmailSettings:SMTPServer"])
                {
                    Port = int.Parse(_configuration["EmailSettings:SMTPPort"]),
                    Credentials = new NetworkCredential(
                        _configuration["EmailSettings:SMTPUsername"],
                        _configuration["EmailSettings:SMTPPassword"]
                    ),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["EmailSettings:FromEmail"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                smtpClient.Send(mailMessage); // Attempt to send
            }
            catch (SmtpException smtpEx)
            {
                // Log SMTP-specific errors
                throw new Exception($"SMTP Error: {smtpEx.StatusCode} - {smtpEx.Message}");
            }
            catch (Exception ex)
            {
                // Log general errors
                throw new Exception($"General Error: {ex.Message}");
            }
        }

    }
}
