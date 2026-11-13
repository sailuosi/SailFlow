using System;
using System.Collections.Generic;
using mooSQL.data;


namespace sailflow.engine.Entity
{
    /// <summary>
    /// Process Entity
    /// 流程实体类
    /// </summary>
    [SooTable(Name = "wf_process", Caption = "流程定义", Type = DBTableType.Table)]
    public class ProcessEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, AutoKey = "Guid", Length = 64, Tag = "sail.id")]
        public string ProcessDefId { get; set; }
        [SooColumn(Name = "process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "process_name", Length = 255)]
        public string ProcessName { get; set; }
        [SooColumn(Name = "process_code", Length = 64)]
        public string ProcessCode { get; set; }
        [SooColumn(Name = "version", Length = 32)]
        public string Version { get; set; }
        [SooColumn(Name = "status")]
        public byte? Status { get; set; } 
        [SooColumn(Name = "app_type", Length = 64)]
        public string AppType { get; set; }
        [SooColumn(Name = "package_type")]
        public byte? PackageType { get; set; }
        [SooColumn(Name = "package_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string PackageId { get; set; }
        [SooColumn(Name = "participant_guid", Length = 64)]
        public string ParticipantGUID { get; set; }
        [SooColumn(Name = "page_url", Length = 512)]
        public string PageUrl { get; set; }
        [SooColumn(Name = "xml_file_name", Length = 255)]
        public string XmlFileName { get; set; }
        [SooColumn(Name = "xml_file_path", Length = 512)]
        public string XmlFilePath { get; set; }
        /// <summary>BPMN XML；Tag sail.xml → InitDb 覆盖为 Json(max)。</summary>
        [SooColumn(Name = "xml_content", DataType = DataFam.Json, Tag = "sail.xml")]
        public string XmlContent { get; set; }
        [SooColumn(Name = "start_type")]
        public byte? StartType { get; set; }
        [SooColumn(Name = "start_expression", Length = 2000)]
        public string StartExpression { get; set; }
        [SooColumn(Name = "end_type")]
        public byte? EndType { get; set; }
        [SooColumn(Name = "end_expression", Length = 2000)]
        public string EndExpression { get; set; }
        [SooColumn(Name = "icon", Length = 128)]
        public string Icon { get; set; }
        [SooColumn(Name = "description", Length = 500)]
        public string Description { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
