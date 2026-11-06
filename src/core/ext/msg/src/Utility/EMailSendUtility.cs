using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Manager;

namespace saiflow.msg
{
    /// <summary>
    /// SMTP 发送工具（默认待办邮件通道使用；SMTP 取自 <see cref="TaskMailOptions"/>）。
    /// </summary>
    public class EMailSendUtility
    {
        private const string TaskEmailSendError = "PROCESS TASK EMAIL SEND ERROR";

        public event EventHandler OnEMailSendCompleted;

        private readonly int TaskId;
        private readonly SailClient Sail;
        private readonly TaskMailOptions Options;

        public EMailSendUtility(SailClient client, TaskMailOptions options, int taskId)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            Options = options ?? throw new ArgumentNullException(nameof(options));
            TaskId = taskId;
        }

        /// <summary>
        /// Send an email asynchronously.
        /// </summary>
        public async Task SendEMailAsync(string title, string body, string receiveEmail)
        {
            var smtp = new SmtpClient
            {
                Credentials = new System.Net.NetworkCredential(
                    Options.SmtpAccount,
                    Options.SmtpPassword),
                Host = Options.SmtpHost,
                Port = Options.SmtpPort,
                EnableSsl = Options.SmtpSsl,
            };

            var mail = new MailMessage
            {
                Subject = title,
                Body = body,
                From = new MailAddress(Options.SmtpAccount),
                BodyEncoding = Encoding.UTF8,
                IsBodyHtml = true,
            };
            mail.To.Add(new MailAddress(receiveEmail));

            smtp.SendCompleted += SendCompletedCallback;
            await smtp.SendMailAsync(mail);
        }

        private void SendCompletedCallback(object sender, AsyncCompletedEventArgs e)
        {
            if (sender is SmtpClient callbackClient)
            {
                if (e.Error != null && !string.IsNullOrEmpty(e.Error.Message))
                {
                    var callbackMailMessage = e.UserState as MailMessage;
                    LogManager.RecordLog(Sail, TaskEmailSendError, LogEventType.Error,
                        LogPriority.Normal, callbackMailMessage, e.Error);
                }
                callbackClient.Dispose();
            }

            if (e.UserState is MailMessage msg) msg.Dispose();
            OnEMailSendCompleted?.Invoke(TaskId, null);
        }
    }
}
