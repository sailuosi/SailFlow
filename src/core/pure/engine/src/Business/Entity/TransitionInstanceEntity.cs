using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using mooSQL.data;

namespace saiflow.engine.Entity
{
    /// <summary>
    /// Transition Instance Entity
    /// 节点转移类
    /// </summary>
    [SooTable(Name = "wf_transition_instance", Caption = "转移实例", Type = DBTableType.Table)]
    public class TransitionInstanceEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
        public string TransitionInstanceId { get; set; }
        [SooColumn(Name = "transition_id")]
        public string TransitionId { get; set; }
        [SooColumn(Name = "app_name")]
        public string AppName { get; set; }
        [SooColumn(Name = "app_instance_id")]
        public string AppInstanceId { get; set; }
        [SooColumn(Name = "process_instance_id", Length = 64)]
        public string ProcessInstanceId { get; set; }
        [SooColumn(Name = "process_id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "transition_type")]
        public byte? TransitionType { get; set; }
        [SooColumn(Name = "flying_type")]
        public byte? FlyingType { get; set; }
        [SooColumn(Name = "from_activity_instance_id", Length = 64)]
        public string FromActivityInstanceId { get; set; }
        [SooColumn(Name = "from_activity_id")]
        public string FromActivityId { get; set; }
        [SooColumn(Name = "from_activity_type")]
        public short? FromActivityType { get; set; }
        [SooColumn(Name = "from_activity_name")]
        public string FromActivityName { get; set; }
        [SooColumn(Name = "to_activity_instance_id", Length = 64)]
        public string ToActivityInstanceId { get; set; }
        [SooColumn(Name = "to_activity_id")]
        public string ToActivityId { get; set; }
        [SooColumn(Name = "to_activity_type")]
        public short? ToActivityType { get; set; }
        [SooColumn(Name = "to_activity_name")]
        public string ToActivityName { get; set; }
        [SooColumn(Name = "condition_parsed_result")]
        public byte? ConditionParsedResult { get; set; }
        [SooColumn(Name = "created_user_id")]
        public string CreatedUserId { get; set; }
        [SooColumn(Name = "created_user_name")]
        public string CreatedUserName { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "record_status_invalid")]
        public byte? RecordStatusInvalid { get; set; }
    }
}
