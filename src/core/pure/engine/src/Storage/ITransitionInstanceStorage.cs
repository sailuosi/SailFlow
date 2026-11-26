using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 转移轨迹表（<see cref="TrackEntity"/> / <c>sf_track</c>）纯库交互（kit 首参、无状态）。
    /// 物理表名由 Manager/Router 解析后传入，Storage 不持分片策略。
    /// </summary>
    public interface ITransitionInstanceStorage
    {
        /// <summary>插入轨迹行；<paramref name="physicalTable"/> 非空时写入热分表。</summary>
        string Insert(SQLBuilder kit, TrackEntity entity, string physicalTable = null);
    }
}
