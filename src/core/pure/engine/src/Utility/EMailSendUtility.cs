using System;
using System.ComponentModel;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Common;
using saiflow.engine.Config;
using saiflow.hub.Localize;
using saiflow.engine.Client;

namespace saiflow.engine.Utility
{
    /// <summary>
    /// EMail send utility — SMTP configuration is read from JobAdminConfig,
    /// which can be initialised via JobAdminConfig.Configure(IConfiguration).
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
            var smtp = new SmtpClient
            {
                Credentials = new System.Net.NetworkCredential(
                    JobAdminConfig.SmtpAccount,
                    JobAdminConfig.SmtpPassword),
                Host      = JobAdminConfig.SmtpHost,
                Port      = JobAdminConfig.SmtpPort,
                EnableSsl = JobAdminConfig.SmtpSsl,
            };

            var mail = new MailMessage
            {
                Subject         = title,
                Body            = body,
                From            = new MailAddress(JobAdminConfig.SmtpAccount),
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
