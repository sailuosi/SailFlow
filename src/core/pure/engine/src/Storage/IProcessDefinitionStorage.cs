using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 流程<strong>定义</strong>元数据表纯库（<c>ProcessEntity</c>；kit 首参）。
    /// 不含 XPDL 文件侧（见 <c>IXPDLStorage</c>）；抛业务异常留 Manager。
    /// </summary>
    public interface IProcessDefinitionStorage
    {
        ProcessEntity GetById(SQLBuilder kit, string processDefId);

        IList<ProcessEntity> ListByProcessIdVersion(SQLBuilder kit, string processId, string version);

        IList<ProcessEntity> ListByProcessIdStatus(SQLBuilder kit, string processId, byte status);

        IList<ProcessEntity> ListByName(SQLBuilder kit, string processName, string version);

        IList<ProcessEntity> ListByCode(SQLBuilder kit, string processCode, string version);

        /// <summary>StartType=Message 且 StartExpression=topic。</summary>
        IList<ProcessEntity> ListByMessage(SQLBuilder kit, string topic);

        IList<ProcessEntity> ListByAppType(SQLBuilder kit, string appType);

        IList<ProcessEntity> GetAll(SQLBuilder kit);

        IList<ProcessEntity> ListByPackageId(SQLBuilder kit, string packageId);

        IList<ProcessEntity> ListPoolsByPackageId(SQLBuilder kit, string packageId);

        string Insert(SQLBuilder kit, ProcessEntity entity);

        void Update(SQLBuilder kit, ProcessEntity entity);

        void DeleteById(SQLBuilder kit, string processDefId);

        void Delete(SQLBuilder kit, ProcessEntity entity);

        void DeleteByProcessId(SQLBuilder kit, string processId);

        void DeleteByProcessIdVersion(SQLBuilder kit, string processId, string version);

        /// <summary>删除 PackageId 下全部子行（DeleteProcess 包级清理）。</summary>
        void DeleteByPackageId(SQLBuilder kit, string packageId);

        /// <summary>删除 PackageId 下 PackageType=Pool 的泳道流程。</summary>
        void DeletePoolsByPackageId(SQLBuilder kit, string packageId);

        void DeletePoolsByPackageIdAndDefIds(
            SQLBuilder kit,
            string packageId,
            IList<string> processDefIds);

        /// <summary>按 PackageId 批量更新子流程 Status（主包流程状态联动）。</summary>
        void UpdateStatusByPackageId(SQLBuilder kit, string packageProcessDefId, byte status);
    }
}
