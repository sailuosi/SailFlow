using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace sailflow.data
{
    /// <summary>
    /// 流程实例实体（表 <c>sf_proc_instance</c>）。
    /// 一次启动对应一行；承载业务袋 biz*、运行态、子流程唤起关系与发起单位。
    /// </summary>
    [SooTable(Name = "sf_proc_instance", Caption = "流程实例", Type = DBTableType.Table)]
    public class ProcessInstanceEntity
    {
        /// <summary>
        /// 流程实例主键（列 <c>pi_id</c>）。
        /// </summary>
        [SooColumn(Name = "pi_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string ProcessInstanceId { get; set; }

        /// <summary>
        /// 逻辑流程 Id（列 <c>pi_process_id</c>），来自定义 <see cref="ProcessEntity.ProcessId"/>。
        /// 嵌套子流程实例可改写为父流程 ProcessId（见 CreateNewProcessInstanceObject）。
        /// </summary>
        [SooColumn(Name = "pi_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 流程名称（列 <c>pi_name</c>）：定义名快照，不是实例标题。
        /// 改定义名不回溯已建实例；展示优先读 <see cref="Title"/>，空时再回落本字段。
        /// </summary>
        [SooColumn(Name = "pi_name", Length = 255)]
        public string ProcessName { get; set; }

        /// <summary>
        /// 流程实例标题（列 <c>pi_title</c>）。
        /// 待办/列表/门户对外展示；由启动入参、创建 Hook 或后续标题模板写入。
        /// </summary>
        [SooColumn(Name = "pi_title", Length = 255)]
        public string Title { get; set; }

        /// <summary>
        /// 启动时所依据的流程版本（列 <c>pi_version</c>）。
        /// </summary>
        [SooColumn(Name = "pi_version", Length = 32)]
        public string Version { get; set; }

        /// <summary>
        /// 业务类型（列 <c>pi_biz_type</c>）。
        /// 启动 BizDataDto / 创建 Hook 注入；引擎不解释具体字典。
        /// </summary>
        [SooColumn(Name = "pi_biz_type", Length = 64)]
        public string BizType { get; set; }

        /// <summary>
        /// 业务通道（列 <c>pi_biz_channel</c>）。
        /// 旧 AppName 语义收口：路由/FormCode/接入方等；Hook 条件 BizChannels 可匹配。
        /// 活动/任务/转移表同步冗余此列便于查询。
        /// </summary>
        [SooColumn(Name = "pi_biz_channel", Length = 128)]
        public string BizChannel { get; set; }

        /// <summary>
        /// 业务主键（列 <c>pi_biz_pk</c>）。
        /// 旧 AppInstanceId：业务单据 Id 等；关联业务表与网关汇聚计算常用键。
        /// </summary>
        [SooColumn(Name = "pi_biz_pk", Length = 128)]
        public string BizPK { get; set; }

        /// <summary>
        /// 业务单据编码（列 <c>pi_biz_code</c>）。
        /// 旧 AppInstanceCode；与节点定义 Activity.BizCode（节点业务编码）不同命名空间。
        /// </summary>
        [SooColumn(Name = "pi_biz_code", Length = 64)]
        public string BizCode { get; set; }

        /// <summary>
        /// 业务名称（列 <c>pi_biz_name</c>），展示用，可与通道/编码不同。
        /// </summary>
        [SooColumn(Name = "pi_biz_name", Length = 128)]
        public string BizName { get; set; }

        /// <summary>
        /// 流程实例运行状态（列 <c>pi_process_state</c>），见 <see cref="sailflow.engine.Common.ProcessStateEnum"/>。
        /// 0=NotStart；1=Ready；2=Running；4=Completed；5=Suspended；6=Canceled；7=Discarded；8=Terminated（如超期自然终止）。
        /// </summary>
        [SooColumn(Name = "pi_process_state")]
        public short? ProcessState { get; set; }

        /// <summary>
        /// 子流程类型（列 <c>pi_sub_proc_type</c>），见 <see cref="sailflow.engine.Common.SubProcessTypeEnum"/>。
        /// 0=None 主流程；1=Nested 嵌入式；2=Referenced 外部引用。
        /// </summary>
        [SooColumn(Name = "pi_sub_proc_type")]
        public short? SubProcessType { get; set; }

        /// <summary>
        /// 外部引用子流程的定义行 Id（列 <c>pi_sub_process_id</c>）。
        /// Referenced 时指向被调流程的 ProcessDefId；Nested 通常不用。
        /// </summary>
        [SooColumn(Name = "pi_sub_process_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string SubProcessDefId { get; set; }

        /// <summary>
        /// 子流程逻辑 ProcessId / 子图标识（列 <c>pi_sub_proc_id</c>），来自 SubProcessNode.SubProcessId。
        /// </summary>
        [SooColumn(Name = "pi_sub_proc_id", Length = 64, Tag = "sail.id")]
        public string SubProcessId { get; set; }

        /// <summary>
        /// 唤起本实例的父活动实例 Id（列 <c>pi_i_act_instance_id</c>；角色 i=invoked）。
        /// 子流程创建时写入父节点活动实例。
        /// </summary>
        [SooColumn(Name = "pi_i_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string InvokedActInstanceId { get; set; }

        /// <summary>
        /// 唤起方活动定义 Id（列 <c>pi_i_activity_id</c>），BPMN 节点 id，非表外键。
        /// </summary>
        [SooColumn(Name = "pi_i_activity_id", Length = 64)]
        public string InvokedActivityId { get; set; }

        /// <summary>
        /// 实例级作业定时器类型（列 <c>pi_job_time_type</c>），见 <see cref="sailflow.engine.Common.JobTimerTypeEnum"/>。
        /// 0=None；1=Timer；2=Conditional；3=EMail；4=Message。
        /// </summary>
        [SooColumn(Name = "pi_job_time_type")]
        public short? JobTimerType { get; set; }

        /// <summary>
        /// 实例级作业定时器状态（列 <c>pi_job_time_state</c>），见 <see cref="sailflow.engine.Common.JobTimerStatusEnum"/>。
        /// 0=None；1=Ready；4=Completed（已关闭）。
        /// </summary>
        [SooColumn(Name = "pi_job_time_state")]
        public short? JobTimerStatus { get; set; }

        /// <summary>
        /// 定时/条件触发表达式（列 <c>pi_trig_expression</c>），配合 JobTimerType 使用。
        /// </summary>
        [SooColumn(Name = "pi_trig_expression", Length = 2000)]
        public string TriggerExpression { get; set; }

        /// <summary>
        /// 实例应完成/超期时点（列 <c>pi_overdue_time</c>）。
        /// 定义 EndType=Timer 时由 EndExpression 计算写入。
        /// </summary>
        [SooColumn(Name = "pi_dueat")]
        public DateTime? DueAt { get; set; }

        /// <summary>
        /// 作业定时器已处理时间（列 <c>pi_timer_trt_time</c>），标记定时逻辑已消费。
        /// </summary>
        [SooColumn(Name = "pi_doneat")]
        public DateTime? JobDoneAt { get; set; }

        /// <summary>
        /// 发起单位 Id（列 <c>pi_start_unit_id</c>）。
        /// 可选；来自 Runner.StartUnitId，空时回落 CompanyId。不覆盖节点任务 Unit*。
        /// </summary>
        [SooColumn(Name = "pi_start_unit_id", Length = 64)]
        public string StartUnitId { get; set; }

        /// <summary>
        /// 发起单位编码（列 <c>pi_start_unit_code</c>）。
        /// </summary>
        [SooColumn(Name = "pi_start_unit_code", Length = 64)]
        public string StartUnitCode { get; set; }

        /// <summary>
        /// 发起单位名称（列 <c>pi_start_unit_name</c>）。
        /// </summary>
        [SooColumn(Name = "pi_start_unit_name", Length = 255)]
        public string StartUnitName { get; set; }

        /// <summary>
        /// 创建时间（列 <c>pi_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "pi_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// 创建人 Id（列 <c>pi_crt_user_id</c>），通常为启动用户。
        /// </summary>
        [SooColumn(Name = "pi_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }

        /// <summary>
        /// 创建人姓名（列 <c>pi_crt_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "pi_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }

        /// <summary>
        /// 最后更新时间（列 <c>pi_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "pi_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }

        /// <summary>
        /// 最后更新人 Id（列 <c>pi_upd_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "pi_upd_user_id", Length = 64)]
        public string UpdatedUserId { get; set; }

        /// <summary>
        /// 最后更新人姓名（列 <c>pi_upd_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "pi_upd_user_name", Length = 128)]
        public string UpdatedUserName { get; set; }

        /// <summary>
        /// 办结时间（列 <c>pi_end_time</c>）。
        /// </summary>
        [SooColumn(Name = "pi_end_time")]
        public DateTime? EndedDateTime { get; set; }

        /// <summary>
        /// 办结人 Id（列 <c>pi_end_user_id</c>）。
        /// </summary>
        [SooColumn(Name = "pi_end_user_id", Length = 64)]
        public string EndedUserId { get; set; }

        /// <summary>
        /// 办结人姓名（列 <c>pi_end_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "pi_end_user_name", Length = 128)]
        public string EndedUserName { get; set; }

        /// <summary>
        /// 记录作废标记（列 <c>pi_rec_invalid</c>）。
        /// 0=有效；1=软删/废弃查询过滤。废弃实例及其任务时置 1。
        /// </summary>
        [SooColumn(Name = "pi_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}
