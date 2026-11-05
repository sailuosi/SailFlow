using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Entity;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.hub.Localize;
using saiflow.hub.Resource;
using saiflow.WebUtility;

namespace saiflow.engine.Manager
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

        /// <summary>
        /// 异步按 Id 取流程实例（Clip take(1)，可取消）。
        /// </summary>
        internal async Task<ProcessInstanceEntity> GetByIdAsync(
            SQLBuilder kit,
            int processInstanceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clip = Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.Id, processInstanceId);
            var list = await clip.select(pi).take(1).queryListAsync(cancellationToken).ConfigureAwait(false);
            return list?.FirstOrDefault();
        }

        private ProcessInstanceEntity GetByVersion(SQLBuilder kit, string processId, string version)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.ProcessId, processId);
            clip.where(() => pi.Version, version);
            clip.orderByDesc(() => pi.Id);
            return clip.select(pi).take(1).queryList()?.FirstOrDefault();
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
            var clip = Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.AppInstanceId, appInstanceId);
            clip.where(() => pi.ProcessId, processId);
            clip.where(() => pi.Version, version);
            clip.where(() => pi.RecordStatusInvalid, (byte)0);
            clip.orderByDesc(() => pi.Id);
            return clip.select(pi).take(1).queryList()?.FirstOrDefault();
        }

        internal IEnumerable<ProcessInstanceEntity> GetProcessInstance(string appInstanceId)
        {
            var kit = Sail.UseRunSQL();
            var clip = Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.AppInstanceId, appInstanceId);
            clip.where(() => pi.RecordStatusInvalid, (byte)0);
            clip.orderByDesc(() => pi.Id);
            return clip.select(pi).queryList();
        }

        internal IEnumerable<ProcessInstanceEntity> GetProcessInstance(
            SQLBuilder kit, string appInstanceId, string processId, string version)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.AppInstanceId, appInstanceId);
            clip.where(() => pi.ProcessId, processId);
            clip.where(() => pi.Version, version);
            clip.where(() => pi.RecordStatusInvalid, (byte)0);
            clip.orderByDesc(() => pi.Id);
            return clip.select(pi).queryList();
        }

        internal ProcessInstanceEntity GetSubProcessInstance(
            SQLBuilder kit, string appInstanceId, string processId, string subProcessId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.AppInstanceId, appInstanceId);
            clip.where(() => pi.ProcessId, processId);
            clip.where(() => pi.SubProcessId, subProcessId);
            clip.where(() => pi.RecordStatusInvalid, (byte)0);
            clip.orderByDesc(() => pi.Id);
            var list = clip.select(pi).queryList().ToList();

            if (list.Count == 1)
                return list[0];
            if (list.Count == 0)
                return null;
            throw new WorkflowException(Sail.Messages.GetEngineMessage("processinstancemanager.GetSubProcessInstance.error"));
        }

        internal ProcessInstanceEntity GetRunningProcessInstance(string appInstanceId, string processId)
        {
            var kit = Sail.UseRunSQL();
            var clip = Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.AppInstanceId, appInstanceId);
            clip.where(() => pi.ProcessId, processId);
            clip.where(() => pi.RecordStatusInvalid, (byte)0);
            clip.where(() => pi.ProcessState == (short)ProcessStateEnum.Ready || pi.ProcessState == (short)ProcessStateEnum.Running);
            clip.orderByDesc(() => pi.Id);
            var list = clip.select(pi).queryList().ToList();
            return EnumHelper.GetFirst<ProcessInstanceEntity>(list);
        }

        internal bool CheckSubProcessInstanceCompleted(SQLBuilder kit, int activityInstanceId, string activityId)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.InvokedActivityInstanceId, activityInstanceId);
            clip.where(() => pi.InvokedActivityId, activityId);
            clip.where(() => pi.RecordStatusInvalid, (byte)0);
            clip.where(() => pi.ProcessState, (short)ProcessStateEnum.Completed);
            clip.orderByDesc(() => pi.CreatedDateTime);
            var list = clip.select(pi).queryList().ToList();
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
            var newId = Sibling(kit).useRepo<ProcessInstanceEntity, int>().InsertReturnId(entity);
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
                StartUnitId = !string.IsNullOrWhiteSpace(runner.StartUnitId)
                    ? runner.StartUnitId
                    : runner.CompanyId,
                StartUnitCode = runner.StartUnitCode,
                StartUnitName = runner.StartUnitName,
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
                entity.OverdueDateTime = CalculateOverdueDateTime(entity.CreatedDateTime.Value, processEntity.EndExpression);

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
                throw new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.complete.error"));
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
                throw new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.suspend.error"));
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
                throw new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.resume.error"));
            }
        }

        internal void RecallSubProcess(int invokedActivityInstanceId, WfAppRunner runner)
        {
            var kit = Sail.UseRunSQL();
            RecallSubProcess(kit, invokedActivityInstanceId, runner);
        }

        internal void RecallSubProcess(SQLBuilder kit, int invokedActivityInstanceId, WfAppRunner runner)
        {
            var clip = Sibling(kit).useClip();
            clip.from<ProcessInstanceEntity>(out var pi);
            clip.where(() => pi.InvokedActivityInstanceId, invokedActivityInstanceId);
            clip.where(() => pi.ProcessState, (short)ProcessStateEnum.Suspended);
            clip.orderByDesc(() => pi.Id);
            var list = clip.select(pi).queryList().ToList();

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
                throw new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.recallsubprocess.error"));
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
                throw new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.reverse.error"));
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
                    throw new WorkflowException(Sail.Messages.GetEngineMessage("processinstancemanager.cancel.error"));

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
                throw new WorkflowException(Sail.Messages.GetEngineMessage("processinstancemanager.cancel.error", e.Message));
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
                    throw new WorkflowException(Sail.Messages.GetEngineMessage("processinstancemanager.discard.error", e.Message));
                }
            }
        }

        internal void Discard(SQLBuilder kit, WfAppRunner runner)
        {
            var clip = Sibling(kit).useClip();
            clip.setTable<ProcessInstanceEntity>(out var pi);
            clip.set(() => pi.ProcessState, (short)ProcessStateEnum.Discarded);
            clip.set(() => pi.RecordStatusInvalid, (byte)1);
            clip.set(() => pi.UpdatedDateTime, DateTime.UtcNow);
            clip.set(() => pi.UpdatedUserId, runner.UserId);
            clip.set(() => pi.UpdatedUserName, runner.UserName);
            clip.where(() => pi.AppInstanceId, runner.AppInstanceId);
            clip.where(() => pi.ProcessId, runner.ProcessId);
            clip.where(() => pi.Version, runner.Version);
            clip.doUpdate();
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
                    throw new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.terminate.error", ex.Message));
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
            throw new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.terminate.error"));
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
            throw new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.setoverdue.error"));
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
                var clip = Sibling(kit).useClip();
                clip.setTable<TaskEntity>(out var t);
                clip.where(() => t.ProcessInstanceId, processInstanceId);
                clip.doDelete();

                DeleteByProcessInstance(kit, Sail.HotShard.ResolveTransitionTable(entity.ProcessId), processInstanceId);
                if (Sail.HotShard.IsSharded(entity.ProcessId))
                    DeleteByProcessInstance(kit, Sail.HotShard.DefaultTransitionTable, processInstanceId);

                clip = Sibling(kit).useClip();
                clip.setTable<ActivityInstanceEntity>(out var ai);
                clip.where(() => ai.ProcessInstanceId, processInstanceId);
                clip.doDelete();

                DeleteByProcessInstance(kit, Sail.HotShard.ResolveVariableTable(entity.ProcessId), processInstanceId);
                if (Sail.HotShard.IsSharded(entity.ProcessId))
                    DeleteByProcessInstance(kit, Sail.HotShard.DefaultVariableTable, processInstanceId);

                Sibling(kit).useRepo<ProcessInstanceEntity, int>().DeleteById(processInstanceId);
                return true;
            }
            throw new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.delete.error"));
        }

        static void DeleteByProcessInstance(SQLBuilder kit, string physicalTable, int processInstanceId)
        {
            try
            {
                Sibling(kit).setTable(physicalTable)
                    .where("process_instance_id", processInstanceId)
                    .doDelete();
            }
            catch
            {
                // 分表未创建时忽略
            }
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
                    var ok = Delete(kit, entity.Id.Value, true);
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
