using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TexellCheckInMobile.Helpers
{
    public static class ExceptionHelper
    {
        /// <summary>
        /// This function allows us to take actions whenever an exception is caught elsewhere.
        /// It should be manually called in every function's try-catch block.
        /// The current functionality is to send an email with the exception details to
        /// the email addresses stored in the configuration setting "ExceptionEmails".
        /// </summary>
        /// <param name="ex">The exception that was caught</param>
        public static void Throw(Exception ex)
        {
            // Build a string containing the HTML body of the email
            string body = "";
            body += "An exception occurred in your PowerBranch Mobile deployment.<br />";

            // Add an HTML table with the exception information
            body += "<table>";
            body += "<tr><th>Message</th><th>Source</th><th>Call Stack</th></tr>";

            // We loop through the entire chain of exceptions, if there were multiple
            while (ex != null)
            {
                // If the exception provided a stack trace (the functions it came from), add rows for them
                if (ex.StackTrace != null)
                {
                    // Split the string containing the stack trace into an array with each individual line in the trace
                    string[] traces = ex.StackTrace.Split(new string[] { " at", "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).Reverse().ToArray();
                    // Add the first HTML row which also contains the exception message and source file
                    body += "<tr>";
                    body += "<td valign=\"top\" rowspan=\"" + traces.Length + "\">" + ex.Message + "</td>";
                    body += "<td valign=\"top\" rowspan=\"" + traces.Length + "\">" + ex.Source + "</td>";
                    body += "<td>" + traces[0] + "</td>";
                    body += "</tr>";
                    // Add the rows for the rest of the lines in the stack trace
                    for (int i = 1; i < traces.Length; i++)
                    {
                        body += "<tr><td>" + traces[i] + "</td></tr>";
                    }
                }
                else
                {
                    // No stack trace provided, so just add one row with the exception message and source file
                    body += "<tr>";
                    body += "<td valign=\"top\">" + ex.Message + "</td>";
                    body += "<td valign=\"top\">" + ex.Source + "</td>";
                    body += "<td></td>";
                    body += "</tr>";
                }
                // Loop through the next exception in the exception chain; if null, the loop will not continue
                ex = ex.InnerException;
            }
            // Add the closing table tag
            body += "</table>";

            // Attempt to send the exception emails
            // We wrap it in a try-catch to prevent SendEmailHelper.SendEmail() from calling this function again, creating an endless loop
            try
            {
                // Get the comma-separated list of email addresses to send to from the ExceptionEmails appsetting
                string[] emailTos = Startup.ConfigurationInstance.GetSection("AppSettings")["ExceptionEmails"].Split(',');
                // Loop through the email addresses and send the prepared HTML message body to each
                foreach (string emailTo in emailTos)
                {
                    SendEmailHelper.SendEmail("Admin@texell.org", emailTo, "[PowerBranch] Exception Report", body);
                }
            }
            catch (Exception) { }


            // The following code is only compiled as part of a debug build, so only when testing locally, not in production
#if DEBUG
            // Actually throws the exception as if it hadn't been handled
            throw ex;
#endif
        }
    }
}
