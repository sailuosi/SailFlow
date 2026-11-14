using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Transition Instance Entity
    /// 节点转移类
    /// </summary>
    [SooTable(Name = "wf_tran_instance", Caption = "转移实例", Type = DBTableType.Table)]
    public class TransitionInstanceEntity
    {
        [SooColumn(Name = "ti_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string TransitionInstanceId { get; set; }
        [SooColumn(Name = "ti_tran_id", Length = 64)]
        public string TransitionId { get; set; }
        [SooColumn(Name = "ti_app_name", Length = 128)]
        public string AppName { get; set; }
        [SooColumn(Name = "ti_app_instance_id", Length = 128)]
        public string AppInstanceId { get; set; }
        [SooColumn(Name = "ti_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }
        [SooColumn(Name = "ti_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "ti_tran_type")]
        public byte? TransitionType { get; set; }
        [SooColumn(Name = "ti_fly_type")]
        public byte? FlyingType { get; set; }
                /// <summary>来源活动实例；含义 f=from（frm 超长）。</summary>
        [SooColumn(Name = "ti_f_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string FromActivityInstanceId { get; set; }
        [SooColumn(Name = "ti_frm_activity_id", Length = 64)]
        public string FromActivityId { get; set; }
        [SooColumn(Name = "ti_frm_activity_type")]
        public short? FromActivityType { get; set; }
        [SooColumn(Name = "ti_frm_activity_name", Length = 255)]
        public string FromActivityName { get; set; }
                /// <summary>去向活动实例；含义 t=to。</summary>
        [SooColumn(Name = "ti_t_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ToActivityInstanceId { get; set; }
        [SooColumn(Name = "ti_to_activity_id", Length = 64)]
        public string ToActivityId { get; set; }
        [SooColumn(Name = "ti_to_activity_type")]
        public short? ToActivityType { get; set; }
        [SooColumn(Name = "ti_to_activity_name", Length = 255)]
        public string ToActivityName { get; set; }
        [SooColumn(Name = "ti_cond_result")]
        public byte? ConditionParsedResult { get; set; }
        [SooColumn(Name = "ti_crt_user_id", Length = 64)]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "ti_crt_user_name", Length = 128)]
        public string CreatedUserName { get; set; }
        [SooColumn(Name = "ti_crt_time")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "ti_rec_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}