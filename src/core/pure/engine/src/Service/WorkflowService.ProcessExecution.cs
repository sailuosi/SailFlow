using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using sailflow.data;
using sailflow.hub.Localize;
using sailflow.engine.Common;
using sailflow.engine.Xpdl;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Result;
using sailflow.engine.Event;
using sailflow.engine.Runtime;
using sailflow.engine.Client;
using mooSQL.data;
using sailflow.engine.Runtime;
using sailflow.engine.Hooks;
using sailflow.engine.Actions.Assignee;
using sailflow.engine.Actions.Lifecycle;
using sailflow.engine.Actions.Transition;

namespace sailflow.engine.Service
{
    /// <summary>
    /// Workflow Service - Process Execution
    /// 工作流服务 - 流程执行部分
    /// </summary>
    public partial class WorkflowService : IWorkflowService
    {
        /// <summary>无状态：开事务执行，成功才 commit。</summary>
        private WfExecutedResult CommitOnSuccess(Func<SQLBuilder, WfExecutedResult> action)
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            var result = action(kit);
            if (result.Status == WfExecutedStatus.Success)
                kit.commit();
            return result;
        }

        private async Task<WfExecutedResult> CommitOnSuccessAsync(Func<SQLBuilder, Task<WfExecutedResult>> action)
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            var result = await action(kit).ConfigureAwait(false);
            if (result.Status == WfExecutedStatus.Success)
                kit.commit();
            return result;
        }
        static string ResolveInstanceLockKey(FlowCommandContext runner)
            => string.IsNullOrEmpty(runner?.BizPK) ? runner?.ProcessId : runner.BizPK;

        /// <summary>
        /// 办理动词 Async 共用：开事务 → await 实例锁 → 同步 Core → 成功则提交。
        /// 范围收窄：除 Run/RunAuto 外，动词不扩 Factory 预载 async；Core 内勿再 Acquire。
        /// </summary>
        async Task<WfExecutedResult> RunVerbAsync(
            FlowCommandContext runner,
            CancellationToken cancellationToken,
            Func<SQLBuilder, FlowCommandContext, WfExecutedResult> core)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await CommitOnSuccessAsync(async kit =>
            {
                WfExecutedResult result;
                using (await InstanceExecutionLock.AcquireAsync(ResolveInstanceLockKey(runner), cancellationToken)
                    .ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    result = core(kit, runner);
                }
                return result;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// A1：动作骨架入口——事务 + 实例锁在 Service；Action 只吃已开 kit（无事务语义）。
        /// </summary>
        WfExecutedResult RunAction(FlowCommandContext runner, Func<SQLBuilder, FlowCommandContext, WfExecutedResult> execute) =>
            CommitOnSuccess(kit =>
            {
                using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                    return execute(kit, runner);
            });

        Task<WfExecutedResult> RunActionAsync(
            FlowCommandContext runner,
            CancellationToken cancellationToken,
            Func<SQLBuilder, FlowCommandContext, WfExecutedResult> execute) =>
            RunVerbAsync(runner, cancellationToken, execute);

        #region Process Startup
        /// <summary>
        /// 在已有事务 kit 上启动。入口请用 <see cref="StartProcess"/> 或 <c>Sail.CreateRunBuilder(...).Start()</c>。
        /// </summary>
        public WfExecutedResult Start(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult startedResult = WfExecutedResult.Default();
            try
            {
                using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                    startedResult = ExecuteStartCore(kit, runner);
            }
            catch (System.Exception e)
            {
                startedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                startedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.startprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_START_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return startedResult;
        }

        async Task<WfExecutedResult> StartAsync(SQLBuilder kit, FlowCommandContext runner, CancellationToken cancellationToken)
        {
            WfExecutedResult startedResult = WfExecutedResult.Default();
            try
            {
                using (await InstanceExecutionLock.AcquireAsync(ResolveInstanceLockKey(runner), cancellationToken)
                    .ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    startedResult = ExecuteStartCore(kit, runner);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                startedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                startedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.startprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_START_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return startedResult;
        }

        WfExecutedResult ExecuteStartCore(SQLBuilder kit, FlowCommandContext runner)
        {
            var startedResult = WfExecutedResult.Default();
            var runtimeInstance = Runtimes.CreateRuntimeInstanceStartup(runner, kit, ref startedResult);

            if (startedResult.Status == WfExecutedStatus.Exception)
                return startedResult;

            var gateCtx = FlowCapabilityPipeline.BuildGateContext(
                Sail,
                kit,
                runner,
                null,
                runtimeInstance.RunningActivityInstance,
                null,
                FlowOperation.Start);
            var startActivity = runtimeInstance.ProcessModel?.GetStartActivity();
            if (startActivity != null)
            {
                gateCtx.ActivityId = startActivity.ActivityId;
                gateCtx.ActivityCode = startActivity.ActivityCode;
            }

            if (!FlowCapabilityPipeline.TryGate(Sail, kit, gateCtx, startedResult))
                return startedResult;

            runtimeInstance.Execute(kit);
            startedResult = runtimeInstance.WfExecutedResult;
            if (startedResult.Status == WfExecutedStatus.Success)
            {
                var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                    Sail, kit, runner,
                    startedResult.ProcessInstanceIdStarted,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    EventFireTypeEnum.OnProcessStarted,
                    FlowOperation.Start);
                FlowCapabilityPipeline.AfterSuccess(
                    Sail, kit, hookCtx, EventFireTypeEnum.OnProcessStarted);
            }
            return startedResult;
        }

        /// <summary>
        /// Start process
        /// </summary>
        public WfExecutedResult StartProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => Start(kit, runner));

        /// <summary>
        /// Start process async：可取消等待实例锁；Mediator 办理仍同步（阶段 2）。
        /// </summary>
        public async Task<WfExecutedResult> StartProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await CommitOnSuccessAsync(kit => StartAsync(kit, runner, cancellationToken)).ConfigureAwait(false);
        }

        #endregion

        #region Process Startup by Message
        /// <summary>
        /// Start by Message
        /// 消息启动流程
        /// </summary>

        public WfExecutedResult StartByMessage(SQLBuilder kit, FlowCommandContext runner)
        {
            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetByMessage(runner.MessageTopic);
            runner.ProcessId = entity.ProcessId;
            runner.ProcessCode = entity.ProcessCode;
            runner.Version = entity.Version;
            runner.UserId = WfDefine.SYSTEM_INTERNAL_USER_ID;
            runner.UserName = WfDefine.SYSTEM_INTERNAL_USER_NAME;
            return Start(kit, runner);
        }

        /// <summary>
        /// Start by Message
        /// 流程流转
        /// </summary>
        public WfExecutedResult StartProcessByMessage(FlowCommandContext runner)
            => CommitOnSuccess(kit => StartByMessage(kit, runner));

        /// <summary>
        /// Start by Message Async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public async Task<WfExecutedResult> StartProcessByMessageAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await CommitOnSuccessAsync(async kit =>
            {
                var pm = Sail.ManagerFactory.Processes;
                var entity = pm.GetByMessage(runner.MessageTopic);
                runner.ProcessId = entity.ProcessId;
                runner.ProcessCode = entity.ProcessCode;
                runner.Version = entity.Version;
                runner.UserId = WfDefine.SYSTEM_INTERNAL_USER_ID;
                runner.UserName = WfDefine.SYSTEM_INTERNAL_USER_NAME;
                return await StartAsync(kit, runner, cancellationToken).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        #endregion

        #region Process Run
        /// <summary>
        /// Running process
        /// Coding example:
        /// var wfResult = sail.CreateRunBuilder(...).UseBiz(...).UseProcess(...).Run();
        /// 或 workflow.RunProcess(runner);
        /// </summary>

        public WfExecutedResult Run(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult runAppResult = WfExecutedResult.Default();
            try
            {
                using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                    runAppResult = ExecuteRunCore(kit, runner);
            }
            catch (System.Exception e)
            {
                runAppResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                runAppResult.Message = Sail.Messages.GetEngineMessage("workflowservice.runprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return runAppResult;
        }

        async Task<WfExecutedResult> RunAsync(SQLBuilder kit, FlowCommandContext runner, CancellationToken cancellationToken)
        {
            WfExecutedResult runAppResult = WfExecutedResult.Default();
            try
            {
                using (await InstanceExecutionLock.AcquireAsync(ResolveInstanceLockKey(runner), cancellationToken)
                    .ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    runAppResult = await ExecuteRunCoreAsync(kit, runner, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                runAppResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                runAppResult.Message = Sail.Messages.GetEngineMessage("workflowservice.runprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return runAppResult;
        }

        WfExecutedResult ExecuteRunCore(SQLBuilder kit, FlowCommandContext runner)
        {
            var runAppResult = WfExecutedResult.Default();
            var runtimeInstance = Runtimes.CreateRuntimeInstanceAppRunning(runner, kit, ref runAppResult);
            return FinishRunCore(kit, runner, runtimeInstance, runAppResult);
        }

        async Task<WfExecutedResult> ExecuteRunCoreAsync(SQLBuilder kit, FlowCommandContext runner, CancellationToken cancellationToken)
        {
            var (runtimeInstance, runAppResult) = await Runtimes
                .CreateRuntimeInstanceAppRunningAsync(runner, kit, cancellationToken)
                .ConfigureAwait(false);
            return FinishRunCore(kit, runner, runtimeInstance, runAppResult);
        }

        WfExecutedResult FinishRunCore(
            SQLBuilder kit,
            FlowCommandContext runner,
            WfRuntimeManager runtimeInstance,
            WfExecutedResult runAppResult)
        {
            if (runAppResult.Status == WfExecutedStatus.Exception)
                return runAppResult;

            var gateCtx = FlowCapabilityPipeline.BuildGateContext(
                Sail,
                kit,
                runner,
                runtimeInstance.ProcessInstanceId,
                runtimeInstance.RunningActivityInstance,
                runtimeInstance.TaskView?.TaskId,
                FlowOperation.Run);
            if (!FlowCapabilityPipeline.TryGate(Sail, kit, gateCtx, runAppResult))
                return runAppResult;

            runtimeInstance.Execute(kit);
            runAppResult = runtimeInstance.WfExecutedResult;
            if (runAppResult.Status == WfExecutedStatus.Success)
            {
                var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                    Sail, kit, runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    EventFireTypeEnum.OnProcessContinued,
                    FlowOperation.Run);
                FlowCapabilityPipeline.AfterSuccess(
                    Sail, kit, hookCtx, EventFireTypeEnum.OnProcessContinued);
            }
            return runAppResult;
        }

        /// <summary>
        /// Process Run
        /// Explanation: The new method uniformly calls RunProcess()
        /// 流程流转
        /// 说明：新方法统一调用 RunProcess()
        /// </summary>
        public WfExecutedResult RunProcessApp(FlowCommandContext runner)
            => CommitOnSuccess(kit => Run(kit, runner));

        /// <summary>
        /// Run Process
        /// 流程流转
        /// </summary>
        public WfExecutedResult RunProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => Run(kit, runner));

        /// <summary>
        /// Run Process Async
        /// 异步执行流程
        /// </summary>
        /// <summary>
        /// Run async：可取消等待实例锁；Factory 预载 IO-await；Mediator Execute 仍同步（阶段 3 首刀）。
        /// </summary>
        public async Task<WfExecutedResult> RunProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await CommitOnSuccessAsync(kit => RunAsync(kit, runner, cancellationToken)).ConfigureAwait(false);
        }

        #endregion

        #region Process Run Auto
        /// <summary>
        /// Run Automatically
        /// 自动运行
        /// </summary>

        public WfExecutedResult RunAuto(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult runAppResult = WfExecutedResult.Default();
            try
            {
                using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                    runAppResult = ExecuteRunAutoCore(kit, runner);
            }
            catch (System.Exception e)
            {
                runAppResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                runAppResult.Message = Sail.Messages.GetEngineMessage("workflowservice.runprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return runAppResult;
        }

        async Task<WfExecutedResult> RunAutoAsync(SQLBuilder kit, FlowCommandContext runner, CancellationToken cancellationToken)
        {
            WfExecutedResult runAppResult = WfExecutedResult.Default();
            try
            {
                using (await InstanceExecutionLock.AcquireAsync(ResolveInstanceLockKey(runner), cancellationToken)
                    .ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    runAppResult = await ExecuteRunAutoCoreAsync(kit, runner, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                runAppResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                runAppResult.Message = Sail.Messages.GetEngineMessage("workflowservice.runprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return runAppResult;
        }

        WfExecutedResult ExecuteRunAutoCore(SQLBuilder kit, FlowCommandContext runner)
        {
            var runAppResult = WfExecutedResult.Default();
            var runtimeInstance = Runtimes.CreateRuntimeInstanceRunAuto(runner, kit, ref runAppResult);
            return FinishRunAutoCore(kit, runner, runtimeInstance, runAppResult);
        }

        async Task<WfExecutedResult> ExecuteRunAutoCoreAsync(SQLBuilder kit, FlowCommandContext runner, CancellationToken cancellationToken)
        {
            var (runtimeInstance, runAppResult) = await Runtimes
                .CreateRuntimeInstanceRunAutoAsync(runner, kit, cancellationToken)
                .ConfigureAwait(false);
            return FinishRunAutoCore(kit, runner, runtimeInstance, runAppResult);
        }

        WfExecutedResult FinishRunAutoCore(
            SQLBuilder kit,
            FlowCommandContext runner,
            WfRuntimeManager runtimeInstance,
            WfExecutedResult runAppResult)
        {
            if (runAppResult.Status == WfExecutedStatus.Exception)
                return runAppResult;

            runtimeInstance.Execute(kit);
            runAppResult = runtimeInstance.WfExecutedResult;
            if (runAppResult.Status == WfExecutedStatus.Success)
            {
                var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                    Sail, kit, runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    EventFireTypeEnum.OnProcessContinued,
                    FlowOperation.Run);
                FlowCapabilityPipeline.AfterSuccess(
                    Sail, kit, hookCtx, EventFireTypeEnum.OnProcessContinued);
            }
            return runAppResult;
        }

        /// <summary>
        /// Run process auto
        /// 流程执行自动
        /// </summary>
        public WfExecutedResult RunProcessAuto(FlowCommandContext runner)
            => CommitOnSuccess(kit => RunAuto(kit, runner));

        /// <summary>
        /// Run process auto async：可取消等待实例锁；Factory 预载 IO-await；Mediator Execute 仍同步（阶段 3 首刀）。
        /// </summary>
        public async Task<WfExecutedResult> RunProcessAutoAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await CommitOnSuccessAsync(kit => RunAutoAsync(kit, runner, cancellationToken)).ConfigureAwait(false);
        }

        #endregion

        #region Process Withdraw
        /// <summary>在已有事务 kit 上撤回（带锁）。Transition 族 <see cref="WithdrawAction"/>。</summary>
        public WfExecutedResult Withdraw(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new WithdrawAction(Sail, kit, runner).Execute();
        }

        /// <summary>流程撤回 → Transition 族动作骨架。</summary>
        public WfExecutedResult WithdrawProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new WithdrawAction(Sail, kit, r).Execute());

        /// <summary>Process withdraw async：可取消等待实例锁；Action/Mediator 仍同步。</summary>
        public Task<WfExecutedResult> WithdrawProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new WithdrawAction(Sail, kit, r).Execute());

        #endregion

        #region Process Sendback
        /// <summary>
        /// 在已有事务 kit 上退回（带锁）。A3：横切由 <see cref="SendBackAction"/> / Transition 族固化。
        /// </summary>
        public WfExecutedResult SendBack(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new SendBackAction(Sail, kit, runner).Execute();
        }

        /// <summary>流程退回 → Transition 族动作骨架。</summary>
        public WfExecutedResult SendBackProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new SendBackAction(Sail, kit, r).Execute());

        /// <summary>Process sendback async：可取消等待实例锁；Action/Mediator 仍同步。</summary>
        public Task<WfExecutedResult> SendBackProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new SendBackAction(Sail, kit, r).Execute());

        #endregion

        #region Process Resend
        /// <summary>
        /// Process Resend
        /// 流程转发
        /// </summary>

        public WfExecutedResult Resend(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteResendCore(kit, runner);
        }

        WfExecutedResult ExecuteResendCore(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult resendResult = WfExecutedResult.Default();

            //Resend start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = Runtimes.CreateRuntimeInstanceResend(runner, ref resendResult);

                if (resendResult.Status == WfExecutedStatus.Exception)
                {
                    return resendResult;
                }

                var resendGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    FlowOperation.Resend);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, resendGateCtx, resendResult))
                    return resendResult;

                runtimeInstance.Execute(kit);
                    resendResult = runtimeInstance.WfExecutedResult;
                    if (resendResult.Status == WfExecutedStatus.Success)
                    {
                        var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                            Sail, kit, runner,
                            runtimeInstance.ProcessInstanceId,
                            runtimeInstance.RunningActivityInstance,
                            runtimeInstance.TaskView?.TaskId,
                            EventFireTypeEnum.OnProcessResent,
                            FlowOperation.Resend);
                        FlowCapabilityPipeline.AfterSuccess(
                            Sail, kit, hookCtx, EventFireTypeEnum.OnProcessResent);
                    }
            }
            catch (System.Exception e)
            {
                resendResult.Status = WfExecutedStatus.Failed;
                // 优先带 sailflow 栈帧，避免只剩 InnerException 的 NRE 原文
                var frame = FlattenExceptionFrames(e);
                var error = string.IsNullOrEmpty(frame) ? (e.InnerException?.Message ?? e.Message) : frame;
                resendResult.Message = Sail.Messages.GetEngineMessage("workflowservice.resendprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RESEND_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return resendResult;
        }

        static string FlattenExceptionFrames(Exception e)
        {
            if (e == null) return null;
            var parts = new System.Collections.Generic.List<string>();
            for (var cur = e; cur != null; cur = cur.InnerException)
            {
                parts.Add(cur.GetType().Name + ": " + cur.Message);
                if (cur.StackTrace == null) continue;
                foreach (var line in cur.StackTrace.Split('\n'))
                {
                    var t = line.Trim();
                    if (t.Contains("sailflow", StringComparison.OrdinalIgnoreCase))
                    {
                        parts.Add(t);
                        break;
                    }
                }
            }
            return string.Join(" | ", parts);
        }

        /// <summary>
        /// Process Resend
        /// 重新转发（退回到的节点）
        /// </summary>
        public WfExecutedResult ResendProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => Resend(kit, runner));

        /// <summary>
        /// Process Resend Async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> ResendProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteResendCore);

        #endregion

        #region Process Revise
        /// <summary>
        /// Process revise
        /// (Reassignment of tasks to returned nodes)
        /// 流程修订
        /// (�˻غ�Ľڵ�����ָ������)
        /// </summary>

        public WfExecutedResult Revise(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteReviseCore(kit, runner);
        }

        WfExecutedResult ExecuteReviseCore(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult reviseResult = WfExecutedResult.Default();

            //Revise method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = Runtimes.CreateRuntimeInstanceRevise(runner, ref reviseResult);

                if (reviseResult.Status == WfExecutedStatus.Success)
                {
                    return reviseResult;
                }

                var reviseGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    FlowOperation.Revise);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, reviseGateCtx, reviseResult))
                    return reviseResult;

                runtimeInstance.Execute(kit);
                    reviseResult = runtimeInstance.WfExecutedResult;
                    if (reviseResult.Status == WfExecutedStatus.Success)
                    {
                        var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                            Sail, kit, runner,
                            runtimeInstance.ProcessInstanceId,
                            runtimeInstance.RunningActivityInstance,
                            runtimeInstance.TaskView?.TaskId,
                            EventFireTypeEnum.OnProcessRevised,
                            FlowOperation.Revise);
                        FlowCapabilityPipeline.AfterSuccess(
                            Sail, kit, hookCtx, EventFireTypeEnum.OnProcessRevised);
                    }
            }
            catch(System.Exception e)
            {
                reviseResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                reviseResult.Message = Sail.Messages.GetEngineMessage("workflowservice.reviseprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_REVISE_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return reviseResult;
        }

        /// <summary>
        /// Process revise
        /// 流程修订（退回后的节点重新指派任务）
        /// </summary>
        public WfExecutedResult ReviseProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => Revise(kit, runner));

        /// <summary>
        /// Process revise async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> ReviseProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteReviseCore);

        #endregion

        #region Process Reverse
        /// <summary>
        /// Process reverse
        /// (Completed processes can be reversed)
        /// 流程反签
        /// (�Ѿ����������̿��Ա����
        /// </summary>

        public WfExecutedResult Reverse(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteReverseCore(kit, runner);
        }

        WfExecutedResult ExecuteReverseCore(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult reversedResult = WfExecutedResult.Default();

            //Reverse method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = Runtimes.CreateRuntimeInstanceReverse(runner, ref reversedResult);

                if (reversedResult.Status == WfExecutedStatus.Exception)
                {
                    return reversedResult;
                }

                var reverseGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    FlowOperation.Reverse);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, reverseGateCtx, reversedResult))
                    return reversedResult;

                runtimeInstance.Execute(kit);
                    reversedResult = runtimeInstance.WfExecutedResult;
                    if (reversedResult.Status == WfExecutedStatus.Success)
                    {
                        var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                            Sail, kit, runner,
                            runtimeInstance.ProcessInstanceId,
                            runtimeInstance.RunningActivityInstance,
                            runtimeInstance.TaskView?.TaskId,
                            EventFireTypeEnum.OnProcessReversed,
                            FlowOperation.Reverse);
                        FlowCapabilityPipeline.AfterSuccess(
                            Sail, kit, hookCtx, EventFireTypeEnum.OnProcessReversed);
                    }
            }
            catch (System.Exception e)
            {
                reversedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                reversedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.reverseprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_REVERSE_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return reversedResult;
        }

        /// <summary>
        /// Process reverse
        /// 流程反签
        /// </summary>
        public WfExecutedResult ReverseProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => Reverse(kit, runner));

        /// <summary>
        /// Process reverse async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> ReverseProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteReverseCore);

        #endregion

        #region Process Reject
        public WfExecutedResult Reject(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new RejectAction(Sail, kit, runner).Execute();
        }

        public WfExecutedResult RejectProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new RejectAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> RejectProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new RejectAction(Sail, kit, r).Execute());

        #endregion

        #region Process Close
        public WfExecutedResult Close(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new CloseAction(Sail, kit, runner).Execute();
        }

        public WfExecutedResult CloseProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new CloseAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> CloseProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new CloseAction(Sail, kit, r).Execute());

        #endregion

        #region Process Jump
        public WfExecutedResult Jump(SQLBuilder kit, JumpOptionEnum jumpOption, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new JumpAction(Sail, kit, runner, jumpOption).Execute();
        }

        public WfExecutedResult JumpProcess(FlowCommandContext runner, JumpOptionEnum jumpOption = JumpOptionEnum.Default)
            => RunAction(runner, (kit, r) => new JumpAction(Sail, kit, r, jumpOption).Execute());

        public Task<WfExecutedResult> JumpProcessAsync(
            FlowCommandContext runner,
            JumpOptionEnum jumpOption = JumpOptionEnum.Default,
            CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new JumpAction(Sail, kit, r, jumpOption).Execute());

        #endregion

        #region Process Signforward
        /// <summary>
        /// Process Signforward
        /// 流程会签
        /// </summary>

        public WfExecutedResult SignForward(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteSignForwardCore(kit, runner);
        }

        WfExecutedResult ExecuteSignForwardCore(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult signforwardResult = WfExecutedResult.Default();

            //Signforward start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = Runtimes.CreateRuntimeInstanceSignForward(runner, ref signforwardResult);

                if (signforwardResult.Status == WfExecutedStatus.Exception)
                {
                    return signforwardResult;
                }

                var signForwardGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    FlowOperation.SignForward);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, signForwardGateCtx, signforwardResult))
                    return signforwardResult;

                runtimeInstance.Execute(kit);
                    signforwardResult = runtimeInstance.WfExecutedResult;
                    if (signforwardResult.Status == WfExecutedStatus.Success)
                    {
                        var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                            Sail, kit, runner,
                            runtimeInstance.ProcessInstanceId,
                            runtimeInstance.RunningActivityInstance,
                            runtimeInstance.TaskView?.TaskId,
                            EventFireTypeEnum.OnProcessSignForwarded,
                            FlowOperation.SignForward);
                        FlowCapabilityPipeline.AfterSuccess(
                            Sail, kit, hookCtx, EventFireTypeEnum.OnProcessSignForwarded);
                    }
            }
            catch (System.Exception e)
            {
                signforwardResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                signforwardResult.Message = Sail.Messages.GetEngineMessage("workflowservice.signforwardprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_SIGN_FORWARD_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return signforwardResult;
        }

        /// <summary>
        /// Process Signforward
        /// 会签
        /// </summary>
        public WfExecutedResult SignForwardProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => SignForward(kit, runner));

        /// <summary>
        /// Process Signforward Async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> SignForwardProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteSignForwardCore);

        #endregion

        #region Process Transfer
        /// <summary>
        /// 转办：结束本人待办 + 本节点交给他人，不推进下一节点。
        /// A1：横切由 <see cref="TransferAction"/> / <see cref="AssigneeActionBase"/> 固化；本方法只包事务与锁。
        /// </summary>
        public WfExecutedResult TransferProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new TransferAction(Sail, kit, r).Execute());

        /// <summary>
        /// Transfer async：可取消等待实例锁；Action 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> TransferProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new TransferAction(Sail, kit, r).Execute());

        /// <summary>在已有事务 kit 上转办（调用方负责锁时可直接用；带锁请走 <see cref="TransferProcess"/>）。</summary>
        public WfExecutedResult Transfer(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new TransferAction(Sail, kit, runner).Execute();
        }

        #endregion

        #region Process Resume Suspend Cancel Discard Terminate
        /// <summary>A2：挂起（Lifecycle 族）；事务+锁由 <see cref="RunAction"/>。</summary>
        public WfExecutedResult SuspendProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new SuspendAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> SuspendProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new SuspendAction(Sail, kit, r).Execute());

        /// <summary>A2：恢复（Lifecycle 族）。</summary>
        public WfExecutedResult ResumeProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new ResumeAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> ResumeProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new ResumeAction(Sail, kit, r).Execute());

        /// <summary>兼容 bool：按 taskId 挂起。</summary>
        public bool SuspendProcess(string taskId, FlowCommandContext runner)
        {
            var result = RunAction(runner, (kit, r) => new SuspendAction(Sail, kit, r, taskId).Execute());
            return result.Status == WfExecutedStatus.Success;
        }

        /// <summary>兼容 bool：按 processInstanceId 恢复。（历史参数名 activityInstanceId，实为流程实例 Id）</summary>
        public bool ResumeProcess(string processInstanceId, FlowCommandContext runner)
        {
            var result = RunAction(runner, (kit, r) => new ResumeAction(Sail, kit, r, processInstanceId).Execute());
            return result.Status == WfExecutedStatus.Success;
        }

        /// <summary>
        /// Process Cancel
        /// 取消流程
        /// </summary>
        public bool CancelProcess(FlowCommandContext runner)
        {
            var pim = Managers.ProcessInstances;
            return pim.Cancel(runner);
        }

        /// <summary>
        /// Process Discard
        /// 流程流转
        /// </summary>
        public bool DiscardProcess(FlowCommandContext runner)
        {
            var pim = Managers.ProcessInstances;
            return pim.Discard(runner);
        }

        /// <summary>
        /// Process Terminate
        /// 终止流程
        /// </summary>
        public bool TerminateProcess(FlowCommandContext terminator)
        {
            var pim = Managers.ProcessInstances;
            return pim.Terminate(terminator);
        }

        #endregion

        #region Task Approval Agree Refuse
        /// <summary>
        /// Agree
        /// 同意
        /// </summary>
        public void AgreeTask(string taskId)
        {
            var aim = Sail.ManagerFactory.Activities;
            aim.Agree(taskId);
        }


        /// <summary>
        /// Refuse
        /// 拒绝
        /// </summary>
        public void RefuseTask(string taskId)
        {
            var aim = Sail.ManagerFactory.Activities;
            aim.Refuse(taskId);
        }
        #endregion
    }
}
