using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.utils;
using mooSQL.data;

namespace sailflow.data
{
    /// <summary>
    /// 任务/待办实体（表 <c>sf_task</c>）。
    /// 活动实例下按办理对象拆分的工作项；含委托、沟通参与者、发送/下达/激活与个人超期。
    /// </summary>
    [SooTable(Name = "sf_task", Caption = "任务", Type = DBTableType.Table)]
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
        /// 票权（列 <c>tk_weight</c>）；默认 1。Leave EffectiveWeight 用。
        /// </summary>
        [SooColumn(Name = "tk_weight")]
        public decimal? Weight { get; set; }

        /// <summary>
        /// 委托代批所代理的根办理人 SignId（列 <c>tk_proxy_sign_id</c>）；空=根席。
        /// </summary>
        [SooColumn(Name = "tk_proxy_sign_id", Length = 64)]
        public string ProxySignId { get; set; }

        /// <summary>
        /// 结束类型码（列 <c>tk_end_type</c>），见 <see cref="TaskEndTypes"/>；打开态为空。
        /// </summary>
        [SooColumn(Name = "tk_end_type", Length = 64)]
        public string EndType { get; set; }

        /// <summary>
        /// 办结简约描述（列 <c>tk_end_text</c>）；Host 本地化写入；≠ Msg 审批意见。
        /// </summary>
        [SooColumn(Name = "tk_end_text", Length = 255)]
        public string EndText { get; set; }

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
        /// 办理对象类型（列 <c>tk_sign_kind</c>），见 <see cref="SignKindEnum"/>。
        /// null/0=User 人；1=Dept；2=Role；3=Group；4=Custom。
        /// </summary>
        [SooColumn(Name = "tk_sign_kind")]
        public int? SignKind { get; set; }

        /// <summary>
        /// 办理对象引用 Id（列 <c>tk_sign_id</c>）。
        /// Kind=人时为用户 Id；非人时为 Dept/Role/Group/Custom 的平台引用。
        /// </summary>
        [SooColumn(Name = "tk_sign_id", Length = 64)]
        public string SignId { get; set; }

        /// <summary>
        /// 办理对象显示名（列 <c>tk_sign_name</c>）。
        /// </summary>
        [SooColumn(Name = "tk_sign_name", Length = 128)]
        public string SignName { get; set; }

        /// <summary>
        /// 会签/队列办理序号（列 <c>tk_idx</c>）；默认 0。非紧急程度（缓急见 <see cref="Hurry"/>）。
        /// </summary>
        [SooColumn(Name = "tk_idx")]
        public int? Idx { get; set; }

        /// <summary>
        /// 办理缓急（列 <c>tk_hurry</c>）：0无 1平急 2加急 3特急 4特提；默认 0。
        /// </summary>
        [SooColumn(Name = "tk_hurry")]
        public int? Hurry { get; set; }

        /// <summary>
        /// 提醒内容（列 <c>tk_tips</c>）；办理人打开待办可见；最长 500。
        /// </summary>
        [SooColumn(Name = "tk_tips", Length = 500)]
        public string Tips { get; set; }

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
        /// 是否已阅（列 <c>tk_is_read</c>）：0/null=未阅；1=已阅。
        /// </summary>
        [SooColumn(Name = "tk_is_read")]
        public byte? IsRead { get; set; }

        /// <summary>
        /// 首次已阅时刻（列 <c>tk_read_at</c>）。
        /// </summary>
        [SooColumn(Name = "tk_read_at")]
        public DateTime? ReadAt { get; set; }

        /// <summary>
        /// 审批/办理意见（列 <c>tk_msg</c>）。
        /// </summary>
        [SooColumn(Name = "tk_msg", Length = 500)]
        public string Msg { get; set; }

        /// <summary>
        /// 行级扩展参数（列 <c>tk_row_para</c>，varchar 1000）；对齐 ddflow RowPara。
        /// </summary>
        [SooColumn(Name = "tk_row_para", Length = 1000)]
        public string RowPara { get; set; }

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
        /// 发送时间（列 <c>tk_send_at</c>）：待办发出去的时刻；再发送只更新本字段。
        /// </summary>
        [SooColumn(Name = "tk_send_at")]
        public DateTime? SendAt { get; set; }

        /// <summary>
        /// 首次下达时间（列 <c>tk_got_at</c>）；本行第一次下达写入，之后不覆盖。
        /// </summary>
        [SooColumn(Name = "tk_got_at")]
        public DateTime? GotAt { get; set; }

        /// <summary>
        /// 激活可办时间（列 <c>tk_active_at</c>）。
        /// 串行会签未轮到时为 null；轮到后写入，方可办理。
        /// </summary>
        [SooColumn(Name = "tk_active_at")]
        public DateTime? ActiveAt { get; set; }

        /// <summary>
        /// 个人应完成时间（列 <c>tk_due_at</c>）。
        /// 未激活为 null；激活后按时限策略计算。
        /// </summary>
        [SooColumn(Name = "tk_due_at")]
        public DateTime? DueAt { get; set; }

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
        /// 办结时间（列 <c>tk_completed_at</c>）。
        /// </summary>
        [SooColumn(Name = "tk_completed_at")]
        public DateTime? CompletedAt { get; set; }

        /// <summary>
        /// 记录作废标记（列 <c>tk_rec_invalid</c>）：0=有效；1=软删（废弃实例等联动）。
        /// </summary>
        [SooColumn(Name = "tk_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}
