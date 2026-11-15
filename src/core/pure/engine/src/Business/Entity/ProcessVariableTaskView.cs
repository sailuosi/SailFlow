using System;
using System.Collections.Generic;
using System.Text;
using sailflow.engine.Common;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 任务视角下的流程变量投影（非落库视图 DTO）。
    /// 将某任务上下文关联的变量名值一并带回，便于办理页展示。
    /// </summary>
    public class ProcessVariableTaskView
    {
        /// <summary>关联任务 Id。</summary>
        public string TaskId { get; set; }

        /// <summary>
        /// 变量作用域字符串，见 <see cref="ProcessVariableScopeEnum"/>（System/Process/Activity）。
        /// </summary>
        public string VariableScope { get; set; }

        /// <summary>变量名。</summary>
        public string Name { get; set; }

        /// <summary>变量当前值。</summary>
        public string Value { get; set; }

        /// <summary>值媒体类型，指示 Value 如何解释。</summary>
        public string MediaType { get; set; }
    }
}
