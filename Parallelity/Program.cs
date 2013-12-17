using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Mail;

using Parallelity.Windows.Forms;

namespace Parallelity
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => UnhandledExceptionHandler((Exception)e.ExceptionObject);
            Application.ThreadException += (sender, e) => UnhandledExceptionHandler(e.Exception);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }

        #warning Replace crash handler email password
        static void UnhandledExceptionHandler(Exception e)
        {
            DialogResult result = MessageBox.Show(
                    "Wystąpił krytyczny błąd programu. Czy chcesz wysłać szczegóły dotyczące wyjątku aplikacji do działu obsługi?",
                    "Błąd",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1);

            if (result != System.Windows.Forms.DialogResult.Yes)
                return;

            new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(
                    "parallelity.application@gmail.com",
                    "<password>"
                )
            }
            .Send(new MailMessage(
                "parallelity.application@gmail.com",
                "parallelity.application@gmail.com",
                "Crash report",
                "Date: " + DateTime.Now.ToString() + "\n" +
                "Exception message: " + e.Message + "\n" +
                "\n" +
                "Stack trace:\n" +
                e.StackTrace
            ));
        }
    }
}
