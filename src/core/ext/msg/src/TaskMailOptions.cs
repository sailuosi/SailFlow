namespace sailflow.msg
{
    /// <summary>
    /// 默认待办邮件 SMTP 与模板选项（在 <c>UseDefaultTaskMail</c> 时刻成型；不进引擎 <c>SailConfig</c>）。
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

        /// <summary>邮件主题；可用 Use 时刻覆盖。</summary>
        public string Subject { get; set; } = "新待办事项提醒邮件";

        /// <summary>
        /// 邮件 HTML 正文模板。占位符与历史一致：
        /// <c>{0}</c>=AppBaseUrl，<c>{1}</c>=BizChannel，<c>{2}</c>=BizPK。
        /// </summary>
        public string BodyTemplate { get; set; } =
            "<h1>新待办任务提醒</h1><p>您有一条新的待办任务:<a href='{0}/{1}'>应用名称:{2}, 单据Id:{1}</a> 请您及时登录业务系统处理!</p>";
    }
}
