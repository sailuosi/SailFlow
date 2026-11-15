using System;
using System.Collections.Generic;
using mooSQL.data;


namespace sailflow.engine.Entity
{
    /// <summary>
    /// 流程定义实体（表 <c>wf_process</c>）。
    /// 一行对应「某一逻辑流程 × 某一版本」的定义快照；BPMN/XML 与启动结束策略均落于此。
    /// </summary>
    [SooTable(Name = "wf_process", Caption = "流程定义", Type = DBTableType.Table)]
    public class ProcessEntity
    {
        /// <summary>
        /// 流程定义行主键（列 <c>p_id</c>）。
        /// 每次 Insert/Upgrade 生成新行；同逻辑流程多版本各有独立 DefId。
        /// </summary>
        [SooColumn(Name = "p_id", IsPrimaryKey = true, AutoKey = "SailId", Length = 64, Tag = "sail.id")]
        public string ProcessDefId { get; set; }

        /// <summary>
        /// 逻辑流程 Id（列 <c>p_process_id</c>）。
        /// 跨版本稳定；启动、缓存失效、按版本查询均以 (ProcessId, Version) 定位。
        /// 泳道子流程行亦有各自 ProcessId，并通过 PackageId 挂到协作包。
        /// </summary>
        [SooColumn(Name = "p_process_id", Length = 64, Tag = "sail.id")]
        public string ProcessId { get; set; }

        /// <summary>
        /// 流程名称（定义名；列 <c>p_name</c>）。
        /// 创建流程实例时会拷贝到 <see cref="ProcessInstanceEntity.ProcessName"/>（定义名快照，不是实例标题）。
        /// </summary>
        [SooColumn(Name = "p_name", Length = 255)]
        public string ProcessName { get; set; }

        /// <summary>
        /// 流程编码（列 <c>p_code</c>）。
        /// 业务侧常用作 Hook/Gate 的 ProcessCodes 匹配键、对外契约码。
        /// </summary>
        [SooColumn(Name = "p_code", Length = 64)]
        public string ProcessCode { get; set; }

        /// <summary>
        /// 流程版本号（列 <c>p_version</c>）。
        /// 与 ProcessId 组成唯一业务键；升级时写入新版本行。
        /// </summary>
        [SooColumn(Name = "p_version", Length = 32)]
        public string Version { get; set; }

        /// <summary>
        /// 版本使用状态（列 <c>p_status</c>）。
        /// 由 <c>UpdateProcessUsingState</c> 写入，约定取值 0/1/2（停用、启用中、历史等，以调用方业务为准）；
        /// 新建/发布路径常置 1。
        /// </summary>
        [SooColumn(Name = "p_status")]
        public byte? Status { get; set; }

        /// <summary>
        /// 应用/业务分类标签（列 <c>p_app_type</c>）。
        /// 可用于按类型检索流程定义（如 <c>ProcessManager.GetSingleByAppType</c>），非 BPMN 节点类型。
        /// </summary>
        [SooColumn(Name = "p_app_type", Length = 64)]
        public string AppType { get; set; }

        /// <summary>
        /// 流程包类型（列 <c>p_pkg_type</c>），见 <see cref="sailflow.engine.Common.PackageTypeEnum"/>。
        /// 1=Package 协作流程包；2=Pool 泳道流程。
        /// </summary>
        [SooColumn(Name = "p_pkg_type")]
        public byte? PackageType { get; set; }

        /// <summary>
        /// 所属协作包定义 Id（列 <c>p_pkg_id</c>）。
        /// 泳道子流程指向主包行的 <see cref="ProcessDefId"/>；主包自身通常为空。
        /// </summary>
        [SooColumn(Name = "p_pkg_id", Length = 64, Tag = "sail.id", IsFK = true)]
        public string PackageId { get; set; }

        /// <summary>
        /// 泳道参与者 GUID（列 <c>p_ptcp_guid</c>）。
        /// 协作包下各 Pool 流程与设计器 ParticipantId 对齐，用于泳道保存/升级时匹配子流程。
        /// </summary>
        [SooColumn(Name = "p_ptcp_guid", Length = 64)]
        public string ParticipantGUID { get; set; }

        /// <summary>
        /// 默认办理页/表单 URL（列 <c>p_page_url</c>）。
        /// 产品侧打开待办时可回落使用；引擎主路径不强制消费。
        /// </summary>
        [SooColumn(Name = "p_page_url", Length = 512)]
        public string PageUrl { get; set; }

        /// <summary>
        /// BPMN/XML 文件名（列 <c>p_xml_file_name</c>），上传或导出时的原始文件名。
        /// </summary>
        [SooColumn(Name = "p_xml_file_name", Length = 255)]
        public string XmlFileName { get; set; }

        /// <summary>
        /// BPMN/XML 存储路径（列 <c>p_xml_file_path</c>）；内容以 <see cref="XmlContent"/> 为准时可仅作元数据。
        /// </summary>
        [SooColumn(Name = "p_xml_file_path", Length = 512)]
        public string XmlFilePath { get; set; }

        /// <summary>
        /// 流程模型 XML/BPMN 正文（列 <c>p_xml_content</c>；Tag sail.xml → InitDb 可覆盖为 Json(max)）。
        /// 运行时解析为内存 ProcessModel；变量定义等亦可从此迁移到 <see cref="VariableEntity"/>。
        /// </summary>
        [SooColumn(Name = "p_xml_content", DataType = DataFam.Json, Tag = "sail.xml")]
        public string XmlContent { get; set; }

        /// <summary>
        /// 流程启动类型（列 <c>p_start_type</c>），见 <see cref="sailflow.engine.Common.ProcessStartTypeEnum"/>。
        /// 0=None；1=Timer 定时启动；2=Message 消息启动。
        /// </summary>
        [SooColumn(Name = "p_start_type")]
        public byte? StartType { get; set; }

        /// <summary>
        /// 启动表达式（列 <c>p_start_expression</c>）。
        /// 配合 StartType：定时 Cron/表达式，或消息相关表达式。
        /// </summary>
        [SooColumn(Name = "p_start_expression", Length = 2000)]
        public string StartExpression { get; set; }

        /// <summary>
        /// 流程结束类型（列 <c>p_end_type</c>），见 <see cref="sailflow.engine.Common.ProcessEndTypeEnum"/>。
        /// 0=None；1=Timer 定时结束；2=Message 消息结束。
        /// Timer 时创建实例会据 EndExpression 计算 <see cref="ProcessInstanceEntity.OverdueDateTime"/>。
        /// </summary>
        [SooColumn(Name = "p_end_type")]
        public byte? EndType { get; set; }

        /// <summary>
        /// 结束表达式（列 <c>p_end_expression</c>）。
        /// 配合 EndType：定时超期时长/表达式，或消息结束条件。
        /// </summary>
        [SooColumn(Name = "p_end_expression", Length = 2000)]
        public string EndExpression { get; set; }

        /// <summary>
        /// 流程图标（列 <c>p_icon</c>），产品列表/门户展示用。
        /// </summary>
        [SooColumn(Name = "p_icon", Length = 128)]
        public string Icon { get; set; }

        /// <summary>
        /// 流程说明（列 <c>p_description</c>）。
        /// </summary>
        [SooColumn(Name = "p_description", Length = 500)]
        public string Description { get; set; }

        /// <summary>
        /// 定义创建时间（列 <c>p_crt_time</c>）。
        /// </summary>
        [SooColumn(Name = "p_crt_time")]
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// 定义最后更新时间（列 <c>p_upd_time</c>）。
        /// </summary>
        [SooColumn(Name = "p_upd_time")]
        public DateTime? UpdatedDateTime { get; set; }
    }
}
