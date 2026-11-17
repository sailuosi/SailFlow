using System.Text.Json.Serialization;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.engine.Entity;

namespace sailflow.engine.Hooks
{
    /// <summary>
    /// 流程实例创建钩子：对象已由引擎拼装，尚未 Insert。
    /// Sync、同创建事务；异常 → 启动失败回滚。
    /// </summary>
    public interface IProcessInstanceCreateHook
    {
        string Name { get; }

        void OnCreating(ProcessInstanceCreateContext ctx);
    }

    public sealed class ProcessInstanceCreateContext
    {
        public FlowCommandContext Runner { get; set; }
        public ProcessEntity Process { get; set; }

        /// <summary>待插入的实例；Hook 可就地修改 Title / Biz* 等。</summary>
        public ProcessInstanceEntity Instance { get; set; }

        /// <summary>同事务 kit（勿另开连接）。</summary>
        [JsonIgnore]
        public SQLBuilder Kit { get; set; }
    }
}
