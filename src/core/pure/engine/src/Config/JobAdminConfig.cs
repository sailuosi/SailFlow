namespace saiflow.engine.Config
{
    /// <summary>
    /// 作业 / 邮件相关默认常量（形状默认值，非运行时配置源）。
    /// 运行值请使用 <see cref="Client.SailConfig"/>，由业务侧注册。
    /// </summary>
    public static class JobAdminConfig
    {
        public const string DefaultLocalHostWebApp = "http://localhost/sfmvc/";
        public const string DefaultSmtpAccount = "test@abc.com";
        public const string DefaultSmtpPassword = "123456789";
        public const string DefaultSmtpHost = "smtp.abc.com";
        public const int DefaultSmtpPort = 25;
    }
}
