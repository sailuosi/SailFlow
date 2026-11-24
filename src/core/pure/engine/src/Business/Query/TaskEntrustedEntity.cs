using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sailflow.data
{
    /// <summary>
    /// 任务委托入参（非落库）。
    /// 将原待办委托给他人办理；新任务行的 <c>EntrustedTaskId</c> 指向本 TaskId。
    /// </summary>
    public class TaskEntrustedEntity
    {
        /// <summary>
        /// 被委托的原任务 Id。
        /// </summary>
        public string TaskId { get; set; }

        /// <summary>
        /// 当前操作人（委托人）用户 Id。
        /// </summary>
        public string RunnerId { get; set; }

        /// <summary>
        /// 当前操作人（委托人）姓名。
        /// </summary>
        public string RunnerName { get; set; }

        /// <summary>
        /// 受托人用户 Id（新待办 SignId）。
        /// </summary>
        public string EntrustToUserId { get; set; }

        /// <summary>
        /// 受托人姓名。
        /// </summary>
        public string EntrustToUserName { get; set; }
    }
}
