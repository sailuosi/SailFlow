
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace saiflow.engine.Common
{
    /// <summary>
    /// B · 命令入参袋（请求级 / L-Cmd）。规范类型名见 <see cref="FlowCommandContext"/>（本类为其实现基型）。
    /// 可由 <c>SailClient.CreateRunBuilder(...).Build()</c> 产出；勿挂在 Client/Service 字段。
    /// <para>
    /// 边界：本袋是入参；<c>WfRuntimeManager*</c> / <c>NodeMediator*</c> 是另一次工厂 Create 的执行编排袋。
    /// </para>
    /// Process executor (operator of business applications).
    /// WfAppRunner Json Format：
    /// {
    ///     "UserId":"10",
    ///     "UserName":"Long",
    ///     "AppName":"SamplePrice",
    ///     "AppInstanceId":"100",
    ///     "ProcessId":"072af8c3-482a-4b1c-890b-685ce2fcc75d"
    /// }
    /// </summary>
    public class WfAppRunner
    {
        /// <summary>
        /// Business Data: Application Name
        /// 业务数据：应用名称
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Business data: Application instance Id (such as document number)
        /// 业务数据：应用实例Id（比如单据票据编号）
        /// </summary>
        public string AppInstanceId { get; set; }

        /// <summary>
        /// Business data: Application instance Id (such as document code)
        /// 业务数据：应用实例ID（比如单据票据代码）
        /// </summary>
        public string AppInstanceCode { get; set; }

        /// <summary>
        /// Process data: Process GUID
        /// 流程数据：流程GUID
        /// </summary>
        public string ProcessId { get; set; }

        /// <summary>
        /// Process data: Process Code
        /// 流程数据：流程代码
        /// </summary>
        public string ProcessCode { get; set; }

        /// <summary>
        /// Process data: Process Version
        /// 流程数据：流程版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// User data: User Id
        /// 用户数据：用户Id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// User data: User Name
        /// 流程数据：用户名称
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Company Id
        /// 公司Id
        /// </summary>
        public string CompanyId { get; set; }

        /// <summary>发起单位（写入流程实例 StartUnit*；可空）。未填时可用 CompanyId 回落为 StartUnitId。</summary>
        public string StartUnitId { get; set; }

        public string StartUnitCode { get; set; }

        public string StartUnitName { get; set; }

        /// <summary>
        /// Process Data: Task Id
        /// Task Id, distinguishing the unique task in the current user's ActiveInstance list
        /// 流程数据：待办任务Id
        /// </summary>
        public Nullable<int> TaskId { get; set; }

        /// <summary>
        /// Process data: Activity instance Id
        /// 流程数据：活动实例Id
        /// </summary>
        public Nullable<int> ActivityInstanceId { get; set; }

        /// <summary>
        /// Approval Status
        /// 审批结果状态
        /// </summary>
        public ApprovalStatusEnum ApprovalStatus { get; set; }

        /// <summary>
        /// Conditions
        /// 条件
        /// </summary>
        private IDictionary<string, string> conditions;

        /// <summary>
        /// Process data: conditions parameters
        /// 流程数据：条件参数
        /// </summary>
        public IDictionary<string, string> Conditions 
        {
            get
            {
                if (conditions == null)
                {
                    conditions = new Dictionary<string, string>();
                }
                return conditions;
            }
            set
            {
                conditions = value;
            }
        }
        /// <summary>
        /// Process data: dynamic variables
        /// 流程数据：动态变量
        /// </summary>
        public IDictionary<string, string> DynamicVariables { get; set; }

        /// <summary>
        /// Process data: control parameter sheet
        /// 流程数据：控制参数
        /// </summary>
        public ControlParameterSheet ControlParameterSheet { get; set; }

        /// <summary>
        /// Process data: List of performers for the next step of handling
        /// 流程数据：下一步办理人员列表
        /// </summary>
        public IDictionary<string, PerformerList> NextActivityPerformers { get; set; }

        /// <summary>
        /// Process data: performer type for the next step of handling
        /// 流程数据：下一步执行类型
        /// </summary>
        public NextPerformerIntTypeEnum NextPerformerType { get; set; }

        /// <summary>
        /// Topic used for message initiation
        /// 用于消息启动时的主题
        /// </summary>
        public string MessageTopic { get; set; }
    }
}
