using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 任务/待办实体（表 <c>wf_task</c>）。
    /// 活动实例下按办理人拆分的个人工作项；含委托、沟通参与者、激活与个人超期。
    /// </summary>
    [SooTable(Name = "wf_task", Caption = "任务", Type = DBTableType.Table)]
    public class TaskEntity
    {
        /// <summary>
        /// 任务主键（列 <c>tk_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string TaskId { get; set; }

        /// <summary>
        /// 所属活动实例 Id（列 <c>tk_act_instance_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ActivityInstanceId { get; set; }

        /// <summary>
        /// 所属流程实例 Id（列 <c>tk_proc_instance_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        /// <summary>
        /// 业务通道冗余（列 <c>tk_biz_channel</c>）。
        /// </summary>
        [SooColumn(Name = "tk_biz_channel", Length = 128)]
        public string BizChannel { get; set; }

        /// <summary>
        /// 业务主键冗余（列 <c>tk_biz_pk</c>）。
        /// </summary>
        [SooColumn(Name = "tk_biz_pk", Length = 128)]
        public string BizPK { get; set; }

        /// <summary>
        /// 逻辑流程 Id（列 <c>tk_process_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// BPMN 活动定义 Id（列 <c>tk_activity_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_activity_id", Length = 64)]
        public string ActivityId { get; set; }

        /// <summary>
        /// 活动名称快照（列 <c>tk_activity_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_activity_name", Length = 255)]
        public string ActivityName { get; set; }

        /// <summary>
        /// 任务类型（列 <c>tk_task_type</c>），见内部 <c>TaskTypeEnum</c>。
        /// 1=Manual 人工；2=Automantic 自动化。
        /// </summary>
        [SooColumn(Name = "tk_task_type")]
        public short? TaskType { get; set; }

        /// <summary>
        /// 任务状态（列 <c>tk_task_state</c>），见 <see cref="TaskStateEnum"/>。
        /// 1=Waiting 等待；2=Reading 办理中；4=Completed；8=Withdrawed；9=SendBacked；
        /// 32=Closed（多人可办时他人办完关闭）；48=Canceled。
        /// </summary>
        [SooColumn(Name = "tk_task_state")]
        public short? TaskState { get; set; }

        /// <summary>
        /// 被委托的原任务 Id（列 <c>tk_entr_task_id</c>；角色 entr）。
        /// 委托产生的新待办指向原任务行；非委托任务为空。
        /// </summary>
        [SooColumn(Name = "tk_entr_task_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string EntrustedTaskId { get; set; }

        /// <summary>
        /// 办理人角色（列 <c>tk_assignee_role</c>），见 <see cref="AssigneeRoleEnum"/>。
        /// null/0=Approver 常规审批办理人；1=Participant 沟通参与者（仅阅办，无审批推进权）。
        /// </summary>
        [SooColumn(Name = "tk_assignee_role")]
        public int? AssigneeRole { get; set; }

        /// <summary>
        /// 沟通参与者待办指向的源审批 TaskId（列 <c>tk_consult_src_task_id</c>）。
        /// 锁与收回（ConsultPending）按此聚合；仅 Participant 待办填写。
        /// </summary>
        [SooColumn(Name = "tk_consult_src_task_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ConsultSrcTaskId { get; set; }

        /// <summary>
        /// 办理人用户 Id（列 <c>tk_asgn_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_asgn_user_id", Length = 64)]
        public string AssignedUserId { get; set; }

        /// <summary>
        /// 办理人用户姓名（列 <c>tk_asgn_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_asgn_user_name", Length = 128)]
        public string AssignedUserName { get; set; }

        /// <summary>
        /// 组别 Id（列 <c>tk_group_id</c>），来自 Performer.GroupId。
        /// </summary>
        [SooColumn(Name = "tk_group_id", Length = 64)]
        public string GroupId { get; set; }

        /// <summary>
        /// 组别名称（列 <c>tk_group_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_group_name", Length = 255)]
        public string GroupName { get; set; }

        /// <summary>
        /// 组内身份（列 <c>tk_group_role</c>，字符串：Leader/Member…）。
        /// </summary>
        [SooColumn(Name = "tk_group_role", Length = 64)]
        public string GroupRole { get; set; }

        /// <summary>
        /// 办理单位 Id（列 <c>tk_unit_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_unit_id", Length = 64)]
        public string UnitId { get; set; }

        /// <summary>
        /// 办理单位编码（列 <c>tk_unit_code</c>）。
        /// </summary>
        [SooColumn(Name = "tk_unit_code", Length = 64)]
        public string UnitCode { get; set; }

        /// <summary>
        /// 办理单位名称（列 <c>tk_unit_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_unit_name", Length = 255)]
        public string UnitName { get; set; }

        /// <summary>
        /// 业务侧待办邮件发送标记（列 <c>tk_is_email_sent</c>；非引擎主路径语义）。
        /// 取值见 <c>sailflow.biz.query.TaskEMailSentStatusEnum</c>；创建时写 0=未发送，由查询/补发路径回写。
        /// </summary>
        [SooColumn(Name = "tk_is_email_sent")]
        public byte? IsEMailSent { get; set; }

        /// <summary>
        /// 创建人 Id（列 <c>tk_crt_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }

        /// <summary>
        /// 创建人姓名（列 <c>tk_crt_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }

        /// <summary>
        /// 创建时间（列 <c>tk_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "tk_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// 待办激活（可办）时间（列 <c>tk_activated_time</c>）。
        /// 串行会签未轮到时为 null；轮到后写入，方可办理。
        /// </summary>
        [SooColumn(Name = "tk_activated_time")]
        public DateTime? ActivatedDateTime { get; set; }

        /// <summary>
        /// 个人应完成时间（列 <c>tk_overdue_time</c>）。
        /// 未激活为 null；激活后按时限策略计算。
        /// </summary>
        [SooColumn(Name = "tk_overdue_time")]
        public DateTime? OverdueDateTime { get; set; }

        /// <summary>
        /// 更新时间（列 <c>tk_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "tk_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }

        /// <summary>
        /// 更新人 Id（列 <c>tk_upd_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_upd_user_id", Length = 64)]
        public string UpdatedUserId { get; set; }

        /// <summary>
        /// 更新人姓名（列 <c>tk_upd_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_upd_user_name", Length = 128)]
        public string UpdatedUserName { get; set; }

        /// <summary>
        /// 办结人 Id（列 <c>tk_end_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "tk_end_user_id", Length = 64)]
        public string EndedUserId { get; set; }

        /// <summary>
        /// 办结人姓名（列 <c>tk_end_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_end_user_name", Length = 128)]
        public string EndedUserName { get; set; }

        /// <summary>
        /// 办结时间（列 <c>tk_end_time</c>）。
        /// </summary>
        [SooColumn(Name = "tk_end_time")]
        public DateTime? EndedDateTime { get; set; }

        /// <summary>
        /// 记录作废标记（列 <c>tk_rec_invalid</c>）：0=有效；1=软删（废弃实例等联动）。
        /// </summary>
        [SooColumn(Name = "tk_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}
