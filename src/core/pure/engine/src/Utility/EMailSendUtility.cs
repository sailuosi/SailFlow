using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Common;
using saiflow.hub.Localize;
using saiflow.engine.Client;

namespace saiflow.engine.Utility
{
    /// <summary>
    /// EMail send utility — SMTP 取自已注册的 <see cref="SailClient.Config"/>。
    /// </summary>
    public class EMailSendUtility
    {
        public event EventHandler OnEMailSendCompleted;

        private readonly int TaskId;
        private readonly SailClient Sail;

        public EMailSendUtility(SailClient client, int taskId)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            TaskId = taskId;
        }

        /// <summary>
        /// Send an email asynchronously.
        /// </summary>
        public async Task SendEMailAsync(string title, string body, string receiveEmail)
        {
            var cfg = Sail.Config;
            var smtp = new SmtpClient
            {
                Credentials = new System.Net.NetworkCredential(
                    cfg.SmtpAccount,
                    cfg.SmtpPassword),
                Host      = cfg.SmtpHost,
                Port      = cfg.SmtpPort,
                EnableSsl = cfg.SmtpSsl,
            };

            var mail = new MailMessage
            {
                Subject         = title,
                Body            = body,
                From            = new MailAddress(cfg.SmtpAccount),
                BodyEncoding    = Encoding.UTF8,
                IsBodyHtml      = true,
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
                    LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_TASK_EMAIL_ERROR, LogEventType.Error,
                        LogPriority.Normal, callbackMailMessage, e.Error);
                }
                callbackClient.Dispose();
            }

            if (e.UserState is MailMessage msg) msg.Dispose();
            OnEMailSendCompleted?.Invoke(TaskId, null);
        }
    }
}
