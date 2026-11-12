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
            => string.IsNullOrEmpty(runner?.AppInstanceId) ? runner?.ProcessId : runner.AppInstanceId;

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
        /// var wfResult = sail.CreateRunBuilder(...).UseApp(...).UseProcess(...).Run();
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
        /// <summary>
        /// Process withdraw
        /// 流程撤回
        /// </summary>

        public WfExecutedResult Withdraw(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteWithdrawCore(kit, runner);
        }

        WfExecutedResult ExecuteWithdrawCore(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult withdrawedResult = WfExecutedResult.Default();

            //Withdraw method begins execution
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = Runtimes.CreateRuntimeInstanceWithdraw(runner, ref withdrawedResult);

                //Not satisfied with withdraw operation, return exception
                if (withdrawedResult.Status == WfExecutedStatus.Exception)
                {
                    return withdrawedResult;
                }

                var withdrawGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    FlowOperation.Withdraw);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, withdrawGateCtx, withdrawedResult))
                    return withdrawedResult;

                runtimeInstance.Execute(kit);
                    withdrawedResult = runtimeInstance.WfExecutedResult;
                    if (withdrawedResult.Status == WfExecutedStatus.Success)
                    {
                        var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                            Sail, kit, runner,
                            runtimeInstance.ProcessInstanceId,
                            runtimeInstance.RunningActivityInstance,
                            runtimeInstance.TaskView?.TaskId,
                            EventFireTypeEnum.OnProcessWithdrawn,
                            FlowOperation.Withdraw);
                        FlowCapabilityPipeline.AfterSuccess(
                            Sail, kit, hookCtx, EventFireTypeEnum.OnProcessWithdrawn);
                    }
            }
            catch (System.Exception e)
            {
                withdrawedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                withdrawedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.withdrawprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_WITHDRAW_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return withdrawedResult;
        }

        /// <summary>
        /// Process withdraw
        /// 流程撤回
        /// </summary>
        public WfExecutedResult WithdrawProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => Withdraw(kit, runner));

        /// <summary>
        /// Process withdraw async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> WithdrawProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteWithdrawCore);

        #endregion

        #region Process Sendback
        /// <summary>
        /// Process sendback
        /// 流程退回
        /// </summary>

        public WfExecutedResult SendBack(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteSendBackCore(kit, runner);
        }

        WfExecutedResult ExecuteSendBackCore(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult sendbackResult = WfExecutedResult.Default();

            //sendback operation
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = Runtimes.CreateRuntimeInstanceSendBack(runner, ref sendbackResult);

                if (sendbackResult.Status == WfExecutedStatus.Exception)
                {
                    return sendbackResult;
                }

                var sendBackGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    FlowOperation.SendBack);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, sendBackGateCtx, sendbackResult))
                    return sendbackResult;

                runtimeInstance.Execute(kit);
                    sendbackResult = runtimeInstance.WfExecutedResult;
                    if (sendbackResult.Status == WfExecutedStatus.Success)
                    {
                        var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                            Sail, kit, runner,
                            runtimeInstance.ProcessInstanceId,
                            runtimeInstance.RunningActivityInstance,
                            runtimeInstance.TaskView?.TaskId,
                            EventFireTypeEnum.OnProcessSendBacked,
                            FlowOperation.SendBack);
                        FlowCapabilityPipeline.AfterSuccess(
                            Sail, kit, hookCtx, EventFireTypeEnum.OnProcessSendBacked);
                    }
            }
            catch (System.Exception e)
            {
                sendbackResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                sendbackResult.Message = Sail.Messages.GetEngineMessage("workflowservice.sendbackprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_SENDBACK_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return sendbackResult;
        }

        /// <summary>
        /// Process sendback
        /// 退回到下一步
        /// </summary>
        public WfExecutedResult SendBackProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => SendBack(kit, runner));

        /// <summary>
        /// Process sendback async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> SendBackProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteSendBackCore);

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
        /// <summary>
        /// Process reject
        /// 流程驳回
        /// </summary>

        public WfExecutedResult Reject(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteRejectCore(kit, runner);
        }

        WfExecutedResult ExecuteRejectCore(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult rejectedResult = WfExecutedResult.Default();

            //Reject method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runner.NextActivityPerformers = FillNextActivityPerforms(runner, JumpOptionEnum.Startup);
                runtimeInstance = Runtimes.CreateRuntimeInstanceReject(runner, ref rejectedResult);
                if (rejectedResult.Status == WfExecutedStatus.Exception)
                {
                    return rejectedResult;
                }

                var rejectGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    FlowOperation.Reject);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, rejectGateCtx, rejectedResult))
                    return rejectedResult;

                runtimeInstance.Execute(kit);
                    rejectedResult = runtimeInstance.WfExecutedResult;
                    if (rejectedResult.Status == WfExecutedStatus.Success)
                    {
                        var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                            Sail, kit, runner,
                            runtimeInstance.ProcessInstanceId,
                            runtimeInstance.RunningActivityInstance,
                            runtimeInstance.TaskView?.TaskId,
                            EventFireTypeEnum.OnProcessRejected,
                            FlowOperation.Reject);
                        FlowCapabilityPipeline.AfterSuccess(
                            Sail, kit, hookCtx, EventFireTypeEnum.OnProcessRejected);
                    }
            }
            catch (System.Exception e)
            {
                rejectedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                rejectedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.rejectprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_REJECT_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return rejectedResult;
        }

        /// <summary>
        /// Process reject
        /// 流程驳回
        /// </summary>
        public WfExecutedResult RejectProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => Reject(kit, runner));

        /// <summary>
        /// Process reject async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> RejectProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteRejectCore);

        #endregion

        #region Process Close
        /// <summary>
        /// Process Close
        /// 流程关闭
        /// </summary>

        public WfExecutedResult Close(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteCloseCore(kit, runner);
        }

        WfExecutedResult ExecuteCloseCore(SQLBuilder kit, FlowCommandContext runner)
        {
            WfExecutedResult closedResult = WfExecutedResult.Default();
            try
            {
                runner.NextActivityPerformers = FillNextActivityPerforms(runner, JumpOptionEnum.End);
                var runtimeInstance = Runtimes.CreateRuntimeInstanceClose(runner, ref closedResult);
                if (closedResult.Status == WfExecutedStatus.Exception)
                    return closedResult;

                var closeGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    FlowOperation.Close);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, closeGateCtx, closedResult))
                    return closedResult;

                runtimeInstance.Execute(kit);
                closedResult = runtimeInstance.WfExecutedResult;
                if (closedResult.Status == WfExecutedStatus.Success)
                {
                    var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                        Sail, kit, runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.TaskId,
                        EventFireTypeEnum.OnProcessClosed,
                        FlowOperation.Close);
                    FlowCapabilityPipeline.AfterSuccess(
                        Sail, kit, hookCtx, EventFireTypeEnum.OnProcessClosed);
                }
            }
            catch (System.Exception e)
            {
                closedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                closedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.closeprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_CLOSE_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return closedResult;
        }

        /// <summary>
        ///  Process Close
        /// 流程关闭
        /// </summary>
        public WfExecutedResult CloseProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => Close(kit, runner));

        /// <summary>
        /// Process Close Async：可取消等待实例锁；预载/Execute 仍同步（范围收窄）。
        /// </summary>
        public Task<WfExecutedResult> CloseProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteCloseCore);

        #endregion

        #region Process Jump
        /// <summary>
        /// Process Jump
        /// 流程跳转
        /// </summary>

        public WfExecutedResult Jump(SQLBuilder kit, JumpOptionEnum jumpOption, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteJumpCore(kit, runner, jumpOption);
        }

        WfExecutedResult ExecuteJumpCore(SQLBuilder kit, FlowCommandContext runner, JumpOptionEnum jumpOption)
        {
            WfExecutedResult jumpResult = WfExecutedResult.Default();
            try
            {
                runner.NextActivityPerformers = FillNextActivityPerforms(runner, jumpOption);
                var runtimeInstance = Runtimes.CreateRuntimeInstanceJump(runner, ref jumpResult);

                if (jumpResult.Status == WfExecutedStatus.Exception)
                    return jumpResult;

                var jumpGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.TaskId,
                    FlowOperation.Jump);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, jumpGateCtx, jumpResult))
                    return jumpResult;

                runtimeInstance.Execute(kit);
                jumpResult = runtimeInstance.WfExecutedResult;
                if (jumpResult.Status == WfExecutedStatus.Success)
                {
                    var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                        Sail, kit, runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.TaskId,
                        EventFireTypeEnum.OnProcessJumped,
                        FlowOperation.Jump);
                    FlowCapabilityPipeline.AfterSuccess(
                        Sail, kit, hookCtx, EventFireTypeEnum.OnProcessJumped);
                }
            }
            catch (System.Exception e)
            {
                jumpResult.ExceptionType = WfExceptionType.Jump_OtherError;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                jumpResult.Message = Sail.Messages.GetEngineMessage("workflowservice.jumpprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_JUMP_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return jumpResult;
        }

        /// <summary>
        /// Process Jump
        /// 流程跳转
        /// </summary>
        public WfExecutedResult JumpProcess(FlowCommandContext runner,
            JumpOptionEnum jumpOption = JumpOptionEnum.Default)
            => CommitOnSuccess(kit => Jump(kit, jumpOption, runner));

        /// <summary>
        /// Process Jump Async：可取消等待实例锁；预载/Execute 仍同步（范围收窄）。
        /// </summary>
        public Task<WfExecutedResult> JumpProcessAsync(FlowCommandContext runner, JumpOptionEnum jumpOption = JumpOptionEnum.Default, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, (kit, r) => ExecuteJumpCore(kit, r, jumpOption));

        /// <summary>
        /// Regenerate the list of performers for the jump activity
        /// 流程跳转动作执行人员列表
        /// </summary>
        private IDictionary<string, PerformerList> FillNextActivityPerforms(FlowCommandContext runner,
            JumpOptionEnum jumpOption)
        {
            IDictionary<string, PerformerList> nextActivityPerformers = null;
            var pm = Sail.ProcessModelFactory.CreateByProcess(runner.ProcessId, runner.Version);
            if (jumpOption == JumpOptionEnum.Default)
            {
                nextActivityPerformers = runner.NextActivityPerformers;
            }
            else if (jumpOption == JumpOptionEnum.Startup)
            {
                var firstActivity = pm.GetFirstActivity();
                var aim = Sail.ManagerFactory.Activities;
                var firstActivityInstance = aim.GetActivityInstanceLatest(runner.AppInstanceId, runner.ProcessId, firstActivity.ActivityId);
                nextActivityPerformers = new Dictionary<string, PerformerList>();
                var performer = new Performer(firstActivityInstance.CreatedUserId, firstActivityInstance.CreatedUserName);
                var performerList = new PerformerList();
                performerList.Add(performer);
                nextActivityPerformers.Add(firstActivity.ActivityId, performerList);
            }
            else if (jumpOption == JumpOptionEnum.End)
            {
                var endActivity = pm.GetEndActivity();
                nextActivityPerformers = new Dictionary<string, PerformerList>();
                var performer = new Performer(runner.UserId, runner.UserName);
                var performerList = new PerformerList();
                performerList.Add(performer);
                nextActivityPerformers.Add(endActivity.ActivityId, performerList);
            }
            return nextActivityPerformers;
        }
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
        /// </summary>
        public WfExecutedResult TransferProcess(FlowCommandContext runner)
            => CommitOnSuccess(kit => Transfer(kit, runner));

        /// <summary>
        /// Transfer async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> TransferProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteTransferCore);

        public WfExecutedResult Transfer(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteTransferCore(kit, runner);
        }

        WfExecutedResult ExecuteTransferCore(SQLBuilder kit, FlowCommandContext runner)
        {
            var result = WfExecutedResult.Default();

            try
            {
                if (runner == null || runner.TaskId == null)
                {
                    result.Status = WfExecutedStatus.Exception;
                    result.ExceptionType = WfExceptionType.Transfer_ErrorArguments;
                    result.Message = "Transfer requires TaskId.";
                    return result;
                }

                var targets = FlattenTransferTargets(runner);
                if (targets.Count == 0)
                {
                    result.Status = WfExecutedStatus.Exception;
                    result.ExceptionType = WfExceptionType.Transfer_NoneTargets;
                    result.Message = "Transfer requires NextActivityPerformers with at least one user.";
                    return result;
                }

                var tm = Sail.ManagerFactory.Tasks;
                var taskView = tm.GetTaskView(kit, runner.TaskId);
                if (taskView?.ActivityInstanceId == null)
                {
                    result.Status = WfExecutedStatus.Exception;
                    result.ExceptionType = WfExceptionType.Transfer_ErrorArguments;
                    result.Message = "Transfer task not found.";
                    return result;
                }

                var aim = Sail.ManagerFactory.Activities;
                var activityInstance = aim.GetById(kit, taskView.ActivityInstanceId);
                var processInstanceId = activityInstance.ProcessInstanceId;

                var gateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    processInstanceId,
                    activityInstance,
                    runner.TaskId,
                    FlowOperation.Transfer);
                if (!FlowCapabilityPipeline.TryGate(Sail, kit, gateCtx, result))
                    return result;

                var managed = tm.Transfer(kit, runner.TaskId, runner, targets);
                if (managed.Status != WfDataManagedStatusEnum.Success)
                {
                    result.Status = WfExecutedStatus.Exception;
                    result.ExceptionType = MapTransferException(managed.Message);
                    result.Message = managed.Message;
                    return result;
                }

                result.Status = WfExecutedStatus.Success;
                result.Message = "Transfer succeeded.";

                var hookCtx = FlowCapabilityPipeline.BuildHookContext(
                    Sail,
                    kit,
                    runner,
                    processInstanceId,
                    activityInstance,
                    runner.TaskId,
                    EventFireTypeEnum.OnProcessTransferred,
                    FlowOperation.Transfer);
                FlowCapabilityPipeline.AfterSuccess(
                    Sail, kit, hookCtx, EventFireTypeEnum.OnProcessTransferred);
            }
            catch (System.Exception e)
            {
                result.Status = WfExecutedStatus.Failed;
                result.ExceptionType = WfExceptionType.Transfer_RuntimeError;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                result.Message = "TransferProcess error: " + error;
                LogManager.RecordLog(Sail, "WF_PROCESS_TRANSFER_ERROR", LogEventType.Error, LogPriority.High, runner, e);
            }

            return result;
        }

        private static PerformerList FlattenTransferTargets(FlowCommandContext runner)
        {
            var list = new PerformerList();
            if (runner?.NextActivityPerformers == null)
                return list;
            foreach (var kv in runner.NextActivityPerformers)
            {
                if (kv.Value == null) continue;
                foreach (var p in kv.Value)
                {
                    if (p != null && !string.IsNullOrWhiteSpace(p.UserId))
                        list.Add(p);
                }
            }
            return list;
        }

        private static string MapTransferException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.Transfer_RuntimeError;
            if (message.Contains("self", StringComparison.OrdinalIgnoreCase)
                || message.Contains("current user's", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Transfer_NotMineTask;
            if (message.Contains("ready/running", StringComparison.OrdinalIgnoreCase)
                || message.Contains("to-do", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Transfer_NotInRunning;
            if (message.Contains("target", StringComparison.OrdinalIgnoreCase)
                || message.Contains("performer", StringComparison.OrdinalIgnoreCase))
                return WfExceptionType.Transfer_NoneTargets;
            return WfExceptionType.Transfer_RuntimeError;
        }

        #endregion

        #region Process Resume Suspend Cancel Discard Terminate
        /// <summary>
        /// Resume process instance (only applicable to suspended operations)
        /// Suspend (restore) processes, cancel (run) processes, discard processes in progress or completed
        /// 恢复流程实例(只针对已挂起)
        /// 挂起(恢复)流程、取消(运行)流程、撤回执行或执行中的流程
        /// </summary>
        public bool ResumeProcess(string processInstanceId, FlowCommandContext runner)
        {
            bool result = true;
            try
            {
                var pim = Managers.ProcessInstances;
                pim.Resume(processInstanceId, runner);
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
        }
        /// <summary>
        /// Process Suspend
        /// 流程挂起实例
        /// </summary>
        public bool SuspendProcess(string taskId, FlowCommandContext runner)
        {
            bool result = true;
            try
            {
                var pim = Managers.ProcessInstances;
                var taskMng = Sail.ManagerFactory.Tasks;
                TaskEntity taskentity = taskMng.GetTask(taskId);
                pim.Suspend(taskentity.ProcessInstanceId, runner);
            }
            catch (Exception ex)
            {
                result = false;
            }
            return result;
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
