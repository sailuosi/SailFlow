using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.SqlProvider;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Xpdl.Node;
using saiflow.hub.Localize;
using saiflow.hub.Resource;
using saiflow.WebUtility;

namespace saiflow.engine.Business.Manager
{
    /// <summary>
    /// 流程实例（wf_process_instance）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    internal class ProcessInstanceManager
    {
        public ProcessInstanceManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }
        private static SQLBuilder Sibling(SQLBuilder kit)
        {
            var next = kit.DBLive.useSQL();
            if (kit.Executor != null)
                next.useTransaction(kit.Executor);
            return next;
        }

        #region Query
        internal ProcessInstanceEntity GetProcessInstanceLatest(string appInstanceId, string processId, string version)
        {
            var kit = Sail.UseRunSQL();
            return GetProcessInstanceLatest(kit, appInstanceId, processId, version);
        }

        internal ProcessInstanceEntity GetProcessInstanceLatest(
            SQLBuilder kit, string appInstanceId, string processId, string version)
            => GetProcessInstanceCurrent(kit, appInstanceId, processId, version);

        internal ProcessInstanceEntity GetById(int processInstanceId)
            => Sail.UseRunSQL().useRepo<ProcessInstanceEntity, int>().GetById(processInstanceId);

        internal ProcessInstanceEntity GetById(SQLBuilder kit, int processInstanceId)
            => Sibling(kit).useRepo<ProcessInstanceEntity, int>().GetById(processInstanceId);

        private ProcessInstanceEntity GetByVersion(SQLBuilder kit, string processId, string version)
        {
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessInstanceTable)
                .where("process_id", processId)
                .where("version", version)
                .orderBy("id DESC")
                .query<ProcessInstanceEntity>()
                .ToList();
            return EnumHelper.GetFirst<ProcessInstanceEntity>(list);
        }

        internal ProcessInstanceEntity GetByActivity(int activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetByActivity(kit, activityInstanceId);
        }

        internal ProcessInstanceEntity GetByActivity(SQLBuilder kit, int activityInstanceId)
        {
            var activityInstance = Sibling(kit).useRepo<ActivityInstanceEntity, int>().GetById(activityInstanceId);
            return Sibling(kit).useRepo<ProcessInstanceEntity, int>().GetById(activityInstance.ProcessInstanceId);
        }

        internal bool IsSubProcess(ProcessInstanceEntity entity)
            => entity.InvokedActivityInstanceId > 0 && !string.IsNullOrEmpty(entity.InvokedActivityId);

        internal User GetProcessInitiator(int processInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetProcessInitiator(kit, processInstanceId);
        }

        internal User GetProcessInitiator(SQLBuilder kit, int processInstanceId)
        {
            var entity = GetById(kit, processInstanceId);
            return new User { UserId = entity.CreatedUserId, UserName = entity.CreatedUserName };
        }

        internal ProcessInstanceEntity GetProcessInstanceCurrent(string appInstanceId, string processId, string version)
        {
            var kit = Sail.UseRunSQL();
            return GetProcessInstanceCurrent(kit, appInstanceId, processId, version);
        }

        internal ProcessInstanceEntity GetProcessInstanceCurrent(
            SQLBuilder kit, string appInstanceId, string processId, string version)
        {
            var processInstanceList = GetProcessInstance(kit, appInstanceId, processId, version).ToList();
            return processInstanceList != null && processInstanceList.Count > 0
                ? processInstanceList[0]
                : null;
        }

        internal IEnumerable<ProcessInstanceEntity> GetProcessInstance(string appInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessInstanceTable)
                .where("app_instance_id", appInstanceId)
                .where("record_status_invalid", 0)
                .orderBy("id DESC")
                .query<ProcessInstanceEntity>();
        }

        internal IEnumerable<ProcessInstanceEntity> GetProcessInstance(
            SQLBuilder kit, string appInstanceId, string processId, string version)
        {
            return Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessInstanceTable)
                .where("app_instance_id", appInstanceId)
                .where("process_id", processId)
                .where("version", version)
                .where("record_status_invalid", 0)
                .orderBy("id DESC")
                .query<ProcessInstanceEntity>();
        }

        internal ProcessInstanceEntity GetSubProcessInstance(
            SQLBuilder kit, string appInstanceId, string processId, string subProcessId)
        {
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessInstanceTable)
                .where("app_instance_id", appInstanceId)
                .where("process_id", processId)
                .where("sub_process_id", subProcessId)
                .where("record_status_invalid", 0)
                .orderBy("id DESC")
                .query<ProcessInstanceEntity>()
                .ToList();

            if (list.Count == 1)
                return list[0];
            if (list.Count == 0)
                return null;
            throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.GetSubProcessInstance.error"));
        }

        internal ProcessInstanceEntity GetRunningProcessInstance(string appInstanceId, string processId)
        {
            var kit = Sail.UseRunSQL();
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessInstanceTable)
                .where("app_instance_id", appInstanceId)
                .where("process_id", processId)
                .where("record_status_invalid", 0)
                .where("(process_state=1 OR process_state=2)")
                .orderBy("id DESC")
                .query<ProcessInstanceEntity>()
                .ToList();
            return EnumHelper.GetFirst<ProcessInstanceEntity>(list);
        }

        internal int GetProcessInstanceCount(string processId, string version)
        {
            var kit = Sail.UseRunSQL();
            return Sibling(kit)
                .from(SailSqlDialect.ProcessInstanceTable)
                .where("process_id", processId)
                .where("version", version)
                .count();
        }

        internal bool CheckSubProcessInstanceCompleted(SQLBuilder kit, int activityInstanceId, string activityId)
        {
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessInstanceTable)
                .where("invoked_activity_instance_id", activityInstanceId)
                .where("invoked_activity_id", activityId)
                .where("record_status_invalid", 0)
                .where("process_state", 4)
                .orderBy("created_datetime DESC")
                .query<ProcessInstanceEntity>()
                .ToList();
            return list.Count == 1;
        }
        #endregion

        #region Write
        internal int Insert(ProcessInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return Insert(kit, entity);
        }

        internal int Insert(SQLBuilder kit, ProcessInstanceEntity entity)
        {
            var newId = Sibling(kit).useRepo<ProcessInstanceEntity, int>().InsertWithPK(entity);
            entity.Id = newId;
            return newId;
        }

        internal void Update(ProcessInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        internal void Update(SQLBuilder kit, ProcessInstanceEntity entity)
            => Sibling(kit).useRepo<ProcessInstanceEntity>().Update(entity);

        internal ProcessInstanceEntity CreateNewProcessInstanceObject(
            WfAppRunner runner,
            ProcessEntity processEntity,
            SubProcessNode subProcessNode = null)
        {
            var entity = new ProcessInstanceEntity
            {
                ProcessId = processEntity.ProcessId,
                ProcessName = processEntity.ProcessName,
                Version = processEntity.Version,
                AppName = runner.AppName,
                AppInstanceId = runner.AppInstanceId,
                AppInstanceCode = runner.AppInstanceCode,
                ProcessState = (int)ProcessStateEnum.Running,
                CreatedUserId = runner.UserId,
                CreatedUserName = runner.UserName,
                CreatedDateTime = DateTime.UtcNow,
                UpdatedUserId = runner.UserId,
                UpdatedUserName = runner.UserName,
                UpdatedDateTime = DateTime.UtcNow
            };

            if (subProcessNode != null)
            {
                entity.SubProcessId = subProcessNode.SubProcessId;
                entity.InvokedActivityId = subProcessNode.ActivityInstance.ActivityId;
                entity.InvokedActivityInstanceId = subProcessNode.ActivityInstance.Id;
                if (subProcessNode.SubProcessNested != null)
                {
                    entity.ProcessId = subProcessNode.ActivityInstance.ProcessId;
                    entity.SubProcessType = (short)SubProcessTypeEnum.Nested;
                }
                else
                {
                    entity.SubProcessType = (short)SubProcessTypeEnum.Referenced;
                    entity.SubProcessDefId = subProcessNode.SubProcessDefId;
                }
            }

            if (processEntity.EndType == (short)ProcessEndTypeEnum.Timer)
                entity.OverdueDateTime = CalculateOverdueDateTime(entity.CreatedDateTime, processEntity.EndExpression);

            return entity;
        }

        private static DateTime CalculateOverdueDateTime(DateTime current, string expression)
        {
            var timeSpan = System.Xml.XmlConvert.ToTimeSpan(expression);
            return current.Add(timeSpan);
        }
        #endregion

        #region Business rules
        internal ProcessInstanceEntity Complete(int processInstanceId, WfAppRunner runner)
        {
            var kit = Sail.UseRunSQL();
            return Complete(kit, processInstanceId, runner);
        }

        internal ProcessInstanceEntity Complete(SQLBuilder kit, int processInstanceId, WfAppRunner runner)
        {
            var processInstance = GetById(kit, processInstanceId);
            var processState = (ProcessStateEnum)Enum.Parse(typeof(ProcessStateEnum), processInstance.ProcessState.ToString());
            if ((processState | ProcessStateEnum.Running) == ProcessStateEnum.Running)
            {
                processInstance.ProcessState = (short)ProcessStateEnum.Completed;
                processInstance.EndedDateTime = DateTime.UtcNow;
                processInstance.EndedUserId = runner.UserId;
                processInstance.EndedUserName = runner.UserName;
                Update(kit, processInstance);
            }
            else
            {
                throw new ProcessInstanceException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.complete.error"));
            }
            return processInstance;
        }

        internal void Suspend(int processInstanceId, WfAppRunner runner)
        {
            var kit = Sail.UseRunSQL();
            Suspend(kit, processInstanceId, runner);
        }

        internal void Suspend(SQLBuilder kit, int processInstanceId, WfAppRunner runner)
        {
            var bEntity = GetById(kit, processInstanceId);
            var processState = (ProcessStateEnum)Enum.Parse(typeof(ProcessStateEnum), bEntity.ProcessState.ToString());
            if ((processState | ProcessStateEnum.Running) == ProcessStateEnum.Running)
            {
                bEntity.ProcessState = (short)ProcessStateEnum.Suspended;
                bEntity.UpdatedDateTime = DateTime.UtcNow;
                bEntity.UpdatedUserId = runner.UserId;
                bEntity.UpdatedUserName = runner.UserName;
                Update(kit, bEntity);
            }
            else
            {
                throw new ProcessInstanceException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.suspend.error"));
            }
        }

        internal void Resume(int processInstanceId, WfAppRunner runner)
        {
            var kit = Sail.UseRunSQL();
            Resume(kit, processInstanceId, runner);
        }

        internal void Resume(SQLBuilder kit, int processInstanceId, WfAppRunner runner)
        {
            var bEntity = GetById(kit, processInstanceId);
            var processState = (ProcessStateEnum)Enum.Parse(typeof(ProcessStateEnum), bEntity.ProcessState.ToString());
            if ((processState | ProcessStateEnum.Suspended) == ProcessStateEnum.Suspended)
            {
                bEntity.ProcessState = (short)ProcessStateEnum.Running;
                bEntity.UpdatedDateTime = DateTime.UtcNow;
                bEntity.UpdatedUserId = runner.UserId;
                bEntity.UpdatedUserName = runner.UserName;
                Update(kit, bEntity);
            }
            else
            {
                throw new ProcessInstanceException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.resume.error"));
            }
        }

        internal void RecallSubProcess(int invokedActivityInstanceId, WfAppRunner runner)
        {
            var kit = Sail.UseRunSQL();
            RecallSubProcess(kit, invokedActivityInstanceId, runner);
        }

        internal void RecallSubProcess(SQLBuilder kit, int invokedActivityInstanceId, WfAppRunner runner)
        {
            var list = Sibling(kit)
                .select("*")
                .from(SailSqlDialect.ProcessInstanceTable)
                .where("invoked_activity_instance_id", invokedActivityInstanceId)
                .where("process_state", 5)
                .orderBy("id DESC")
                .query<ProcessInstanceEntity>()
                .ToList();

            if (list != null && list.Count == 1)
            {
                var bEntity = list[0];
                bEntity.ProcessState = (short)ProcessStateEnum.Running;
                bEntity.UpdatedDateTime = DateTime.UtcNow;
                bEntity.UpdatedUserId = runner.UserId;
                bEntity.UpdatedUserName = runner.UserName;
                Update(kit, bEntity);
            }
            else
            {
                throw new ProcessInstanceException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.recallsubprocess.error"));
            }
        }

        internal void Reverse(int processInstanceId, WfAppRunner currentUser)
        {
            var kit = Sail.UseRunSQL();
            Reverse(kit, processInstanceId, currentUser);
        }

        internal void Reverse(SQLBuilder kit, int processInstanceId, WfAppRunner currentUser)
        {
            var bEntity = GetById(kit, processInstanceId);
            if (bEntity.ProcessState == (short)ProcessStateEnum.Completed)
            {
                bEntity.ProcessState = (short)ProcessStateEnum.Running;
                bEntity.UpdatedUserId = currentUser.UserId;
                bEntity.UpdatedUserName = currentUser.UserName;
                bEntity.UpdatedDateTime = DateTime.UtcNow;
                Update(kit, bEntity);
            }
            else
            {
                throw new ProcessInstanceException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.reverse.error"));
            }
        }

        internal bool Cancel(WfAppRunner runner)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var ok = Cancel(kit, runner);
                    kit.commit();
                    return ok;
                }
                catch
                {
                    throw;
                }
            }
        }

        internal bool Cancel(SQLBuilder kit, WfAppRunner runner)
        {
            try
            {
                var entity = GetProcessInstanceLatest(kit, runner.AppInstanceId, runner.ProcessId, runner.Version);
                if (entity == null || entity.ProcessState != (short)ProcessStateEnum.Running)
                    throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.cancel.error"));

                entity.ProcessState = (short)ProcessStateEnum.Canceled;
                entity.RecordStatusInvalid = 1;
                entity.UpdatedUserId = runner.UserId;
                entity.UpdatedUserName = runner.UserName;
                entity.UpdatedDateTime = DateTime.UtcNow;
                Update(kit, entity);
                return true;
            }
            catch (System.Exception e)
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.cancel.error", e.Message));
            }
        }

        internal bool Discard(WfAppRunner runner)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    Discard(kit, runner);
                    kit.commit();
                    return true;
                }
                catch (System.Exception e)
                {
                    throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.discard.error", e.Message));
                }
            }
        }

        internal void Discard(SQLBuilder kit, WfAppRunner runner)
        {
            Sibling(kit)
                .setTable(SailSqlDialect.ProcessInstanceTable)
                .set("process_state", 7)
                .set("record_status_invalid", 1)
                .set("updated_datetime", DateTime.UtcNow)
                .set("updated_user_id", runner.UserId)
                .set("updated_user_name", runner.UserName)
                .where("app_instance_id", runner.AppInstanceId)
                .where("process_id", runner.ProcessId)
                .where("version", runner.Version)
                .doUpdate();
        }

        internal bool Terminate(WfAppRunner runner)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var entity = GetProcessInstanceLatest(kit, runner.AppInstanceId, runner.ProcessId, runner.Version);
                    var ok = Terminate(kit, entity, runner.UserId, runner.UserName);
                    kit.commit();
                    return ok;
                }
                catch (System.Exception ex)
                {
                    throw new ProcessInstanceException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.terminate.error", ex.Message));
                }
            }
        }

        internal bool Terminate(SQLBuilder kit, ProcessInstanceEntity entity, string userId, string userName)
        {
            if (entity.ProcessState == (int)ProcessStateEnum.Running
                || entity.ProcessState == (int)ProcessStateEnum.Ready
                || entity.ProcessState == (int)ProcessStateEnum.Suspended)
            {
                entity.ProcessState = (short)ProcessStateEnum.Terminated;
                entity.EndedUserId = userId;
                entity.EndedUserName = userName;
                entity.EndedDateTime = DateTime.UtcNow;
                Update(kit, entity);
                return true;
            }
            throw new ProcessInstanceException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.terminate.error"));
        }

        internal bool SetOverdue(int processInstanceId, DateTime overdueDateTime, WfAppRunner runner)
        {
            var kit = Sail.UseRunSQL();
            return SetOverdue(kit, processInstanceId, overdueDateTime, runner);
        }

        internal bool SetOverdue(SQLBuilder kit, int processInstanceId, DateTime overdueDateTime, WfAppRunner runner)
        {
            var entity = GetById(kit, processInstanceId);
            if (entity.ProcessState == (int)ProcessStateEnum.Running
                || entity.ProcessState == (int)ProcessStateEnum.Ready
                || entity.ProcessState == (int)ProcessStateEnum.Suspended)
            {
                entity.OverdueDateTime = overdueDateTime;
                entity.UpdatedUserId = runner.UserId;
                entity.UpdatedUserName = runner.UserName;
                entity.UpdatedDateTime = DateTime.UtcNow;
                Update(kit, entity);
                return true;
            }
            throw new ProcessInstanceException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.setoverdue.error"));
        }

        internal bool Delete(int processInstanceId, bool isForced = false)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var ok = Delete(kit, processInstanceId, isForced);
                    kit.commit();
                    return ok;
                }
                catch
                {
                    throw;
                }
            }
        }

        internal bool Delete(SQLBuilder kit, int processInstanceId, bool isForced = false)
        {
            var entity = GetById(kit, processInstanceId);
            if (entity.ProcessState == (int)ProcessStateEnum.Canceled || isForced)
            {
                Sibling(kit).setTable(SailSqlDialect.TaskTable)
                    .where("process_instance_id", processInstanceId).doDelete();
                Sibling(kit).setTable(SailSqlDialect.TransitionInstanceTable)
                    .where("process_instance_id", processInstanceId).doDelete();
                Sibling(kit).setTable(SailSqlDialect.ActivityInstanceTable)
                    .where("process_instance_id", processInstanceId).doDelete();
                Sibling(kit).setTable(SailSqlDialect.ProcessVariableTable)
                    .where("process_instance_id", processInstanceId).doDelete();
                Sibling(kit).useRepo<ProcessInstanceEntity, int>().DeleteById(processInstanceId);
                return true;
            }
            throw new ProcessInstanceException(LocalizeMessagesLocator.Current.GetEngineMessage("processinstancemanager.delete.error"));
        }

        internal bool Delete(string processId, string version)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var entity = GetByVersion(kit, processId, version);
                    if (entity == null)
                        return false;
                    var ok = Delete(kit, entity.Id, true);
                    kit.commit();
                    return ok;
                }
                catch
                {
                    throw;
                }
            }
        }
        #endregion
    }
}
