using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.events;
using sailflow.hooks;
using sailflow.log;
using sailflow.engine.Xpdl;
using sailflow.hub.Localize;
using sailflow.hub.Resource;
using sailflow.WebUtility;
using sailflow.run;

namespace sailflow.data
{
    /// <summary>
    /// 流程实例（sf_proc_instance）。
    /// 终态：无事务从 Client 取 kit；有事务只用传入的同一个 kit。
    /// </summary>
    public class ProcessInstanceManager : IProcessInstanceManager
    {
        public ProcessInstanceManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        /// <summary>业务硬失败：写 <see cref="SailFailLog"/> 后原样抛出。</summary>
        Exception FailThrow(Exception ex, string op, string detail = null)
        {
            SailFailLog.Exception(Sail, SailLogCategories.Engine, "ProcessInstance." + op, ex, detail);
            return ex;
        }
        #region Query
        public ProcessInstanceEntity GetProcessInstanceLatest(string bizPK, string processId, string version)
        {
            var kit = Sail.UseRunSQL();
            return GetProcessInstanceLatest(kit, bizPK, processId, version);
        }

        public ProcessInstanceEntity GetProcessInstanceLatest(
            SQLBuilder kit, string bizPK, string processId, string version)
            => GetProcessInstanceCurrent(kit, bizPK, processId, version);

        public ProcessInstanceEntity GetById(string processInstanceId)
            => Sail.UseRunSQL().findRowById<ProcessInstanceEntity>(processInstanceId);

        public ProcessInstanceEntity GetById(SQLBuilder kit, string processInstanceId)
            => kit.findRowById<ProcessInstanceEntity>(processInstanceId);

        /// <summary>
        /// 异步按 Id 取流程实例（Clip take(1)，可取消）。
        /// </summary>
        public async Task<ProcessInstanceEntity> GetByIdAsync(
            SQLBuilder kit,
            string processInstanceId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.ProcessInstanceId, processInstanceId);
            var list = await clip.select(pi).take(1).queryListAsync(cancellationToken).ConfigureAwait(false);
            return list?.FirstOrDefault();
        }

        private ProcessInstanceEntity GetByVersion(SQLBuilder kit, string processId, string version)
        {
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.ProcessId, processId)
                .where(() => pi.Version, version)
                .orderByDesc(() => pi.CreatedDateTime);
            return clip.select(pi).take(1).queryList()?.FirstOrDefault();
        }

        public ProcessInstanceEntity GetByActivity(string activityInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetByActivity(kit, activityInstanceId);
        }

        public ProcessInstanceEntity GetByActivity(SQLBuilder kit, string activityInstanceId)
        {
            var activityInstance = kit.findRowById<ActivityInstanceEntity>(activityInstanceId);
            return kit.findRowById<ProcessInstanceEntity>(activityInstance.ProcessInstanceId);
        }

        public bool IsSubProcess(ProcessInstanceEntity entity)
            => !string.IsNullOrWhiteSpace(entity.InvokedActInstanceId) && !string.IsNullOrEmpty(entity.InvokedActivityId);

        public User GetProcessInitiator(string processInstanceId)
        {
            var kit = Sail.UseRunSQL();
            return GetProcessInitiator(kit, processInstanceId);
        }

        public User GetProcessInitiator(SQLBuilder kit, string processInstanceId)
        {
            var entity = GetById(kit, processInstanceId);
            return new User { UserId = entity.CreatedUserId, UserName = entity.CreatedUserName };
        }

        public ProcessInstanceEntity GetProcessInstanceCurrent(string bizPK, string processId, string version)
        {
            var kit = Sail.UseRunSQL();
            return GetProcessInstanceCurrent(kit, bizPK, processId, version);
        }

        public ProcessInstanceEntity GetProcessInstanceCurrent(
            SQLBuilder kit, string bizPK, string processId, string version)
        {
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.BizPK, bizPK)
                .where(() => pi.ProcessId, processId)
                .where(() => pi.Version, version)
                .where(() => pi.RecordStatusInvalid, (byte)0)
                .orderByDesc(() => pi.CreatedDateTime);
            return clip.select(pi).take(1).queryList()?.FirstOrDefault();
        }

        public IEnumerable<ProcessInstanceEntity> GetProcessInstance(string bizPK)
        {
            var kit = Sail.UseRunSQL();
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.BizPK, bizPK)
                .where(() => pi.RecordStatusInvalid, (byte)0)
                .orderByDesc(() => pi.CreatedDateTime);
            return clip.select(pi).queryList();
        }

        public IEnumerable<ProcessInstanceEntity> GetProcessInstance(
            SQLBuilder kit, string bizPK, string processId, string version)
        {
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.BizPK, bizPK)
                .where(() => pi.ProcessId, processId)
                .where(() => pi.Version, version)
                .where(() => pi.RecordStatusInvalid, (byte)0)
                .orderByDesc(() => pi.CreatedDateTime);
            return clip.select(pi).queryList();
        }

        public ProcessInstanceEntity GetSubProcessInstance(
            SQLBuilder kit, string bizPK, string processId, string subProcessId)
        {
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.BizPK, bizPK)
                .where(() => pi.ProcessId, processId)
                .where(() => pi.SubProcessId, subProcessId)
                .where(() => pi.RecordStatusInvalid, (byte)0)
                .orderByDesc(() => pi.CreatedDateTime);
            var list = clip.select(pi).queryList().ToList();

            if (list.Count == 1)
                return list[0];
            if (list.Count == 0)
                return null;
            throw FailThrow(new WorkflowException(Sail.Messages.GetEngineMessage("processinstancemanager.GetSubProcessInstance.error")), "GetSubProcessInstance");
        }

        public ProcessInstanceEntity GetRunningProcessInstance(string bizPK, string processId)
        {
            var kit = Sail.UseRunSQL();
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.BizPK, bizPK)
                .where(() => pi.ProcessId, processId)
                .where(() => pi.RecordStatusInvalid, (byte)0)
                .whereIn(() => pi.ProcessState, new short?[]
                {
                    (short)ProcessStateEnum.Ready,
                    (short)ProcessStateEnum.Running
                })
                .orderByDesc(() => pi.CreatedDateTime);
            var list = clip.select(pi).queryList().ToList();
            return EnumHelper.GetFirst<ProcessInstanceEntity>(list);
        }

        public bool CheckSubProcessInstanceCompleted(SQLBuilder kit, string activityInstanceId, string activityId)
        {
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.InvokedActInstanceId, activityInstanceId)
                .where(() => pi.InvokedActivityId, activityId)
                .where(() => pi.RecordStatusInvalid, (byte)0)
                .where(() => pi.ProcessState, (short)ProcessStateEnum.Completed)
                .orderByDesc(() => pi.CreatedDateTime);
            var list = clip.select(pi).queryList().ToList();
            return list.Count >= 1;
        }
        #endregion

        #region Write
        public string Insert(ProcessInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return Insert(kit, entity);
        }

        public string Insert(SQLBuilder kit, ProcessInstanceEntity entity)
        {
            var newId = kit.useRepo<ProcessInstanceEntity, string>().InsertReturnId(entity);
            entity.ProcessInstanceId = newId;
            return newId;
        }

        public void Update(ProcessInstanceEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        public void Update(SQLBuilder kit, ProcessInstanceEntity entity)
            => kit.useRepo<ProcessInstanceEntity>().Update(entity);

        public ProcessInstanceEntity CreateNewProcessInstanceObject(
            FlowCommandContext runner,
            ProcessEntity processEntity,
            SubProcessNode subProcessNode = null,
            SQLBuilder kit = null)
        {
            runner?.ApplyBizData();

            var entity = new ProcessInstanceEntity
            {
                ProcessId = processEntity.ProcessId,
                ProcessName = processEntity.ProcessName,
                Version = processEntity.Version,
                Title = null,
                BizType = runner?.BizType,
                BizChannel = runner?.BizChannel,
                BizPK = runner?.BizPK,
                BizCode = runner?.BizCode,
                BizName = runner?.BizName,
                ProcessState = (int)ProcessStateEnum.Running,
                StartUnitId = !string.IsNullOrWhiteSpace(runner?.StartUnitId)
                    ? runner.StartUnitId
                    : runner?.CompanyId,
                StartUnitCode = runner?.StartUnitCode,
                StartUnitName = runner?.StartUnitName,
                CreatedUserId = runner?.UserId,
                CreatedUserName = runner?.UserName,
                CreatedDateTime = DateTime.UtcNow,
                UpdatedUserId = runner?.UserId,
                UpdatedUserName = runner?.UserName,
                UpdatedDateTime = DateTime.UtcNow
            };

            if (subProcessNode != null)
            {
                entity.SubProcessId = subProcessNode.SubProcessId;
                entity.InvokedActivityId = subProcessNode.ActivityInstance.ActivityId;
                entity.InvokedActInstanceId = subProcessNode.ActivityInstance.ActivityInstanceId;
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
                entity.DueAt = CalculateOverdueDateTime(entity.CreatedDateTime.Value, processEntity.EndExpression);

            RunProcessInstanceCreateHooks(kit, runner, processEntity, entity);
            return entity;
        }

        private void RunProcessInstanceCreateHooks(
            SQLBuilder kit,
            FlowCommandContext runner,
            ProcessEntity processEntity,
            ProcessInstanceEntity entity)
        {
            var regs = Sail.Capabilities.ProcessInstanceCreateHooks;
            if (regs.Count == 0) return;

            var ctx = new ProcessInstanceCreateContext
            {
                Runner = runner,
                Process = processEntity,
                Instance = entity,
                Kit = kit
            };

            var log = Sail.Log;
            var warnMs = Sail.CapabilityOptions?.SyncHookWarnMs ?? 0;
            var kitPresent = kit != null;

            foreach (var reg in regs)
            {
                if (reg?.Handler == null) continue;
                if (!MatchesCreateHook(reg, processEntity, entity)) continue;

                var timeoutMs = ResolveCreateHookTimeoutMs(reg);
                var label = CapabilityPluginNames.Display(reg.Name, reg.Title);
                log?.Debug(
                    SailLogCategories.CreateHook,
                    "开始 名称={0} 同事务Kit={1} 超时={2}毫秒 流程={3}",
                    label,
                    kitPresent ? "是" : "否",
                    timeoutMs,
                    processEntity?.ProcessCode ?? processEntity?.ProcessId ?? "-");

                var sw = Stopwatch.StartNew();
                try
                {
                    reg.Handler.OnCreating(ctx);
                    sw.Stop();
                    var elapsed = sw.ElapsedMilliseconds;

                    if (timeoutMs > 0 && elapsed > timeoutMs)
                    {
                        log?.Error(
                            SailLogCategories.CreateHook,
                            string.Format(
                                "失败 名称={0} 耗时={1}毫秒 原因=超时 超时={2}毫秒",
                                label, elapsed, timeoutMs));
                        throw new FlowSyncHookTimeoutException(
                            reg.Name,
                            EventFireTypeEnum.OnProcessStarting,
                            timeoutMs,
                            elapsed,
                            kitPresent);
                    }

                    if (warnMs > 0 && elapsed > warnMs)
                    {
                        log?.Warning(
                            SailLogCategories.CreateHook,
                            "偏慢 名称={0} 耗时={1}毫秒 告警={2}毫秒",
                            label, elapsed, warnMs);
                    }

                    log?.Debug(
                        SailLogCategories.CreateHook,
                        "结束 名称={0} 耗时={1}毫秒",
                        label, elapsed);
                }
                catch (FlowSyncHookTimeoutException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    sw.Stop();
                    log?.Error(
                        SailLogCategories.CreateHook,
                        ex,
                        "失败 名称={0} 耗时={1}毫秒",
                        label, sw.ElapsedMilliseconds);
                    throw;
                }
            }
        }

        private int ResolveCreateHookTimeoutMs(ProcessInstanceCreateHookRegistration reg)
        {
            if (reg?.TimeoutMs is int regMs)
                return regMs;
            if (reg?.Handler is ProcessInstanceCreateHookBase bas)
                return bas.TimeoutMs;
            return Sail.CapabilityOptions?.SyncHookTimeoutMs ?? 0;
        }

        private static bool MatchesCreateHook(
            ProcessInstanceCreateHookRegistration reg,
            ProcessEntity processEntity,
            ProcessInstanceEntity entity)
        {
            // 与 Gate/Hook 差异：创建 Hook 允许「仅 BizChannels/BizTypes」过滤。
            // ProcessCodes/ProcessIds 皆空 → 流程维不限制（避免默认 Whitelist+空名单永远不触发）。
            var hasProcessFilter =
                (reg.ProcessCodes?.Length ?? 0) > 0 || (reg.ProcessIds?.Length ?? 0) > 0;
            if (hasProcessFilter
                && !FlowCapabilityMatch.MatchesProcess(
                    reg.Match,
                    reg.ProcessCodes,
                    reg.ProcessIds,
                    processEntity?.ProcessCode,
                    processEntity?.ProcessId))
                return false;

            if (!FlowCapabilityMatch.MatchesOptionalList(reg.BizChannels, entity?.BizChannel))
                return false;

            if (!FlowCapabilityMatch.MatchesOptionalList(reg.BizTypes, entity?.BizType))
                return false;

            return true;
        }

        private static DateTime CalculateOverdueDateTime(DateTime current, string expression)
        {
            var timeSpan = System.Xml.XmlConvert.ToTimeSpan(expression);
            return current.Add(timeSpan);
        }
        #endregion

        #region Business rules
        public ProcessInstanceEntity Complete(string processInstanceId, FlowCommandContext runner)
        {
            var kit = Sail.UseRunSQL();
            return Complete(kit, processInstanceId, runner);
        }

        public ProcessInstanceEntity Complete(SQLBuilder kit, string processInstanceId, FlowCommandContext runner)
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
                throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.complete.error")), "Complete");
            }
            return processInstance;
        }

        public void Suspend(string processInstanceId, FlowCommandContext runner)
        {
            var kit = Sail.UseRunSQL();
            Suspend(kit, processInstanceId, runner);
        }

        public void Suspend(SQLBuilder kit, string processInstanceId, FlowCommandContext runner)
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
                throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.suspend.error")), "Suspend");
            }
        }

        public void Resume(string processInstanceId, FlowCommandContext runner)
        {
            var kit = Sail.UseRunSQL();
            Resume(kit, processInstanceId, runner);
        }

        public void Resume(SQLBuilder kit, string processInstanceId, FlowCommandContext runner)
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
                throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.resume.error")), "Resume");
            }
        }


        public void RecallSubProcess(SQLBuilder kit, string invokedActivityInstanceId, FlowCommandContext runner)
        {
            var clip = kit.useClip();
            clip.from<ProcessInstanceEntity>(out var pi)
                .where(() => pi.InvokedActInstanceId, invokedActivityInstanceId)
                .where(() => pi.ProcessState, (short)ProcessStateEnum.Suspended)
                .orderByDesc(() => pi.CreatedDateTime);
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
                throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.recallsubprocess.error")), "RecallSubProcess");
            }
        }


        public void Reverse(SQLBuilder kit, string processInstanceId, FlowCommandContext currentUser)
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
                throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.reverse.error")), "Reverse");
            }
        }

        public bool Cancel(FlowCommandContext runner)
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

        public bool Cancel(SQLBuilder kit, FlowCommandContext runner)
        {
            try
            {
                var entity = GetProcessInstanceLatest(kit, runner.BizPK, runner.ProcessId, runner.Version);
                if (entity == null || entity.ProcessState != (short)ProcessStateEnum.Running)
                    throw FailThrow(new WorkflowException(Sail.Messages.GetEngineMessage("processinstancemanager.cancel.error")), "Cancel");

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
                throw FailThrow(new WorkflowException(Sail.Messages.GetEngineMessage("processinstancemanager.cancel.error", e.Message)), "Cancel");
            }
        }

        public bool Discard(FlowCommandContext runner)
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
                    throw FailThrow(new WorkflowException(Sail.Messages.GetEngineMessage("processinstancemanager.discard.error", e.Message)), "Discard");
                }
            }
        }

        public void Discard(SQLBuilder kit, FlowCommandContext runner)
        {
            var clip = kit.useClip();
            clip.setTable<ProcessInstanceEntity>(out var pi)
                .set(() => pi.ProcessState, (short)ProcessStateEnum.Discarded)
                .set(() => pi.RecordStatusInvalid, (byte)1)
                .set(() => pi.UpdatedDateTime, DateTime.UtcNow)
                .set(() => pi.UpdatedUserId, runner.UserId)
                .set(() => pi.UpdatedUserName, runner.UserName)
                .where(() => pi.BizPK, runner.BizPK)
                .where(() => pi.ProcessId, runner.ProcessId)
                .where(() => pi.Version, runner.Version)
                .doUpdate();
        }

        public bool Terminate(FlowCommandContext runner)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    ProcessInstanceEntity entity;
                    if (!string.IsNullOrWhiteSpace(runner.ProcessInstanceId))
                        entity = GetById(kit, runner.ProcessInstanceId);
                    else
                        entity = GetProcessInstanceLatest(kit, runner.BizPK, runner.ProcessId, runner.Version);
                    var ok = Terminate(kit, entity, runner);
                    kit.commit();
                    return ok;
                }
                catch (System.Exception ex)
                {
                    throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.terminate.error", ex.Message)), "Terminate");
                }
            }
        }

        public bool Terminate(SQLBuilder kit, ProcessInstanceEntity entity, string userId, string userName)
            => Terminate(kit, entity, new FlowCommandContext { UserId = userId, UserName = userName });

        public bool Terminate(SQLBuilder kit, ProcessInstanceEntity entity, FlowCommandContext runner)
        {
            if (entity == null)
                throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.terminate.error")), "Terminate");

            var userId = runner?.UserId;
            var userName = runner?.UserName;

            if (entity.ProcessState == (int)ProcessStateEnum.Running
                || entity.ProcessState == (int)ProcessStateEnum.Ready
                || entity.ProcessState == (int)ProcessStateEnum.Suspended)
            {
                entity.ProcessState = (short)ProcessStateEnum.Terminated;
                entity.EndedUserId = userId;
                entity.EndedUserName = userName;
                entity.EndedDateTime = DateTime.UtcNow;
                Update(kit, entity);

                // 级联收口未完结活动与待办，避免实例已终止但待办仍可办
                var aiClip = kit.useClip();
                aiClip.setTable<ActivityInstanceEntity>(out var ai)
                    .set(() => ai.ActivityState, (short)ActivityStateEnum.Terminated)
                    .set(() => ai.EndedDateTime, DateTime.UtcNow)
                    .set(() => ai.EndedUserId, userId)
                    .set(() => ai.EndedUserName, userName)
                    .where(() => ai.ProcessInstanceId, entity.ProcessInstanceId)
                    .whereIn(() => ai.ActivityState, new short?[]
                    {
                        (short)ActivityStateEnum.Ready,
                        (short)ActivityStateEnum.Running,
                        (short)ActivityStateEnum.Suspended
                    })
                    .doUpdate();

                // 先读出待取消待办，批量 update 后再发 Cancelled（与 Withdraw/SendBack 同旁路）
                var openTaskClip = kit.useClip();
                openTaskClip.from<TaskEntity>(out var ot)
                    .where(() => ot.ProcessInstanceId, entity.ProcessInstanceId)
                    .whereIn(() => ot.TaskState, new short?[]
                    {
                        (short)TaskStateEnum.Waiting,
                        (short)TaskStateEnum.Reading
                    });
                var openTasks = openTaskClip.select(ot).queryList()?.ToList()
                    ?? new List<TaskEntity>();

                var taskClip = kit.useClip();
                taskClip.setTable<TaskEntity>(out var t)
                    .set(() => t.TaskState, (short)TaskStateEnum.Canceled)
                    .set(() => t.RecordStatusInvalid, (byte)1)
                    .set(() => t.CompletedAt, DateTime.UtcNow)
                    .set(() => t.EndedUserId, userId)
                    .set(() => t.EndedUserName, userName)
                    .where(() => t.ProcessInstanceId, entity.ProcessInstanceId)
                    .whereIn(() => t.TaskState, new short?[]
                    {
                        (short)TaskStateEnum.Waiting,
                        (short)TaskStateEnum.Reading
                    })
                    .doUpdate();

                foreach (var task in openTasks)
                {
                    task.TaskState = (short)TaskStateEnum.Canceled;
                    task.RecordStatusInvalid = 1;
                    task.CompletedAt = DateTime.UtcNow;
                    task.EndedUserId = userId;
                    task.EndedUserName = userName;
                    Sail.CapabilityDispatcher.FireTaskLifecycle(
                        kit, task, activityInstance: null, runner, TaskNotifyKind.Cancelled);
                }

                return true;
            }
            throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.terminate.error")), "Terminate");
        }

        public bool SetOverdue(string processInstanceId, DateTime overdueDateTime, FlowCommandContext runner)
        {
            var kit = Sail.UseRunSQL();
            return SetOverdue(kit, processInstanceId, overdueDateTime, runner);
        }

        public bool SetOverdue(SQLBuilder kit, string processInstanceId, DateTime overdueDateTime, FlowCommandContext runner)
        {
            var entity = GetById(kit, processInstanceId);
            if (entity.ProcessState == (int)ProcessStateEnum.Running
                || entity.ProcessState == (int)ProcessStateEnum.Ready
                || entity.ProcessState == (int)ProcessStateEnum.Suspended)
            {
                entity.DueAt = overdueDateTime;
                entity.UpdatedUserId = runner.UserId;
                entity.UpdatedUserName = runner.UserName;
                entity.UpdatedDateTime = DateTime.UtcNow;
                Update(kit, entity);
                return true;
            }
            throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.setoverdue.error")), "SetOverdue");
        }

        public bool Delete(string processInstanceId, bool isForced = false)
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

        public bool Delete(SQLBuilder kit, string processInstanceId, bool isForced = false)
        {
            var entity = GetById(kit, processInstanceId);
            if (entity.ProcessState == (int)ProcessStateEnum.Canceled || isForced)
            {
                var clip = kit.useClip();
                clip.setTable<TaskEntity>(out var t)
                    .where(() => t.ProcessInstanceId, processInstanceId)
                    .doDelete();

                DeleteTransitionsByProcessInstance(kit, Sail.HotShard.ResolveTransitionTable(entity.ProcessId), processInstanceId);
                if (Sail.HotShard.IsSharded(entity.ProcessId))
                    DeleteTransitionsByProcessInstance(kit, Sail.HotShard.DefaultTransitionTable, processInstanceId);

                clip = kit.useClip();
                clip.setTable<ActivityInstanceEntity>(out var ai)
                    .where(() => ai.ProcessInstanceId, processInstanceId)
                    .doDelete();

                DeleteVariablesByProcessInstance(kit, Sail.HotShard.ResolveVariableTable(entity.ProcessId), processInstanceId);
                if (Sail.HotShard.IsSharded(entity.ProcessId))
                    DeleteVariablesByProcessInstance(kit, Sail.HotShard.DefaultVariableTable, processInstanceId);

                kit.useRepo<ProcessInstanceEntity, string>().DeleteById(processInstanceId);
                return true;
            }
            throw FailThrow(new ProcessInstanceException(Sail.Messages.GetEngineMessage("processinstancemanager.delete.error")), "Delete");
        }

        static void DeleteTransitionsByProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId)
        {
            try
            {
                var clip = kit.useClip();
                clip.setTable<TrackEntity>(out var t)
                    .useSQL(b => b.setTable(physicalTable))
                    .where(() => t.ProcessInstanceId, processInstanceId)
                    .doDelete();
            }
            catch
            {
                // 分表未创建时忽略
            }
        }

        static void DeleteVariablesByProcessInstance(SQLBuilder kit, string physicalTable, string processInstanceId)
        {
            try
            {
                var clip = kit.useClip();
                clip.setTable<ProcessVariableEntity>(out var v)
                    .useSQL(b => b.setTable(physicalTable))
                    .where(() => v.ProcessInstanceId, processInstanceId)
                    .doDelete();
            }
            catch
            {
                // 分表未创建时忽略
            }
        }

        public bool Delete(string processId, string version)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var entity = GetByVersion(kit, processId, version);
                    if (entity == null)
                        return false;
                    var ok = Delete(kit, entity.ProcessInstanceId, true);
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
