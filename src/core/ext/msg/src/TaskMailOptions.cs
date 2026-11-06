namespace saiflow.msg
{
    /// <summary>
    /// 默认待办邮件 SMTP 选项（在 <c>UseDefaultTaskMail</c> 时刻成型；不进引擎 <c>SailConfig</c>）。
    /// </summary>
    public sealed class TaskMailOptions
    {
        public string SmtpHost { get; set; } = "smtp.abc.com";

        public int SmtpPort { get; set; } = 25;

        public string SmtpAccount { get; set; } = "test@abc.com";

        public string SmtpPassword { get; set; } = "123456789";

        public bool SmtpSsl { get; set; }

        /// <summary>邮件正文中的业务站点基址（原 SailConfig.LocalHostWebApp）。</summary>
        public string AppBaseUrl { get; set; } = "http://localhost/sfmvc/";
    }
}
