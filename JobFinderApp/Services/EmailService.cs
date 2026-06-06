using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.Net.Mail;

namespace JobFinderApp.Services
{
    public class EmailService : IEmailService
    {
        public void SendEmail(string toEmail, string subject, string body)
        {
            var fromEmail = "fakirsabah45@gmail.com";
            var password = "dbjr bilk ends iyzg"; // Use App Password (Important)

            MailMessage message = new MailMessage();
            message.From = new MailAddress(fromEmail);
            message.Subject = subject;
            message.Body = body;
            message.To.Add(toEmail);
            message.IsBodyHtml = true;

            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.Credentials = new NetworkCredential(fromEmail, password);
            smtp.EnableSsl = true;

            smtp.Send(message);
        }
    }
}