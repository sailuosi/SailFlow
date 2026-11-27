using System;
using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;

namespace sailflow.storage
{
    /// <summary>
    /// 归档冷表 / 候选查询纯库（kit 首参；物理表名由调用方按年解析）。
    /// 热表删读写优先复用 PI/Task/AI/Transition/Variable Storage。
    /// </summary>
    public interface IProcessArchiveStorage
    {
        IList<ProcessInstanceEntity> ListArchivableCandidates(
            SQLBuilder kit,
            DateTime cutoffUtc,
            int limit);

        ProcessInstanceEntity GetProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId);

        IList<TaskEntity> ListTasks(SQLBuilder kit, string physicalTable, string processInstanceId);

        IList<ActivityInstanceEntity> ListActivities(SQLBuilder kit, string physicalTable, string processInstanceId);

        IList<TrackEntity> ListTransitions(SQLBuilder kit, string physicalTable, string processInstanceId);

        IList<ProcessVariableEntity> ListVariables(SQLBuilder kit, string physicalTable, string processInstanceId);

        bool ExistsProcessInstance(SQLBuilder kit, string physicalTable, string id);

        bool ExistsTask(SQLBuilder kit, string physicalTable, string id);

        bool ExistsActivityInstance(SQLBuilder kit, string physicalTable, string id);

        bool ExistsTransition(SQLBuilder kit, string physicalTable, string id);

        bool ExistsProcessVariable(SQLBuilder kit, string physicalTable, string id);

        void InsertPreservingKeyProcessInstance(SQLBuilder kit, string physicalTable, ProcessInstanceEntity entity);

        void InsertPreservingKeyTask(SQLBuilder kit, string physicalTable, TaskEntity entity);

        void InsertPreservingKeyActivityInstance(SQLBuilder kit, string physicalTable, ActivityInstanceEntity entity);

        void InsertPreservingKeyTransition(SQLBuilder kit, string physicalTable, TrackEntity entity);

        void InsertPreservingKeyProcessVariable(SQLBuilder kit, string physicalTable, ProcessVariableEntity entity);
    }
}
