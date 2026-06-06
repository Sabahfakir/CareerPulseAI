using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobFinderApp.Services
{
    public interface IEmailService
    {
        void SendEmail(string toEmail, string subject, string body);
    }
}
