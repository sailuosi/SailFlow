using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.engine.Common;
using mooSQL.data;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// Process File Entity
    /// 流程文件实体对象
    /// </summary>
    [SooTable(Name = "wf_process", Caption = "流程定义", Type = DBTableType.Table)]
    public class ProcessFileEntity
    {
        [SooColumn(Name = "process_id", Length = 64, Tag = "sail.id")]
        public String ProcessId { get; set; }
        [SooColumn(Name = "process_name", Length = 255)]
        public String ProcessName { get; set; }
        [SooColumn(Name = "process_code", Length = 64)]
        public String ProcessCode { get; set; }
        [SooColumn(Name = "version", Length = 32)]
        public String Version { get; set; }
        [SooColumn(Name = "status")]
        public byte? Status { get; set; }
        [SooColumn(Name = "start_type")]
        public Byte StartType { get; set; }
        [SooColumn(Name = "start_expression", Length = 2000)]
        public String StartExpression { get; set; }
        [SooColumn(Name = "end_type")]
        public Byte EndType { get; set; }
        [SooColumn(Name = "end_expression", Length = 2000)]
        public String EndExpression { get; set; }
        [SooColumn(Name = "description", Length = 500)]
        public String Description { get; set; }
        [SooColumn(Name = "xml_content", DataType = DataFam.Json, Tag = "sail.xml")]
        public String XmlContent { get; set; }
    }

    /// <summary>
    /// 泳道流程实体对象
    /// </summary>
    public class ProcessFilePool
    {
        public List<ProcessEntity> ProcessEntityList { get; set; }
        public string XmlContent { get; set; }
    }

}
