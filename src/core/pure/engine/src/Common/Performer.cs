using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace sailflow.engine.Common
{
    /// <summary>
    /// The performer object of the task
    /// 任务的执行者对象
    /// </summary>
    public class Performer
    {
        /// <summary>
        /// Construct mehtod
        /// 构造函数
        /// </summary>
        public Performer(string userId, string userName)
        {
            UserId = userId;
            UserName = userName;
        }

        /// <summary>
        /// User Id
        /// 用户Id
        /// </summary>
        public string UserId
        {
            get;
            set;
        }

        /// <summary>
        /// User Name
        /// 用户名称
        /// </summary>
        public string UserName
        {
            get;
            set;
        }

        /// <summary>
        /// 办理组别标识（Leave 用；一维办理可空）。勿与企业单位 Unit* 混用。
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>组别展示名（可选）。</summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 组内身份（string 码，如 Leader/Member；勿用 bool）。未填时建议按 Member 解释。
        /// </summary>
        public string GroupRole { get; set; }

        /// <summary>企业单位键（string，可空；展示/筛选；Leave 不读）。</summary>
        public string UnitId { get; set; }

        /// <summary>企业单位编码（string，可空）。</summary>
        public string UnitCode { get; set; }

        /// <summary>企业单位名称（string，可空）。</summary>
        public string UnitName { get; set; }
    }

    /// <summary>
    /// Performer List
    /// 执行者列表类
    /// </summary>
    public class PerformerList : List<Performer>
    {
        public PerformerList()
        {
        }
    }
}
