using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Process Variable Entity
    /// 流程实体类
    /// </summary>
    [SooTable(Name = "wf_process_variable", Caption = "流程变量运行时", Type = DBTableType.Table)]
    public class ProcessVariableEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string ProcessVariableId { get; set; }
        [SooColumn(Name = "variable_scope", Length = 64)]
        public string VariableScope { get; set; }
        [SooColumn(Name = "app_instance_id", Length = 128)]
        public string AppInstanceId { get; set; }
        [SooColumn(Name = "process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "process_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }
        [SooColumn(Name = "activity_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ActivityInstanceId { get; set; }
        [SooColumn(Name = "activity_id", Length = 64)]
        public string ActivityId { get; set; }
        [SooColumn(Name = "activity_name", Length = 255)]
        public string ActivityName { get; set; }
        [SooColumn(Name = "name", Length = 128)]
        public string Name { get; set; }
        [SooColumn(Name = "value", DataType = DataFam.Json, Tag = "sail.xml")]
        public string Value { get; set; }
        [SooColumn(Name = "media_type", Length = 64)]
        public string MediaType { get; set;  }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
