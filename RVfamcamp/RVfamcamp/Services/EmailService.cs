using System.Net;
using System.Net.Mail;

namespace RVfamcamp.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendEmail(string toEmail, string subject, string body)
        {
            var username = _config["EmailSettings:Username"];
            var password = _config["EmailSettings:AppPassword"];

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(username, password),
                EnableSsl = true,
            };

            var message = new MailMessage
            {
                From = new MailAddress(username, "RVfamcamp"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            try
            {
                await smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {   
                Console.WriteLine("EMAIL ERROR: " + ex.Message);
            }
        }
    }
}