using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using saiflow.engine.Common;
using mooSQL.data;

namespace saiflow.engine.Entity
{
    /// <summary>
    /// Process Variable Entity
    /// 流程实体类
    /// </summary>
    [SooTable(Name = "wf_process_variable", Caption = "流程变量运行时", Type = DBTableType.Table)]
    public class ProcessVariableEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64)]
        public string ProcessVariableId { get; set; }
        [SooColumn(Name = "variable_scope")]
        public string VariableScope { get; set; }
        [SooColumn(Name = "app_instance_id")]
        public string AppInstanceId { get; set; }
        [SooColumn(Name = "process_id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "process_instance_id", Length = 64)]
        public string ProcessInstanceId { get; set; }
        [SooColumn(Name = "activity_instance_id", Length = 64)]
        public string ActivityInstanceId { get; set; }
        [SooColumn(Name = "activity_id")]
        public string ActivityId { get; set; }
        [SooColumn(Name = "activity_name")]
        public string ActivityName { get; set; }
        [SooColumn(Name = "name")]
        public string Name { get; set; }
        [SooColumn(Name = "value")]
        public string Value { get; set; }
        [SooColumn(Name = "media_type")]
        public string MediaType { get; set;  }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
