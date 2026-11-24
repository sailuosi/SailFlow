using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sailflow.utils
{
    /// <summary>
    /// List of dynamic variable control sheet
    /// 动态变量控制列表清单
    /// </summary>
    public class ControlParameterSheet
    {
        /// <summary>
        /// Conditional parameter variables, used for ConditionalStart/ConditionalTask
        /// 条件参数变量，用于ConditionalStart/ConditionalTask
        /// </summary>
        public IDictionary<string, string> ConditionalVariables { get; set; }

        /// <summary>
        /// 加签类型
        /// Run time (dynamic) signing of flow variables
        /// </summary>
        public string SignForwardType { get; set; }

        /// <summary>
        /// Approval rate type: count/percentage
        /// 加签通过率类型 count/percentage
        /// </summary>
        public string SignForwardCompareType { get; set; }

        /// <summary>
        /// Order of signing completion
        /// 加签完成次序
        /// </summary>
        public float SignForwardCompleteOrder { get; set; }

        /// <summary>
        /// Handling the situation of returning as a countersignature node and running it again
        /// </summary>
        public byte RecreatedMultipleInstanceWhenResending { get; set; }

        /// <summary>
        /// Enhance merged control variables
        /// 增强合并控制变量
        /// </summary>
        public int EOrJoinTokenPassCount { get; set; }

        /// <summary>
        /// Wether to cancel adjacent nodes
        /// 是否取消相邻节点
        /// </summary>
        public byte IsCancellingBrothersNode { get; set; }

        /// <summary>
        /// 驳回落点：Terminate / Previous / Starter / Specified（见 <see cref="RejectModeCodes"/>）。
        /// 缺省按 Previous。由 Host 从节点策略注入；引擎不解析 sf:NodePolicy。
        /// </summary>
        public string RejectMode { get; set; }

        /// <summary>
        /// 仅 <see cref="RejectModeCodes.Specified"/>：回跳目标活动定义 Id。
        /// </summary>
        public string RejectReturnActivityId { get; set; }
    }
}
