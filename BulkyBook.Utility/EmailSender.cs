using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Identity.UI.Services;
using MimeKit;

namespace BulkyBook.Utility
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var emailToSent = new MimeMessage();
            emailToSent.From.Add(MailboxAddress.Parse("adnanniloy1313@gmail.com"));
            emailToSent.To.Add(MailboxAddress.Parse(email));
            emailToSent.Subject = subject;
            emailToSent.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = htmlMessage };

            //send email
            using (var emailClient = new SmtpClient())
            {
                emailClient.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
                emailClient.Authenticate("adnanniloy1313@gmail.com", "lfrjvvqgepcdsfkf");
                emailClient.Send(emailToSent);
                emailClient.Disconnect(true);
            }

            return Task.CompletedTask;
        }
    }
}
