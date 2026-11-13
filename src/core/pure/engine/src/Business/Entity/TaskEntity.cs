using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Task Entity
    /// 任务实体对象
    /// </summary>
    [SooTable(Name = "wf_task", Caption = "任务", Type = DBTableType.Table)]
    public class TaskEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string TaskId { get; set; }
        [SooColumn(Name = "activity_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ActivityInstanceId { get; set; }
        [SooColumn(Name = "process_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }
        [SooColumn(Name = "app_name", Length = 128)]
        public string AppName { get; set; }
        [SooColumn(Name = "app_instance_id", Length = 128)]
        public string AppInstanceId { get; set; }
        [SooColumn(Name = "process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "activity_id", Length = 64)]
        public string ActivityId { get; set; }
        [SooColumn(Name = "activity_name", Length = 255)]
        public string ActivityName { get; set; }
        [SooColumn(Name = "task_type")]
        public short? TaskType { get; set; }
        [SooColumn(Name = "task_state")]
        public short? TaskState { get; set; }
        [SooColumn(Name = "entrusted_task_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string EntrustedTaskId { get; set; }        //被委托任务Id
        [SooColumn(Name = "assigned_user_id", Length = 64)]
        public string AssignedUserId { get; set; }
        [SooColumn(Name = "assigned_user_name", Length = 128)]
        public string AssignedUserName { get; set; }
        /// <summary>组别标识（办理人 GroupId）。</summary>
        [SooColumn(Name = "group_id", Length = 64)]
        public string GroupId { get; set; }
        [SooColumn(Name = "group_name", Length = 255)]
        public string GroupName { get; set; }
        /// <summary>组内身份（string：Leader/Member…）。</summary>
        [SooColumn(Name = "group_role", Length = 64)]
        public string GroupRole { get; set; }
        [SooColumn(Name = "unit_id", Length = 64)]
        public string UnitId { get; set; }
        [SooColumn(Name = "unit_code", Length = 64)]
        public string UnitCode { get; set; }
        [SooColumn(Name = "unit_name", Length = 255)]
        public string UnitName { get; set; }
        /// <summary>
        /// 业务侧待办邮件发送标记（非引擎主路径语义）。
        /// 取值见 <c>sailflow.biz.query.TaskEMailSentStatusEnum</c>；创建时写 0=未发送，由查询/补发路径回写。
        /// </summary>
        [SooColumn(Name = "is_email_sent")]
        public byte? IsEMailSent { get; set; }
        [SooColumn(Name = "created_user_id", Length = 64)]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "created_user_name", Length = 128)]
        public string CreatedUserName { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        /// <summary>待办激活（可办）时间；串行未轮到为 null。</summary>
        [SooColumn(Name = "activated_datetime")]
        public DateTime? ActivatedDateTime { get; set; }
        /// <summary>个人应完成时间；未激活为 null。</summary>
        [SooColumn(Name = "overdue_datetime")]
        public DateTime? OverdueDateTime { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
        [SooColumn(Name = "updated_user_id", Length = 64)]
        public string UpdatedUserId { get; set; }
        [SooColumn(Name = "updated_user_name", Length = 128)]
        public string UpdatedUserName { get; set; }
        [SooColumn(Name = "ended_user_id", Length = 64)]
        public string EndedUserId { get; set; }
        [SooColumn(Name = "ended_user_name", Length = 128)]
        public string EndedUserName { get; set; }
        [SooColumn(Name = "ended_datetime")]
        public DateTime? EndedDateTime { get; set; }
        [SooColumn(Name = "record_status_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}
