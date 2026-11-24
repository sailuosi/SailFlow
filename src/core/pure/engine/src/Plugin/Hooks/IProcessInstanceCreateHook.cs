using System.Text.Json.Serialization;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.data;

namespace sailflow.engine.Hooks
{
    /// <summary>
    /// 流程实例创建钩子：对象已由引擎拼装，尚未 Insert。
    /// Sync、同创建事务；异常 → 启动失败回滚。
    /// </summary>
    /// <remarks>
    /// <para><b>【严重 · 同事务自锁】</b>本钩子始终在发起事务内同步调用，
    /// <see cref="ProcessInstanceCreateContext.Kit"/> 即为该事务连接。
    /// 若在 <see cref="OnCreating"/> 中再 <c>Sail.UseRunSQL()</c> 或无 kit 的 Service/Manager
    /// 访问引擎库，易与主事务互相等待，导致发起闷锅（SQLite Error 5/6 / 命令超时）。</para>
    /// <para><b>正确做法</b>：读定义元数据、写序号等一律用 <c>ctx.Kit</c>（或 <c>kit.useSQL()</c>）。
    /// 详见 theme《专题-Bug治理-Sync钩子同事务自锁》。</para>
    /// </remarks>
    public interface IProcessInstanceCreateHook
    {
        string Name { get; }

        /// <summary>中文显示名（日志用）；默认可空。</summary>
        string Title => null;

        /// <summary>
        /// 实例落库前回调。必须使用 <see cref="ProcessInstanceCreateContext.Kit"/>，禁止另开引擎连接。
        /// </summary>
        void OnCreating(ProcessInstanceCreateContext ctx);
    }

    public sealed class ProcessInstanceCreateContext
    {
        public FlowCommandContext Runner { get; set; }
        public ProcessEntity Process { get; set; }

        /// <summary>待插入的实例；Hook 可就地修改 Title / Biz* 等。</summary>
        public ProcessInstanceEntity Instance { get; set; }

        /// <summary>
        /// 同创建事务 kit。
        /// <para><b>【严重】</b>勿再 <c>Sail.UseRunSQL()</c>；另开连接读写引擎库会与发起事务自锁。
        /// 详见《专题-Bug治理-Sync钩子同事务自锁》。</para>
        /// </summary>
        [JsonIgnore]
        public SQLBuilder Kit { get; set; }
    }
}
