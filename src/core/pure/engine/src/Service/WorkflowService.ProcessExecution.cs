using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Result;
using saiflow.engine.Event;
using saiflow.engine.Runtime;
using saiflow.engine.Client;
using mooSQL.data;
using saiflow.engine.Runtime;
using saiflow.engine.Hooks;

namespace saiflow.engine.Service
{
    /// <summary>
    /// Workflow Service - Process Execution
    /// 工作流服务 - 流程执行部分
    /// </summary>
    public partial class WorkflowService : IWorkflowService
    {
        static string ResolveInstanceLockKey(WfAppRunner runner)
            => string.IsNullOrEmpty(runner?.AppInstanceId) ? runner?.ProcessId : runner.AppInstanceId;

        /// <summary>
        /// 办理动词 Async 共用：开事务 → await 实例锁 → 同步 Core → 成功则提交。
        /// Core 内勿再 Acquire（SemaphoreSlim 不可重入）。
        /// </summary>
        async Task<WfExecutedResult> RunVerbAsync(
            WfAppRunner runner,
            CancellationToken cancellationToken,
            Func<SQLBuilder, WfAppRunner, WfExecutedResult> core)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _wfAppRunner = runner;
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    WfExecutedResult result;
                    using (await InstanceExecutionLock.AcquireAsync(ResolveInstanceLockKey(runner), cancellationToken)
                        .ConfigureAwait(false))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        result = core(kit, runner);
                    }
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        #region Process Startup
        /// <summary>
        /// Process Startup
        /// Coding Example:
        /// var wfResult = wfService.CreateRunner(runner.UserId, runner.UserName)
        ///                 .UseApp(runner.AppInstanceId, runner.AppName, runner.AppInstanceCode)
        ///                 .UseProcess(runner.ProcessId, runner.Version)
        ///                 .Start();
        /// </summary>
        public WfExecutedResult Start()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Start(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult Start(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
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

        async Task<WfExecutedResult> StartAsync(SQLBuilder kit, CancellationToken cancellationToken)
        {
            var runner = _wfAppRunner;
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

        WfExecutedResult ExecuteStartCore(SQLBuilder kit, WfAppRunner runner)
        {
            var startedResult = WfExecutedResult.Default();
            var runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceStartup(Sail, runner, kit, ref startedResult);

            if (startedResult.Status == WfExecutedStatus.Exception)
                return startedResult;

            var gateCtx = FlowCapabilityPipeline.BuildGateContext(
                Sail,
                kit,
                runner,
                0,
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
                    runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult StartProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = Start();

            return result;
        }

        /// <summary>
        /// Start process async：可取消等待实例锁；Mediator 办理仍同步（阶段 2）。
        /// </summary>
        public async Task<WfExecutedResult> StartProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _wfAppRunner = runner;
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = await StartAsync(kit, cancellationToken).ConfigureAwait(false);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        #endregion

        #region Process Startup by Message
        /// <summary>
        /// Start by Message
        /// 消息启动流程
        /// </summary>
        public WfExecutedResult StartByMessage()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = StartByMessage(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult StartByMessage(SQLBuilder kit)
        {
            var pm = new ProcessManager(Sail);
            var entity = pm.GetByMessage(_wfAppRunner.MessageTopic);
            _wfAppRunner.ProcessId = entity.ProcessId;
            _wfAppRunner.ProcessCode = entity.ProcessCode;
            _wfAppRunner.Version = entity.Version;

            _wfAppRunner.UserId = WfDefine.SYSTEM_INTERNAL_USER_ID;
            _wfAppRunner.UserName = WfDefine.SYSTEM_INTERNAL_USER_NAME;

            var result = Start(kit);
            return result;
        }

        /// <summary>
        /// Start by Message
        /// 流程流转
        /// </summary>
        public WfExecutedResult StartProcessByMessage(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = StartByMessage();
            return result;
        }

        /// <summary>
        /// Start by Message Async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public async Task<WfExecutedResult> StartProcessByMessageAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _wfAppRunner = runner;
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var pm = new ProcessManager(Sail);
                    var entity = pm.GetByMessage(_wfAppRunner.MessageTopic);
                    _wfAppRunner.ProcessId = entity.ProcessId;
                    _wfAppRunner.ProcessCode = entity.ProcessCode;
                    _wfAppRunner.Version = entity.Version;
                    _wfAppRunner.UserId = WfDefine.SYSTEM_INTERNAL_USER_ID;
                    _wfAppRunner.UserName = WfDefine.SYSTEM_INTERNAL_USER_NAME;

                    var result = await StartAsync(kit, cancellationToken).ConfigureAwait(false);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        #endregion

        #region Process Run
        /// <summary>
        /// Running process
        /// Coding example:
        /// var wfResult = wfService.CreateRunner(runner.UserId, runner.UserName)
        ///                 .UseApp(runner.AppInstanceId, runner.AppName, runner.AppInstanceCode)
        ///                 .UseProcess(runner.ProcessId, runner.Version)
        ///                 .Run();
        /// </summary>
        public WfExecutedResult Run()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Run(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult Run(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
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

        async Task<WfExecutedResult> RunAsync(SQLBuilder kit, CancellationToken cancellationToken)
        {
            var runner = _wfAppRunner;
            WfExecutedResult runAppResult = WfExecutedResult.Default();
            try
            {
                using (await InstanceExecutionLock.AcquireAsync(ResolveInstanceLockKey(runner), cancellationToken)
                    .ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    runAppResult = ExecuteRunCore(kit, runner);
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

        WfExecutedResult ExecuteRunCore(SQLBuilder kit, WfAppRunner runner)
        {
            var runAppResult = WfExecutedResult.Default();
            var runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceAppRunning(Sail, runner, kit, ref runAppResult);
            if (runAppResult.Status == WfExecutedStatus.Exception)
                return runAppResult;

            var gateCtx = FlowCapabilityPipeline.BuildGateContext(
                Sail,
                kit,
                runner,
                runtimeInstance.ProcessInstanceId,
                runtimeInstance.RunningActivityInstance,
                runtimeInstance.TaskView?.Id,
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
                    runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult RunProcessApp(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = Run();

            return result;
        }

        /// <summary>
        /// Run Process
        /// 流程流转
        /// </summary>
        public WfExecutedResult RunProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = Run();

            return result;
        }

        /// <summary>
        /// Run Process Async
        /// 异步执行流程
        /// </summary>
        /// <summary>
        /// Run async：可取消等待实例锁；Mediator 办理仍同步（阶段 2）。
        /// </summary>
        public async Task<WfExecutedResult> RunProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _wfAppRunner = runner;
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = await RunAsync(kit, cancellationToken).ConfigureAwait(false);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        #endregion

        #region Process Run Auto
        /// <summary>
        /// Run Automatically
        /// 自动运行
        /// </summary>
        public WfExecutedResult RunAuto()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = RunAuto(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult RunAuto(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
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

        async Task<WfExecutedResult> RunAutoAsync(SQLBuilder kit, CancellationToken cancellationToken)
        {
            var runner = _wfAppRunner;
            WfExecutedResult runAppResult = WfExecutedResult.Default();
            try
            {
                using (await InstanceExecutionLock.AcquireAsync(ResolveInstanceLockKey(runner), cancellationToken)
                    .ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    runAppResult = ExecuteRunAutoCore(kit, runner);
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

        WfExecutedResult ExecuteRunAutoCore(SQLBuilder kit, WfAppRunner runner)
        {
            var runAppResult = WfExecutedResult.Default();
            var runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceRunAuto(Sail, runner, kit, ref runAppResult);
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
                    runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult RunProcessAuto(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = RunAuto();

            return result;
        }

        /// <summary>
        /// Run process auto async：可取消等待实例锁；Mediator 仍同步（阶段 2）。
        /// </summary>
        public async Task<WfExecutedResult> RunProcessAutoAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _wfAppRunner = runner;
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = await RunAutoAsync(kit, cancellationToken).ConfigureAwait(false);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        #endregion

        #region Process Withdraw
        /// <summary>
        /// Process withdraw
        /// 流程撤回
        /// </summary>
        public WfExecutedResult Withdraw()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Withdraw(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult Withdraw(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteWithdrawCore(kit, runner);
        }

        WfExecutedResult ExecuteWithdrawCore(SQLBuilder kit, WfAppRunner runner)
        {
            WfExecutedResult withdrawedResult = WfExecutedResult.Default();

            //Withdraw method begins execution
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceWithdraw(Sail, runner, ref withdrawedResult);

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
                    runtimeInstance.TaskView?.Id,
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
                            runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult WithdrawProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = Withdraw();

            return result;
        }

        /// <summary>
        /// Process withdraw async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> WithdrawProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteWithdrawCore);

        #endregion

        #region Process Sendback
        /// <summary>
        /// Process sendback
        /// 流程退回
        /// </summary>
        public WfExecutedResult SendBack()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = SendBack(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult SendBack(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteSendBackCore(kit, runner);
        }

        WfExecutedResult ExecuteSendBackCore(SQLBuilder kit, WfAppRunner runner)
        {
            WfExecutedResult sendbackResult = WfExecutedResult.Default();

            //sendback operation
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceSendBack(Sail, runner, ref sendbackResult);

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
                    runtimeInstance.TaskView?.Id,
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
                            runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult SendBackProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = SendBack();

            return result;
        }

        /// <summary>
        /// Process sendback async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> SendBackProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteSendBackCore);

        #endregion

        #region Process Resend
        /// <summary>
        /// Process Resend
        /// 流程转发
        /// </summary>
        public WfExecutedResult Resend()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Resend(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult Resend(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteResendCore(kit, runner);
        }

        WfExecutedResult ExecuteResendCore(SQLBuilder kit, WfAppRunner runner)
        {
            WfExecutedResult resendResult = WfExecutedResult.Default();

            //Resend start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceResend(Sail, runner, ref resendResult);

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
                    runtimeInstance.TaskView?.Id,
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
                            runtimeInstance.TaskView?.Id,
                            EventFireTypeEnum.OnProcessResent,
                            FlowOperation.Resend);
                        FlowCapabilityPipeline.AfterSuccess(
                            Sail, kit, hookCtx, EventFireTypeEnum.OnProcessResent);
                    }
            }
            catch (System.Exception e)
            {
                resendResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                resendResult.Message = Sail.Messages.GetEngineMessage("workflowservice.resendprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RESEND_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            return resendResult;
        }

        /// <summary>
        /// Process Resend
        /// 重新转发（退回到的节点）
        /// </summary>
        public WfExecutedResult ResendProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = Resend();

            return result;
        }

        /// <summary>
        /// Process Resend Async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> ResendProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteResendCore);

        #endregion

        #region Process Revise
        /// <summary>
        /// Process revise
        /// (Reassignment of tasks to returned nodes)
        /// 流程修订
        /// (�˻غ�Ľڵ�����ָ������)
        /// </summary>
        public WfExecutedResult Revise()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Revise(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult Revise(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteReviseCore(kit, runner);
        }

        WfExecutedResult ExecuteReviseCore(SQLBuilder kit, WfAppRunner runner)
        {
            WfExecutedResult reviseResult = WfExecutedResult.Default();

            //Revise method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceRevise(Sail, runner, ref reviseResult);

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
                    runtimeInstance.TaskView?.Id,
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
                            runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult ReviseProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = Revise();

            return result;
        }

        /// <summary>
        /// Process revise async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> ReviseProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteReviseCore);

        #endregion

        #region Process Reverse
        /// <summary>
        /// Process reverse
        /// (Completed processes can be reversed)
        /// 流程反签
        /// (�Ѿ����������̿��Ա����
        /// </summary>
        public WfExecutedResult Reverse()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Reverse(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult Reverse(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteReverseCore(kit, runner);
        }

        WfExecutedResult ExecuteReverseCore(SQLBuilder kit, WfAppRunner runner)
        {
            WfExecutedResult reversedResult = WfExecutedResult.Default();

            //Reverse method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceReverse(Sail, runner, ref reversedResult);

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
                    runtimeInstance.TaskView?.Id,
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
                            runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult ReverseProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = Reverse();

            return result;
        }

        /// <summary>
        /// Process reverse async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> ReverseProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteReverseCore);

        #endregion

        #region Process Reject
        /// <summary>
        /// Process reject
        /// 流程驳回
        /// </summary>
        public WfExecutedResult Reject()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Reject(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult Reject(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteRejectCore(kit, runner);
        }

        WfExecutedResult ExecuteRejectCore(SQLBuilder kit, WfAppRunner runner)
        {
            WfExecutedResult rejectedResult = WfExecutedResult.Default();

            //Reject method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runner.NextActivityPerformers = FillNextActivityPerforms(runner, JumpOptionEnum.Startup);
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceReject(Sail, runner, ref rejectedResult);
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
                    runtimeInstance.TaskView?.Id,
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
                            runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult RejectProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = Reject();

            return result;
        }

        /// <summary>
        /// Process reject async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> RejectProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteRejectCore);

        #endregion

        #region Process Close
        /// <summary>
        /// Process Close
        /// 流程关闭
        /// </summary>
        public WfExecutedResult Close()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Close(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult Close(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteCloseCore(kit, runner);
        }

        WfExecutedResult ExecuteCloseCore(SQLBuilder kit, WfAppRunner runner)
        {
            WfExecutedResult closedResult = WfExecutedResult.Default();

            //Close method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runner.NextActivityPerformers = FillNextActivityPerforms(runner, JumpOptionEnum.End);
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceClose(Sail, runner, ref closedResult);
                if (closedResult.Status == WfExecutedStatus.Exception)
                {
                    return closedResult;
                }

                var closeGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
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
                            runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult CloseProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = Close();

            return result;
        }

        /// <summary>
        /// Process Close Async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> CloseProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteCloseCore);

        #endregion

        #region Process Jump
        /// <summary>
        /// Process Jump
        /// 流程跳转
        /// </summary>
        public WfExecutedResult Jump(JumpOptionEnum jumpOption = JumpOptionEnum.Default)
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Jump(kit, jumpOption);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult Jump(SQLBuilder kit,
            JumpOptionEnum jumpOption = JumpOptionEnum.Default)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteJumpCore(kit, runner, jumpOption);
        }

        WfExecutedResult ExecuteJumpCore(SQLBuilder kit, WfAppRunner runner, JumpOptionEnum jumpOption)
        {
            WfExecutedResult jumpResult = WfExecutedResult.Default();

            //Jump method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runner.NextActivityPerformers = FillNextActivityPerforms(runner, jumpOption);
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceJump(Sail, runner, ref jumpResult);

                if (jumpResult.Status == WfExecutedStatus.Exception)
                {
                    return jumpResult;
                }

                var jumpGateCtx = FlowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
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
                        runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult JumpProcess(WfAppRunner runner, 
            JumpOptionEnum jumpOption = JumpOptionEnum.Default)
        {
            _wfAppRunner = runner;
            var result = Jump(jumpOption);

            return result;
        }

        /// <summary>
        /// Process Jump Async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> JumpProcessAsync(WfAppRunner runner, JumpOptionEnum jumpOption = JumpOptionEnum.Default, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, (kit, r) => ExecuteJumpCore(kit, r, jumpOption));

        /// <summary>
        /// Regenerate the list of performers for the jump activity
        /// 流程跳转动作执行人员列表
        /// </summary>
        private IDictionary<string, PerformerList> FillNextActivityPerforms(WfAppRunner runner,
            JumpOptionEnum jumpOption)
        {
            IDictionary<string, PerformerList> nextActivityPerformers = null;
            var pm = ProcessModelFactory.CreateByProcess(Sail, runner.ProcessId, runner.Version);
            if (jumpOption == JumpOptionEnum.Default)
            {
                nextActivityPerformers = runner.NextActivityPerformers;
            }
            else if (jumpOption == JumpOptionEnum.Startup)
            {
                var firstActivity = pm.GetFirstActivity();
                var aim = new ActivityInstanceManager(Sail);
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
        public WfExecutedResult SignForward()
        {
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = SignForward(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        public WfExecutedResult SignForward(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteSignForwardCore(kit, runner);
        }

        WfExecutedResult ExecuteSignForwardCore(SQLBuilder kit, WfAppRunner runner)
        {
            WfExecutedResult signforwardResult = WfExecutedResult.Default();

            //Signforward start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceSignForward(Sail, runner, ref signforwardResult);

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
                    runtimeInstance.TaskView?.Id,
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
                            runtimeInstance.TaskView?.Id,
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
        public WfExecutedResult SignForwardProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            var result = SignForward();

            return result;
        }

        /// <summary>
        /// Process Signforward Async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> SignForwardProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteSignForwardCore);

        #endregion

        #region Process Transfer
        /// <summary>
        /// 转办：结束本人待办 + 本节点交给他人，不推进下一节点。
        /// </summary>
        public WfExecutedResult TransferProcess(WfAppRunner runner)
        {
            _wfAppRunner = runner;
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    var result = Transfer(kit);
                    if (result.Status == WfExecutedStatus.Success)
                        kit.commit();
                    return result;
                }
                catch
                {
                    throw;
                }
            }
        }

        /// <summary>
        /// Transfer async：可取消等待实例锁；Mediator 仍同步（阶段 2 续）。
        /// </summary>
        public Task<WfExecutedResult> TransferProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
            => RunVerbAsync(runner, cancellationToken, ExecuteTransferCore);

        public WfExecutedResult Transfer(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return ExecuteTransferCore(kit, runner);
        }

        WfExecutedResult ExecuteTransferCore(SQLBuilder kit, WfAppRunner runner)
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

                var tm = new TaskManager(Sail);
                var taskView = tm.GetTaskView(kit, runner.TaskId.Value);
                if (taskView?.ActivityInstanceId == null)
                {
                    result.Status = WfExecutedStatus.Exception;
                    result.ExceptionType = WfExceptionType.Transfer_ErrorArguments;
                    result.Message = "Transfer task not found.";
                    return result;
                }

                var aim = new ActivityInstanceManager(Sail);
                var activityInstance = aim.GetById(kit, taskView.ActivityInstanceId.Value);
                var processInstanceId = activityInstance.ProcessInstanceId ?? 0;

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

                var managed = tm.Transfer(kit, runner.TaskId.Value, runner, targets);
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

        private static PerformerList FlattenTransferTargets(WfAppRunner runner)
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
        public bool ResumeProcess(int processInstanceId, WfAppRunner runner)
        {
            bool result = true;
            try
            {
                var pim = new ProcessInstanceManager(Sail);
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
        public bool SuspendProcess(int taskId, WfAppRunner runner)
        {
            bool result = true;
            try
            {
                var pim = new ProcessInstanceManager(Sail);
                var taskMng = new TaskManager(Sail);
                TaskEntity taskentity = taskMng.GetTask(taskId);
                pim.Suspend(taskentity.ProcessInstanceId.Value, runner);
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
        public bool CancelProcess(WfAppRunner runner)
        {
            var pim = new ProcessInstanceManager(Sail);
            return pim.Cancel(runner);
        }

        /// <summary>
        /// Process Discard
        /// 流程流转
        /// </summary>
        public bool DiscardProcess(WfAppRunner runner)
        {
            var pim = new ProcessInstanceManager(Sail);
            return pim.Discard(runner);
        }

        /// <summary>
        /// Process Terminate
        /// 终止流程
        /// </summary>
        public bool TerminateProcess(WfAppRunner terminator)
        {
            var pim = new ProcessInstanceManager(Sail);
            return pim.Terminate(terminator);
        }

        #endregion

        #region Task Approval Agree Refuse
        /// <summary>
        /// Agree
        /// 同意
        /// </summary>
        public void AgreeTask(int taskId)
        {
            var aim = new ActivityInstanceManager(Sail);
            aim.Agree(taskId);
        }


        /// <summary>
        /// Refuse
        /// 拒绝
        /// </summary>
        public void RefuseTask(int taskId)
        {
            var aim = new ActivityInstanceManager(Sail);
            aim.Refuse(taskId);
        }
        #endregion
    }
}
