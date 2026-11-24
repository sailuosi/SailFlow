using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace sailflow.data
{
    /// <summary>
    /// 转移轨迹实体（表 <c>sf_track</c>）。
    /// 记录一次从源活动实例到目标活动实例的流转轨迹（含前进、退回、跳转等）。
    /// </summary>
    [SooTable(Name = "sf_track", Caption = "转移轨迹", Type = DBTableType.Table)]
    public class TrackEntity
    {
        /// <summary>
        /// 转移实例主键（列 <c>ti_id</c>）。
        /// </summary>
        [SooColumn(Name = "ti_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string TrackId { get; set; }

        /// <summary>
        /// BPMN 连线/转移定义 Id（列 <c>ti_tran_id</c>），非表外键。
        /// </summary>
        [SooColumn(Name = "ti_tran_id", Length = 64)]
        public string TransitionId { get; set; }

        /// <summary>
        /// 业务通道冗余（列 <c>ti_biz_channel</c>）。
        /// </summary>
        [SooColumn(Name = "ti_biz_channel", Length = 128)]
        public string BizChannel { get; set; }

        /// <summary>
        /// 业务主键冗余（列 <c>ti_biz_pk</c>）。
        /// </summary>
        [SooColumn(Name = "ti_biz_pk", Length = 128)]
        public string BizPK { get; set; }

        /// <summary>
        /// 所属流程实例 Id（列 <c>ti_proc_instance_id</c>）。
        /// </summary>
        [SooColumn(Name = "ti_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }

        /// <summary>
        /// 逻辑流程 Id（列 <c>ti_process_id</c>）。
        /// </summary>
        [SooColumn(Name = "ti_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 转移类型（列 <c>ti_tran_type</c>），见 <see cref="sailflow.engine.Common.TransitionTypeEnum"/>。
        /// 1=Forward 前行；2=Withdrawed 撤销；4=Sendback 退回；8=Reversed 返签；
        /// 14=Backward（后退合集）；16=Loop 自身循环。
        /// </summary>
        [SooColumn(Name = "ti_tran_type")]
        public byte? TransitionType { get; set; }

        /// <summary>
        /// 跳转飞行类型（列 <c>ti_fly_type</c>），见 <see cref="sailflow.engine.Common.TransitionFlyingTypeEnum"/>。
        /// 0=NotFlying 普通连线；1=ForwardFlying 向前跳转；2=BackwardFlying 向后跳转。
        /// </summary>
        [SooColumn(Name = "ti_fly_type")]
        public byte? FlyingType { get; set; }

        /// <summary>
        /// 来源活动实例 Id（列 <c>ti_f_act_instance_id</c>；角色 f=from）。
        /// </summary>
        [SooColumn(Name = "ti_f_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string FromActInstanceId { get; set; }

        /// <summary>
        /// 来源活动定义 Id（列 <c>ti_frm_activity_id</c>）。
        /// </summary>
        [SooColumn(Name = "ti_from_act_id", Length = 64)]
        public string FromActId { get; set; }

        /// <summary>
        /// 来源活动类型（列 <c>ti_frm_activity_type</c>），见 <see cref="sailflow.engine.Common.ActivityTypeEnum"/>。
        /// </summary>
        [SooColumn(Name = "ti_from_act_type")]
        public short? FromActType { get; set; }

        /// <summary>
        /// 来源活动名称快照（列 <c>ti_frm_activity_name</c>）。
        /// </summary>
        [SooColumn(Name = "ti_from_act_name", Length = 255)]
        public string FromActName { get; set; }

        /// <summary>
        /// 去向活动实例 Id（列 <c>ti_t_act_instance_id</c>；角色 t=to）。
        /// </summary>
        [SooColumn(Name = "ti_to_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ToActInstanceId { get; set; }

        /// <summary>
        /// 去向活动定义 Id（列 <c>ti_to_activity_id</c>）。
        /// </summary>
        [SooColumn(Name = "ti_to_act_id", Length = 64)]
        public string ToActId { get; set; }

        /// <summary>
        /// 去向活动类型（列 <c>ti_to_activity_type</c>），见 <see cref="sailflow.engine.Common.ActivityTypeEnum"/>。
        /// </summary>
        [SooColumn(Name = "ti_to_act_type")]
        public short? ToActType { get; set; }

        /// <summary>
        /// 去向活动名称快照（列 <c>ti_to_activity_name</c>）。
        /// </summary>
        [SooColumn(Name = "ti_to_act_name", Length = 255)]
        public string ToActName { get; set; }

        /// <summary>
        /// 连线条件解析结果（列 <c>ti_cond_result</c>），见 <see cref="sailflow.engine.Common.ConditionParseResultEnum"/>。
        /// 0=NotPassed 未通过；1=Passed 满足条件（本转移被选中执行）。
        /// </summary>
        [SooColumn(Name = "ti_cond_result")]
        public byte? ConditionParsedResult { get; set; }

        /// <summary>
        /// 创建人 Id（列 <c>ti_crt_user_id</c>），通常为触发流转的办理人。
        /// </summary>
        [SooColumn(Name = "ti_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }

        /// <summary>
        /// 创建人姓名（列 <c>ti_crt_user_name</c>）。
        /// </summary>
        [SooColumn(Name = "ti_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }

        /// <summary>
        /// 创建时间（列 <c>ti_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "ti_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// 记录作废标记（列 <c>ti_rec_invalid</c>）：0=有效；1=软删。
        /// </summary>
        [SooColumn(Name = "ti_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}
