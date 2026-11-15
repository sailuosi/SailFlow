using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.engine.Common;

namespace sailflow.engine.Entity
{
    /// <summary>
    /// 流程变量查询条件（非落库）。
    /// 供 <c>ProcessVariableManager</c> 按作用域 + 实例/活动 + 名称检索运行时变量。
    /// </summary>
    public class ProcessVariableQuery
    {
        /// <summary>
        /// 变量作用域（注意属性名历史拼写 VariableSceope）。
        /// Process 查实例级；Activity 查活动级。
        /// </summary>
        public ProcessVariableScopeEnum VariableSceope { get; set; }

        /// <summary>流程实例 Id；Process 作用域必填。</summary>
        public string ProcessInstanceId { get; set; }

        /// <summary>活动实例 Id；Activity 作用域必填。</summary>
        public string ActivityInstanceId { get; set; }

        /// <summary>变量名；可空表示查该作用域下全部。</summary>
        public string Name { get; set; }
    }
}
