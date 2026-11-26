using System;
using mooSQL.data;
using mooSQL.data.model;
using sailflow.utils;

namespace sailflow.data
{
    /// <summary>
    /// 任务明细读模型（Select + SooJoin，用法同普通实体）。
    /// 主表 <c>sf_task</c> ⋈ <c>sf_act_instance</c> ⋈ <c>sf_proc_instance</c>，
    /// 并 left join 主活动实例以投影 <see cref="MainActivityState"/>。
    /// 不依赖物理视图；写路径仍用 <see cref="TaskEntity"/> 等表实体。
    /// </summary>
    [SooTable(Name = "sf_task", Alias = "t", Caption = "任务明细读模型", Type = DBTableType.Select)]
    [SooJoin(Type = JoinKind.Inner, To = "sf_act_instance", As = "ai",
        OnA = "t.tk_act_instance_id", OnB = "ai.ai_id")]
    [SooJoin(Type = JoinKind.Inner, To = "sf_proc_instance", As = "pi",
        OnA = "t.tk_proc_instance_id", OnB = "pi.pi_id")]
    [SooJoin(Type = JoinKind.Left, To = "sf_act_instance", As = "mai",
        OnA = "ai.ai_m_act_instance_id", OnB = "mai.ai_id")]
    public class TaskViewEntity
    {
        [SooColumn(Name = "tk_id", Kind = FieldKind.Base, IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string TaskId { get; set; }

        [SooColumn(Name = "tk_biz_channel", Kind = FieldKind.Base)]
        public string BizChannel { get; set; }

        [SooColumn(Name = "tk_biz_pk", Kind = FieldKind.Base)]
        public string BizPK { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "pi", SrcField = "pi_biz_code",
            Name = "pi_biz_code", Alias = "pi_biz_code")]
        public string BizCode { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "pi", SrcField = "pi_title",
            Name = "pi_title", Alias = "pi_title")]
        public string Title { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "pi", SrcField = "pi_name",
            Name = "pi_name", Alias = "pi_name")]
        public string ProcessName { get; set; }

        [SooColumn(Name = "tk_proc_instance_id", Kind = FieldKind.Base, Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "pi", SrcField = "pi_process_id",
            Name = "pi_process_id", Alias = "pi_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "pi", SrcField = "pi_version",
            Name = "pi_version", Alias = "pi_version")]
        public string Version { get; set; }

        [SooColumn(Name = "tk_activity_id", Kind = FieldKind.Base)]
        public string ActivityId { get; set; }

        [SooColumn(Name = "tk_act_instance_id", Kind = FieldKind.Base, Length = 64, Tag = "sail.id", IsFK = true)]
        public string ActivityInstanceId { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "ai", SrcField = "ai_activity_name",
            Name = "ai_activity_name", Alias = "ai_activity_name")]
        public string ActivityName { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "ai", SrcField = "ai_activity_code",
            Name = "ai_activity_code", Alias = "ai_activity_code")]
        public string ActivityCode { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "ai", SrcField = "ai_activity_type",
            Name = "ai_activity_type", Alias = "ai_activity_type")]
        public short? ActivityType { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "ai", SrcField = "ai_work_item_type",
            Name = "ai_work_item_type", Alias = "ai_work_item_type")]
        public short? WorkItemType { get; set; }

        [SooColumn(Name = "tk_task_type", Kind = FieldKind.Base)]
        public short? TaskType { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "pi", SrcField = "pi_sub_proc_type",
            Name = "pi_sub_proc_type", Alias = "pi_sub_proc_type")]
        public short? SubProcessType { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "pi", SrcField = "pi_sub_process_id",
            Name = "pi_sub_process_id", Alias = "pi_sub_process_id", Length = 64, Tag = "sail.id")]
        public string SubProcessDefId { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "pi", SrcField = "pi_sub_proc_id",
            Name = "pi_sub_proc_id", Alias = "pi_sub_proc_id", Length = 64, Tag = "sail.id")]
        public string SubProcessId { get; set; }

        [SooColumn(Name = "tk_entr_task_id", Kind = FieldKind.Base, Length = 64, Tag = "sail.id", IsFK = true)]
        public string EntrustedTaskId { get; set; }

        [SooColumn(Name = "tk_weight", Kind = FieldKind.Base)]
        public decimal? Weight { get; set; }

        [SooColumn(Name = "tk_proxy_sign_id", Kind = FieldKind.Base, Length = 64)]
        public string ProxySignId { get; set; }

        [SooColumn(Name = "tk_end_type", Kind = FieldKind.Base, Length = 64)]
        public string EndType { get; set; }

        [SooColumn(Name = "tk_end_text", Kind = FieldKind.Base, Length = 255)]
        public string EndText { get; set; }

        [SooColumn(Name = "tk_assignee_role", Kind = FieldKind.Base)]
        public int? AssigneeRole { get; set; }

        [SooColumn(Name = "tk_consult_src_task_id", Kind = FieldKind.Base, Length = 64, Tag = "sail.id", IsFK = true)]
        public string ConsultSrcTaskId { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "ai", SrcField = "ai_m_act_instance_id",
            Name = "ai_m_act_instance_id", Alias = "ai_m_act_instance_id", Length = 64, Tag = "sail.id")]
        public string MainActInstanceId { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "ai", SrcField = "ai_cmp_order",
            Name = "ai_cmp_order", Alias = "ai_cmp_order")]
        public float? CompleteOrder { get; set; }

        [SooColumn(Name = "tk_sign_kind", Kind = FieldKind.Base)]
        public int? SignKind { get; set; }

        [SooColumn(Name = "tk_sign_id", Kind = FieldKind.Base)]
        public string SignId { get; set; }

        [SooColumn(Name = "tk_sign_name", Kind = FieldKind.Base)]
        public string SignName { get; set; }

        [SooColumn(Name = "tk_idx", Kind = FieldKind.Base)]
        public int? Idx { get; set; }

        [SooColumn(Name = "tk_group_id", Kind = FieldKind.Base)]
        public string GroupId { get; set; }

        [SooColumn(Name = "tk_group_name", Kind = FieldKind.Base)]
        public string GroupName { get; set; }

        [SooColumn(Name = "tk_group_role", Kind = FieldKind.Base)]
        public string GroupRole { get; set; }

        [SooColumn(Name = "tk_unit_id", Kind = FieldKind.Base)]
        public string UnitId { get; set; }

        [SooColumn(Name = "tk_unit_code", Kind = FieldKind.Base)]
        public string UnitCode { get; set; }

        [SooColumn(Name = "tk_unit_name", Kind = FieldKind.Base)]
        public string UnitName { get; set; }

        [SooColumn(Name = "tk_is_email_sent", Kind = FieldKind.Base)]
        public byte? IsEMailSent { get; set; }

        [SooColumn(Name = "tk_is_read", Kind = FieldKind.Base)]
        public byte? IsRead { get; set; }

        [SooColumn(Name = "tk_read_at", Kind = FieldKind.Base)]
        public DateTime? ReadAt { get; set; }

        [SooColumn(Name = "tk_msg", Kind = FieldKind.Base)]
        public string Msg { get; set; }

        [SooColumn(Name = "tk_row_para", Kind = FieldKind.Base)]
        public string RowPara { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "ai", SrcField = "ai_aprv_state",
            Name = "ai_aprv_state", Alias = "ai_aprv_state")]
        public short? ApprovalStatus { get; set; }

        [SooColumn(Name = "tk_crt_time", Kind = FieldKind.Base)]
        public DateTime? CreatedDateTime { get; set; }

        [SooColumn(Name = "tk_send_at", Kind = FieldKind.Base)]
        public DateTime? SendAt { get; set; }

        [SooColumn(Name = "tk_got_at", Kind = FieldKind.Base)]
        public DateTime? GotAt { get; set; }

        [SooColumn(Name = "tk_active_at", Kind = FieldKind.Base)]
        public DateTime? ActiveAt { get; set; }

        [SooColumn(Name = "tk_due_at", Kind = FieldKind.Base)]
        public DateTime? DueAt { get; set; }

        [SooColumn(Name = "tk_upd_time", Kind = FieldKind.Base)]
        public DateTime? UpdatedDateTime { get; set; }

        [SooColumn(Name = "tk_completed_at", Kind = FieldKind.Base)]
        public DateTime? CompletedAt { get; set; }

        [SooColumn(Name = "tk_end_user_id", Kind = FieldKind.Base)]
        public string EndedUserId { get; set; }

        [SooColumn(Name = "tk_end_user_name", Kind = FieldKind.Base)]
        public string EndedUserName { get; set; }

        [SooColumn(Name = "tk_task_state", Kind = FieldKind.Base)]
        public short? TaskState { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "ai", SrcField = "ai_activity_state",
            Name = "ai_activity_state", Alias = "ai_activity_state")]
        public short? ActivityState { get; set; }

        /// <summary>会签主活动实例状态（mai.ai_activity_state）。</summary>
        [SooColumn(Kind = FieldKind.Join, SrcTable = "mai", SrcField = "ai_activity_state",
            Name = "ai_mn_activity_state", Alias = "ai_mn_activity_state")]
        public short? MainActivityState { get; set; }

        [SooColumn(Name = "tk_rec_invalid", Kind = FieldKind.Base)]
        public byte? RecordStatusInvalid { get; set; }

        [SooColumn(Kind = FieldKind.Join, SrcTable = "pi", SrcField = "pi_process_state",
            Name = "pi_process_state", Alias = "pi_process_state")]
        public short? ProcessState { get; set; }
    }
}
