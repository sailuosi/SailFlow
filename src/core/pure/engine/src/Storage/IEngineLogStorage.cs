using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 引擎/作业日志表纯库（kit 首参；物理表名由 Sink 按分表策略解析后传入）。
    /// </summary>
    public interface IEngineLogStorage
    {
        /// <summary>写入 <see cref="LogEntity"/>；基表或分表物理名。</summary>
        void InsertLog(SQLBuilder kit, LogEntity entity, string physicalTable, string templateTable);

        /// <summary>写入 <see cref="JobLogEntity"/>；基表或分表物理名。</summary>
        void InsertJobLog(SQLBuilder kit, JobLogEntity entity, string physicalTable, string templateTable);
    }
}
