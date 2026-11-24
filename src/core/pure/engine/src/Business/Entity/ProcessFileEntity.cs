using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using sailflow.utils;
using mooSQL.data;

namespace sailflow.data
{
    /// <summary>
    /// 流程文件/模型投影实体（映射同表 <c>sf_process</c>）。
    /// 用于读写模型文件相关字段的精简视图，不含 Package/参与者等协作字段。
    /// </summary>
    [SooTable(Name = "sf_process", Caption = "流程定义", Type = DBTableType.Table)]
    public class ProcessFileEntity
    {
        /// <summary>
        /// 逻辑流程 Id（列 <c>p_process_id</c>），跨版本稳定键。
        /// </summary>
        [SooColumn(Name = "p_process_id", Length = 64, Tag = "sail.id")]
        public String ProcessId { get; set; }

        /// <summary>
        /// 流程名称（列 <c>p_name</c>）。
        /// </summary>
        [SooColumn(Name = "p_name", Length = 255)]
        public String ProcessName { get; set; }

        /// <summary>
        /// 流程编码（列 <c>p_code</c>）。
        /// </summary>
        [SooColumn(Name = "p_code", Length = 64)]
        public String ProcessCode { get; set; }

        /// <summary>
        /// 版本号（列 <c>p_version</c>）。
        /// </summary>
        [SooColumn(Name = "p_version", Length = 32)]
        public String Version { get; set; }

        /// <summary>
        /// 版本使用状态（列 <c>p_status</c>），约定 0/1/2，见 <see cref="ProcessEntity.Status"/>。
        /// </summary>
        [SooColumn(Name = "p_status")]
        public byte? Status { get; set; }

        /// <summary>
        /// 启动类型（列 <c>p_start_type</c>），见 <see cref="ProcessStartTypeEnum"/>。
        /// </summary>
        [SooColumn(Name = "p_start_type")]
        public Byte StartType { get; set; }

        /// <summary>
        /// 启动表达式（列 <c>p_start_expression</c>）。
        /// </summary>
        [SooColumn(Name = "p_start_expression", Length = 2000)]
        public String StartExpression { get; set; }

        /// <summary>
        /// 结束类型（列 <c>p_end_type</c>），见 <see cref="ProcessEndTypeEnum"/>。
        /// </summary>
        [SooColumn(Name = "p_end_type")]
        public Byte EndType { get; set; }

        /// <summary>
        /// 结束表达式（列 <c>p_end_expression</c>）。
        /// </summary>
        [SooColumn(Name = "p_end_expression", Length = 2000)]
        public String EndExpression { get; set; }

        /// <summary>
        /// 流程说明（列 <c>p_description</c>）。
        /// </summary>
        [SooColumn(Name = "p_description", Length = 500)]
        public String Description { get; set; }

        /// <summary>
        /// BPMN/XML 模型正文（列 <c>p_xml_content</c>）。
        /// </summary>
        [SooColumn(Name = "p_xml_content", DataType = DataFam.Json, Tag = "sail.xml")]
        public String XmlContent { get; set; }
    }

    /// <summary>
    /// 泳道协作包内存载体：包下各泳道流程定义列表 + 合并 XML。
    /// 非落库表；保存/升级协作包时在内存组装。
    /// </summary>
    public class ProcessFilePool
    {
        /// <summary>协作包内各泳道/参与者对应的流程定义实体列表。</summary>
        public List<ProcessEntity> ProcessEntityList { get; set; }

        /// <summary>协作包整体或合并后的 XML 内容。</summary>
        public string XmlContent { get; set; }
    }

}
