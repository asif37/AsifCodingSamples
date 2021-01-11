using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;


namespace TexellCheckInWebJob.Helpers
{
    public static class SendEmailHelper
    {
        public static void SendEmail(string recipient, string subject, string msg, byte[] attachData = null, string attachName = "")
        {
            var mail = new SendGridMessage();
            mail.SetFrom(new EmailAddress("admin@texell.org", "PowerBranch"));
            mail.AddTo(recipient);
            mail.SetSubject(subject);
            mail.AddContent(MimeType.Html, msg);
            if (attachData != null)
            {
                mail.AddAttachment(attachName, Convert.ToBase64String(attachData), "application/pdf");
            }
            var client = new SendGridClient(ConfigurationManager.AppSettings["EmailAuthenticationKey"]);
           client.SendEmailAsync(mail);
        }

    }
}
