using System;
using System.Collections.Generic;
using mooSQL.data;


namespace saiflow.engine.Business.Entity
{
    /// <summary>
    /// Process Entity
    /// 流程实体类
    /// </summary>
    [SooTable(Name = "wf_process", Caption = "流程定义", Type = DBTableType.Table)]
    public class ProcessEntity
    {
        [SooColumn(Name = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int? Id { get; set; }
        [SooColumn(Name = "process_id")]
        public string ProcessId { get; set; }
        [SooColumn(Name = "process_name")]
        public string ProcessName { get; set; }
        [SooColumn(Name = "process_code")]
        public string ProcessCode { get; set; }
        [SooColumn(Name = "version")]
        public string Version { get; set; }
        [SooColumn(Name = "status")]
        public byte? Status { get; set; } 
        [SooColumn(Name = "app_type")]
        public string AppType { get; set; }
        [SooColumn(Name = "package_type")]
        public byte? PackageType { get; set; }
        [SooColumn(Name = "package_id")]
        public int? PackageId { get; set; }
        [SooColumn(Name = "participant_guid")]
        public string ParticipantGUID { get; set; }
        [SooColumn(Name = "page_url")]
        public string PageUrl { get; set; }
        [SooColumn(Name = "xml_file_name")]
        public string XmlFileName { get; set; }
        [SooColumn(Name = "xml_file_path")]
        public string XmlFilePath { get; set; }
        [SooColumn(Name = "xml_content")]
        public string XmlContent { get; set; }
        [SooColumn(Name = "start_type")]
        public byte? StartType { get; set; }
        [SooColumn(Name = "start_expression")]
        public string StartExpression { get; set; }
        [SooColumn(Name = "end_type")]
        public byte? EndType { get; set; }
        [SooColumn(Name = "end_expression")]
        public string EndExpression { get; set; }
        [SooColumn(Name = "icon")]
        public string Icon { get; set; }
        [SooColumn(Name = "description")]
        public string Description { get; set; }
        [SooColumn(Name = "created_datetime")]
        public DateTime? CreatedDateTime { get; set; }
        [SooColumn(Name = "updated_datetime")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
