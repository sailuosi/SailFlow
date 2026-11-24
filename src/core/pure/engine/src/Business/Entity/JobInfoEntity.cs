using System;
using mooSQL.data;

namespace sailflow.data
{
    /// <summary>
    /// 作业/定时任务定义信息（表 <c>sf_job_detail</c>，人工定名）。
    /// 描述与流程活动绑定的 Job 主题、触发类型与运行状态（定时启动、消息捕获等）。
    /// </summary>
    [SooTable(Name = "sf_job_detail", Caption = "作业信息", Type = DBTableType.Table)]
    public class JobInfoEntity
    {
        /// <summary>
        /// 作业信息主键（列 <c>jb_id</c>）。
        /// </summary>
        [SooColumn(Name = "jb_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string JobInfoId { get; set; }

        /// <summary>
        /// 作业显示名（列 <c>jb_name</c>）。
        /// </summary>
        [SooColumn(Name = "jb_name", Length = 255)]
        public string JobName { get; set; }

        /// <summary>
        /// 作业主题/Topic（列 <c>jb_topic</c>），调度或消息订阅键。
        /// </summary>
        [SooColumn(Name = "jb_topic", Length = 128)]
        public string Topic { get; set; }

        /// <summary>
        /// 关联逻辑流程 Id（列 <c>jb_process_id</c>）。
        /// </summary>
        [SooColumn(Name = "jb_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 流程名称快照（列 <c>jb_proc_name</c>）。
        /// </summary>
        [SooColumn(Name = "jb_proc_name", Length = 255)]
        public string ProcessName { get; set; }

        /// <summary>
        /// 流程版本（列 <c>jb_version</c>）。
        /// </summary>
        [SooColumn(Name = "jb_version", Length = 32)]
        public string Version { get; set; }

        /// <summary>
        /// 关联活动定义 Id（列 <c>jb_activity_id</c>）。
        /// </summary>
        [SooColumn(Name = "jb_activity_id", Length = 64)]
        public string ActivityId { get; set; }

        /// <summary>
        /// 活动名称快照（列 <c>jb_activity_name</c>）。
        /// </summary>
        [SooColumn(Name = "jb_activity_name", Length = 255)]
        public string ActivityName { get; set; }

        /// <summary>
        /// 活动类型描述（列 <c>jb_activity_type</c>），字符串形态便于作业侧展示。
        /// </summary>
        [SooColumn(Name = "jb_activity_type", Length = 64)]
        public string ActivityType { get; set; }

        /// <summary>
        /// 触发类型（列 <c>jb_trigger_type</c>），如 Timer / Message / Conditional 等（与节点 Trigger 配置对应）。
        /// </summary>
        [SooColumn(Name = "jb_trigger_type", Length = 64)]
        public string TriggerType { get; set; }

        /// <summary>
        /// 消息方向（列 <c>jb_msg_direction</c>），见 <see cref="sailflow.utils.MessageDirectionEnum"/>。
        /// Catch=捕获；Throw=抛出；None=默认。
        /// </summary>
        [SooColumn(Name = "jb_msg_direction", Length = 64)]
        public string MessageDirection { get; set; }

        /// <summary>
        /// 作业运行状态（列 <c>jb_job_state</c>），字符串状态码（就绪/运行/完成等，由 Job 模块约定）。
        /// </summary>
        [SooColumn(Name = "jb_job_state", Length = 64)]
        public string JobStatus { get; set; }

        /// <summary>
        /// 创建时间（列 <c>jb_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "jb_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// 创建人 Id（列 <c>jb_crt_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "jb_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }

        /// <summary>
        /// 创建人姓名（列 <c>jb_crt_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "jb_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }

        /// <summary>
        /// 更新时间（列 <c>jb_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "jb_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }

        /// <summary>
        /// 更新人 Id（列 <c>jb_upd_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "jb_upd_user_id", Length = 64)]
        public string UpdatedUserId { get; set; }

        /// <summary>
        /// 更新人姓名（列 <c>jb_upd_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "jb_upd_user_name", Length = 128)]
        public string UpdatedUserName { get; set; }
    }
}
