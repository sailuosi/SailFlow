namespace sailflow.biz.query
{
    /// <summary>
    /// Task Email Sent Status
    /// 邮件发送状态类型（业务侧待办邮件标记，非引擎主路径）
    /// </summary>
    public enum TaskEMailSentStatusEnum
    {
        /// <summary>
        /// Unsent
        /// 未发送
        /// </summary>
        UnSent = 0,

        /// <summary>
        /// Sent
        /// 发送(smtp不保证一定发送成功）
        /// </summary>
        Sent = 1
    }
}
