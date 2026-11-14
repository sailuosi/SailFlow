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
    [SooTable(Name = "wf_proc_variable", Caption = "流程变量运行时", Type = DBTableType.Table)]
    public class ProcessVariableEntity
    {
        [SooColumn(Name = "pv_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string ProcessVariableId { get; set; }
        [SooColumn(Name = "pv_var_scope", Length = 64)]
        public string VariableScope { get; set; }
        [SooColumn(Name = "pv_biz_pk", Length = 128)]
        public string BizPK { get; set; }
        [SooColumn(Name = "pv_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "pv_proc_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ProcessInstanceId { get; set; }
        [SooColumn(Name = "pv_act_instance_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string ActivityInstanceId { get; set; }
        [SooColumn(Name = "pv_activity_id", Length = 64)]
        public string ActivityId { get; set; }
        [SooColumn(Name = "pv_activity_name", Length = 255)]
        public string ActivityName { get; set; }
        [SooColumn(Name = "pv_name", Length = 128)]
        public string Name { get; set; }
        [SooColumn(Name = "pv_value", DataType = DataFam.Json, Tag = "sail.xml")]
        public string Value { get; set; }
        [SooColumn(Name = "pv_media_type", Length = 64)]
        public string MediaType { get; set;  }
        [SooColumn(Name = "pv_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}