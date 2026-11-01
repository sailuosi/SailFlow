using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Business.Result;
using saiflow.engine.Core.Result;
using saiflow.engine.Event;
using saiflow.engine.Core.Event;
using saiflow.engine.Client;
using mooSQL.data;
using saiflow.engine.Core.Runtime;
using saiflow.engine.Hooks;

namespace saiflow.engine.Service
{
    /// <summary>
    /// Workflow Service - Process Execution
    /// 工作流服务 - 流程执行部分
    /// </summary>
    public partial class WorkflowService : IWorkflowService
    {
        /// <summary>
        /// Per-instance semaphores: keyed by AppInstanceId, allowing parallel
        /// execution across different process instances while still serializing
        /// concurrent operations on the same instance.
        /// </summary>
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _instanceSemaphores = new();

        private static SemaphoreSlim GetInstanceSemaphore(string key)
            => _instanceSemaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

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
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //��������ִ��
            //Start method execution
            WfRuntimeManager runtimeInstance = null;
            try
            {
                var startLockKey = string.IsNullOrEmpty(runner.AppInstanceId) ? runner.ProcessId : runner.AppInstanceId;
                var startSem = GetInstanceSemaphore(startLockKey);
                startSem.Wait();
                try
                {
                    runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceStartup(Sail, runner, ref startedResult);

                    if (startedResult.Status == WfExecutedStatus.Exception)
                    {
                        return startedResult;
                    }

                    var gateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                        Sail,
                        kit,
                        runner,
                        0,
                        runtimeInstance.RunningActivityInstance,
                        null,
                        WorkflowOperation.Start);
                    var startActivity = runtimeInstance.ProcessModel?.GetStartActivity();
                    if (startActivity != null)
                    {
                        gateCtx.ActivityId = startActivity.ActivityId;
                        gateCtx.ActivityCode = startActivity.ActivityCode;
                    }

                    if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, gateCtx, startedResult))
                        return startedResult;

                    //���¼�
                    //Register event
                    WfRuntimeManagerFactory.RegisterEvent(runtimeInstance,
                        runtimeInstance_OnWfProcessStarting,
                        runtimeInstance_OnWfProcessStarted);

                    bool isStarted = runtimeInstance.Execute(kit);
                }
                finally
                {
                    startSem.Release();
                }
                //do some thing else here...
                //...
                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                startedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                startedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.startprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_START_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessStarting, 
                   runtimeInstance_OnWfProcessStarted);
                waitHandler.Dispose();
            }
            return startedResult;

            void runtimeInstance_OnWfProcessStarting(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessStarting,
                        runner.EventSubscriptionList,
                        startedResult.ProcessInstanceIdStarted);
            }

            void runtimeInstance_OnWfProcessStarted(object sender, WfEventArgs args)
            {
                startedResult = args.WfExecutedResult;
                if (startedResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit, 
                        EventFireTypeEnum.OnProcessStarted,
                        runner.EventSubscriptionList,
                        startedResult.ProcessInstanceIdStarted);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        startedResult.ProcessInstanceIdStarted,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessStarted,
                        WorkflowOperation.Start);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessStarted);
                }
                waitHandler.Set();
            }
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
        /// Start process async
        /// </summary>
        public async Task<WfExecutedResult> StartProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() => 
            {
                return StartProcess(runner);
            });
            return task;
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
        /// Start by Message Async
        /// 异步消息启动流程
        /// </summary>
        public async Task<WfExecutedResult> StartProcessByMessageAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return StartProcessByMessage(runner);
            });
            return task;
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
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //���з�����ʼִ��
            //The running method starts executing
            WfRuntimeManager runtimeInstance = null;
            try
            {
                var runLockKey = string.IsNullOrEmpty(runner.AppInstanceId) ? runner.ProcessId : runner.AppInstanceId;
                var runSem = GetInstanceSemaphore(runLockKey);
                runSem.Wait();
                try
                {
                    runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceAppRunning(Sail, runner, kit, ref runAppResult);
                    if (runAppResult.Status == WfExecutedStatus.Exception)
                    {
                        return runAppResult;
                    }

                    var gateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        WorkflowOperation.Run);
                    if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, gateCtx, runAppResult))
                        return runAppResult;

                    //ע���¼�������
                    //Register event
                    WfRuntimeManagerFactory.RegisterEvent(runtimeInstance,
                        runtimeInstance_OnWfProcessRunning,
                        runtimeInstance_OnWfProcessContinued);
                    bool isRun = runtimeInstance.Execute(kit);
                }
                finally
                {
                    runSem.Release();
                }
                //do some thing else here...
                //...
                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                runAppResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                runAppResult.Message = Sail.Messages.GetEngineMessage("workflowservice.runprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessRunning,
                    runtimeInstance_OnWfProcessContinued);
                waitHandler.Dispose();
            }
            return runAppResult;

            void runtimeInstance_OnWfProcessRunning(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessRunning,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId); 
            }

            void runtimeInstance_OnWfProcessContinued(object sender, WfEventArgs args)
            {
                runAppResult = args.WfExecutedResult;
                if (runAppResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessContinued,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessContinued,
                        WorkflowOperation.Run);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessContinued);
                }
                waitHandler.Set();
            }
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
        public async Task<WfExecutedResult> RunProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return RunProcess(runner);
            });
            return task;
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
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //���з�����ʼִ��
            //The running method starts executing
            WfRuntimeManager runtimeInstance = null;
            try
            {
                var runAutoLockKey = string.IsNullOrEmpty(runner.AppInstanceId) ? runner.ProcessId : runner.AppInstanceId;
                var runAutoSem = GetInstanceSemaphore(runAutoLockKey);
                runAutoSem.Wait();
                try
                {
                    runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceRunAuto(Sail, runner, kit, ref runAppResult);
                    if (runAppResult.Status == WfExecutedStatus.Exception)
                    {
                        return runAppResult;
                    }

                    //ע���¼�������
                    //Register event
                    WfRuntimeManagerFactory.RegisterEvent(runtimeInstance,
                        runtimeInstance_OnWfProcessRunning,
                        runtimeInstance_OnWfProcessContinued);
                    bool isRun = runtimeInstance.Execute(kit);
                }
                finally
                {
                    runAutoSem.Release();
                }
                //do some thing else here...
                //...
                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                runAppResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                runAppResult.Message = Sail.Messages.GetEngineMessage("workflowservice.runprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance,
                    runtimeInstance_OnWfProcessRunning,
                    runtimeInstance_OnWfProcessContinued);
                waitHandler.Dispose();
            }
            return runAppResult;

            void runtimeInstance_OnWfProcessRunning(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessRunning,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessContinued(object sender, WfEventArgs args)
            {
                runAppResult = args.WfExecutedResult;
                if (runAppResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessContinued,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);
                }
                waitHandler.Set();
            }
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
        /// Run process auto async
        /// 异步执行自动
        /// </summary>
        public async Task<WfExecutedResult> RunProcessAutoAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return RunProcessAuto(runner);
            });
            return task;
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
            WfExecutedResult withdrawedResult = WfExecutedResult.Default();
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //����������ʼִ��
            //Withdraw method begins execution
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceWithdraw(Sail, runner, ref withdrawedResult);

                //�����㳷�������������쳣�����Ϣ
                //Not satisfied with withdraw operation, return exception
                if (withdrawedResult.Status == WfExecutedStatus.Exception)
                {
                    return withdrawedResult;
                }

                var withdrawGateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
                    WorkflowOperation.Withdraw);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, withdrawGateCtx, withdrawedResult))
                    return withdrawedResult;

                //ע���¼�������
                //Register event
                WfRuntimeManagerFactory.RegisterEvent(runtimeInstance,
                    runtimeInstance_OnWfProcessWithdrawing,
                    runtimeInstance_OnWfProcessWithdrawn);
                bool isWithdrawn = runtimeInstance.Execute(kit);

                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                withdrawedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                withdrawedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.withdrawprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_WITHDRAW_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessWithdrawing,
                    runtimeInstance_OnWfProcessWithdrawn);
                waitHandler.Dispose();
            }
            return withdrawedResult;

            void runtimeInstance_OnWfProcessWithdrawing(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessWithdrawing,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessWithdrawn(object sender, WfEventArgs args)
            {
                withdrawedResult = args.WfExecutedResult;
                if (withdrawedResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessWithdrawn,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessWithdrawn,
                        WorkflowOperation.Withdraw);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessWithdrawn);
                }
                waitHandler.Set();
            }
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
        /// Process withdraw async
        /// 异步撤回流程
        /// </summary>
        public async Task<WfExecutedResult> WithdrawProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return WithdrawProcess(runner);
            });
            return task;
        }

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
            WfExecutedResult sendbackResult = WfExecutedResult.Default();
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //�˻ؿ�ʼ
            //sendback operation
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceSendBack(Sail, runner, ref sendbackResult);

                if (sendbackResult.Status == WfExecutedStatus.Exception)
                {
                    return sendbackResult;
                }

                var sendBackGateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
                    WorkflowOperation.SendBack);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, sendBackGateCtx, sendbackResult))
                    return sendbackResult;

                //ע���¼�������
                //Register event
                WfRuntimeManagerFactory.RegisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessSendBacking,
                    runtimeInstance_OnWfProcessSendBacked);
                bool isSendBacked = runtimeInstance.Execute(kit);

                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                sendbackResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                sendbackResult.Message = Sail.Messages.GetEngineMessage("workflowservice.sendbackprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_SENDBACK_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessSendBacking,
                    runtimeInstance_OnWfProcessSendBacked);
                waitHandler.Dispose();
            }
            return sendbackResult;


            void runtimeInstance_OnWfProcessSendBacking(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessSendBacking,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessSendBacked(object sender, WfEventArgs args)
            {
                sendbackResult = args.WfExecutedResult;
                if (sendbackResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessSendBacked,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessSendBacked,
                        WorkflowOperation.SendBack);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessSendBacked);
                }
                waitHandler.Set();
            }
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
        /// Process sendback async
        /// 异步退回到下一步
        /// </summary>
        public async Task<WfExecutedResult> SendBackProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return SendBackProcess(runner);
            });
            return task;
        }

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
            WfExecutedResult resendResult = WfExecutedResult.Default();
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //���ͷ�����ʼִ��
            //Resend start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceResend(Sail, runner, ref resendResult);

                if (resendResult.Status == WfExecutedStatus.Exception)
                {
                    return resendResult;
                }

                var resendGateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
                    WorkflowOperation.Resend);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, resendGateCtx, resendResult))
                    return resendResult;

                //ע���¼�������
                //Register event
                WfRuntimeManagerFactory.RegisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessResending,
                    runtimeInstance_OnWfProcessResent);
                bool isResent = runtimeInstance.Execute(kit);

                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                resendResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                resendResult.Message = Sail.Messages.GetEngineMessage("workflowservice.resendprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RESEND_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessResending,
                    runtimeInstance_OnWfProcessResent);
                waitHandler.Dispose();
            }
            return resendResult;

            void runtimeInstance_OnWfProcessResending(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessResending,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessResent(object sender, WfEventArgs args)
            {
                resendResult = args.WfExecutedResult;
                if (resendResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessResent,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessResent,
                        WorkflowOperation.Resend);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessResent);
                }
                waitHandler.Set();
            }
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
        /// Process Resend Async
        /// 异步转发流程
        /// </summary>
        public async Task<WfExecutedResult> ResendProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return ResendProcess(runner);
            });
            return task;
        }

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
            WfExecutedResult reviseResult = WfExecutedResult.Default();
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //�޶�������ʼִ��
            //Revise method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceRevise(Sail, runner, ref reviseResult);

                if (reviseResult.Status == WfExecutedStatus.Success)
                {
                    return reviseResult;
                }

                var reviseGateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
                    WorkflowOperation.Revise);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, reviseGateCtx, reviseResult))
                    return reviseResult;

                //ע���¼�������
                //Register event
                WfRuntimeManagerFactory.RegisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessRevising,
                    runtimeInstance_OnWfProcessRevised);
                bool isRevised = runtimeInstance.Execute(kit);

                waitHandler.WaitOne();
            }
            catch(System.Exception e)
            {
                reviseResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                reviseResult.Message = Sail.Messages.GetEngineMessage("workflowservice.reviseprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_REVISE_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ע���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessRevising,
                    runtimeInstance_OnWfProcessRevised);
                waitHandler.Dispose();
            }
            return reviseResult;

            void runtimeInstance_OnWfProcessRevising(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessRevising,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessRevised(object sendder, WfEventArgs args)
            {
                reviseResult = args.WfExecutedResult;
                if (reviseResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessRevised,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessRevised,
                        WorkflowOperation.Revise);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessRevised);
                }
                waitHandler.Set();
            }
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
        /// Process revise async
        /// 异步修订流程
        /// </summary>
        public async Task<WfExecutedResult> ReviseProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return ReviseProcess(runner);
            });
            return task;
        }

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
            WfExecutedResult reversedResult = WfExecutedResult.Default();
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //��ǩ������ʼִ��
            //Reverse method start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceReverse(Sail, runner, ref reversedResult);

                if (reversedResult.Status == WfExecutedStatus.Exception)
                {
                    return reversedResult;
                }

                var reverseGateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
                    WorkflowOperation.Reverse);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, reverseGateCtx, reversedResult))
                    return reversedResult;

                //ע���¼�������
                //Register event
                WfRuntimeManagerFactory.RegisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessReversing,
                    runtimeInstance_OnWfProcessReversed);
                bool isReversed = runtimeInstance.Execute(kit);

                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                reversedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                reversedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.reverseprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_REVERSE_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessReversing,
                    runtimeInstance_OnWfProcessReversed);
                waitHandler.Dispose();
            }
            return reversedResult;

            void runtimeInstance_OnWfProcessReversing(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessReversing,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessReversed(object sender, WfEventArgs args)
            {
                reversedResult = args.WfExecutedResult;
                if (reversedResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessReversed,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessReversed,
                        WorkflowOperation.Reverse);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessReversed);
                }
                waitHandler.Set();
            }
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
        /// Process reverse async
        /// 异步反签流程
        /// </summary>
        public async Task<WfExecutedResult> ReverseProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return ReverseProcess(runner);
            });
            return task;
        }

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
            

            WfExecutedResult rejectedResult = WfExecutedResult.Default();
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //���ط�����ʼִ��
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

                var rejectGateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
                    WorkflowOperation.Reject);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, rejectGateCtx, rejectedResult))
                    return rejectedResult;

                //ע���¼�������
                //Register event
                WfRuntimeManagerFactory.RegisterEvent(runtimeInstance,
                    runtimeInstance_OnWfProcessRejecting,
                    runtimeInstance_OnWfProcessRejected);
                bool isRejected = runtimeInstance.Execute(kit);

                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                rejectedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                rejectedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.rejectprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_REJECT_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance,
                    runtimeInstance_OnWfProcessRejecting,
                    runtimeInstance_OnWfProcessRejected);
                waitHandler.Dispose();
            }
            return rejectedResult;

            void runtimeInstance_OnWfProcessRejecting(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessRejecting,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessRejected(object sender, WfEventArgs args)
            {
                rejectedResult = args.WfExecutedResult;
                if (rejectedResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessRejected,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessRejected,
                        WorkflowOperation.Reject);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessRejected);
                }
                waitHandler.Set();
            }
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
        /// Process reject async
        /// 异步驳回流程
        /// </summary>
        public async Task<WfExecutedResult> RejectProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return RejectProcess(runner);
            });
            return task;
        }

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
            WfExecutedResult closedResult = WfExecutedResult.Default();
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //�رշ�����ʼִ��
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

                var closeGateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
                    WorkflowOperation.Close);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, closeGateCtx, closedResult))
                    return closedResult;

                //ע���¼�������
                //Register event
                WfRuntimeManagerFactory.RegisterEvent(runtimeInstance,
                    runtimeInstance_OnWfProcessClosing,
                    runtimeInstance_OnWfProcessClosed);
                bool isRejected = runtimeInstance.Execute(kit);
            }
            catch (System.Exception e)
            {
                closedResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                closedResult.Message = Sail.Messages.GetEngineMessage("workflowservice.closeprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_CLOSE_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance,
                    runtimeInstance_OnWfProcessClosing,
                    runtimeInstance_OnWfProcessClosed);
                waitHandler.Dispose();
            }
            return closedResult;

            void runtimeInstance_OnWfProcessClosing(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessClosing,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessClosed(object sender, WfEventArgs args)
            {
                closedResult = args.WfExecutedResult;
                if (closedResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessClosed,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessClosed,
                        WorkflowOperation.Close);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessClosed);
                }
                waitHandler.Set();
            }
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
        ///  Process Close Async
        /// 异步关闭流程
        /// </summary>
        public async Task<WfExecutedResult> CloseProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return CloseProcess(runner);
            });
            return task;
        }

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
            WfExecutedResult jumpResult = WfExecutedResult.Default();
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //��ת������ʼִ��
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

                var jumpGateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
                    WorkflowOperation.Jump);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, jumpGateCtx, jumpResult))
                    return jumpResult;

                //ע���¼�������
                //Register event
                WfRuntimeManagerFactory.RegisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessJumping,
                    runtimeInstance_OnWfProcessJumped);          
                bool isJumped = runtimeInstance.Execute(kit);

                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                jumpResult.ExceptionType = WfExceptionType.Jump_OtherError;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                jumpResult.Message = Sail.Messages.GetEngineMessage("workflowservice.jumpprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_JUMP_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessJumping,
                    runtimeInstance_OnWfProcessJumped);
                waitHandler.Dispose();
            }
            return jumpResult;

            void runtimeInstance_OnWfProcessJumping(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessJumping,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessJumped(object sender, WfEventArgs args)
            {
                jumpResult = args.WfExecutedResult;
                if (jumpResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessJumped,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessJumped,
                        WorkflowOperation.Jump);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessJumped);
                }
                waitHandler.Set();
            }
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
        /// Process Jump Async
        /// 异步跳转流程
        /// </summary>
        public async Task<WfExecutedResult> JumpProcessAsync(WfAppRunner runner, JumpOptionEnum jumpOption = JumpOptionEnum.Default)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return JumpProcess(runner, jumpOption);
            });
            return task;
        }

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
            WfExecutedResult signforwardResult = WfExecutedResult.Default();
            AutoResetEvent waitHandler = new AutoResetEvent(false);

            //��ǩ������ʼִ��
            //Signforward start to execute
            WfRuntimeManager runtimeInstance = null;
            try
            {
                runtimeInstance = WfRuntimeManagerFactory.CreateRuntimeInstanceSignForward(Sail, runner, ref signforwardResult);

                if (signforwardResult.Status == WfExecutedStatus.Exception)
                {
                    return signforwardResult;
                }

                var signForwardGateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    runtimeInstance.ProcessInstanceId,
                    runtimeInstance.RunningActivityInstance,
                    runtimeInstance.TaskView?.Id,
                    WorkflowOperation.SignForward);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, signForwardGateCtx, signforwardResult))
                    return signforwardResult;

                //ע���¼�������
                //Register event
                WfRuntimeManagerFactory.RegisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessSignForwarding,
                    runtimeInstance_OnWfProcessSignForwarded);

                bool isSignForwarding = runtimeInstance.Execute(kit);

                waitHandler.WaitOne();
            }
            catch (System.Exception e)
            {
                signforwardResult.Status = WfExecutedStatus.Failed;
                var error = e.InnerException != null ? e.InnerException.Message : e.Message;
                signforwardResult.Message = Sail.Messages.GetEngineMessage("workflowservice.signforwardprocess.error", error);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_SIGN_FORWARD_ERROR, LogEventType.Error, LogPriority.High, runner, e);
            }
            finally
            {
                //ж���¼�
                //Unregister event
                WfRuntimeManagerFactory.UnregisterEvent(runtimeInstance, 
                    runtimeInstance_OnWfProcessSignForwarding,
                    runtimeInstance_OnWfProcessSignForwarded);
                waitHandler.Dispose();
            }
            return signforwardResult;

            void runtimeInstance_OnWfProcessSignForwarding(object sender, WfEventArgs args)
            {
                Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                    EventFireTypeEnum.OnProcessSignForwarding,
                    runner.EventSubscriptionList,
                    runtimeInstance.ProcessInstanceId);
            }

            void runtimeInstance_OnWfProcessSignForwarded(object sender, WfEventArgs args)
            {
                signforwardResult = args.WfExecutedResult;
                if (signforwardResult.Status == WfExecutedStatus.Success)
                {
                    Event.EventExecutor.InvokeExternalEvent(Sail, kit,
                        EventFireTypeEnum.OnProcessSignForwarded,
                        runner.EventSubscriptionList,
                        runtimeInstance.ProcessInstanceId);

                    var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                        Sail,
                        kit,
                        runner,
                        runtimeInstance.ProcessInstanceId,
                        runtimeInstance.RunningActivityInstance,
                        runtimeInstance.TaskView?.Id,
                        EventFireTypeEnum.OnProcessSignForwarded,
                        WorkflowOperation.SignForward);
                    WorkflowCapabilityPipeline.AfterSuccess(Sail, kit, hookCtx, EventFireTypeEnum.OnProcessSignForwarded);
                }
                waitHandler.Set();
            }
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
        /// Process Signforward Async
        /// 异步流程会签
        /// </summary>
        public async Task<WfExecutedResult> SignForwardProcessAsync(WfAppRunner runner)
        {
            var task = await Task.Run<WfExecutedResult>(() =>
            {
                return SignForwardProcess(runner);
            });
            return task;
        }

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

        public async Task<WfExecutedResult> TransferProcessAsync(WfAppRunner runner)
        {
            return await Task.Run(() => TransferProcess(runner));
        }

        public WfExecutedResult Transfer(SQLBuilder kit)
        {
            var runner = _wfAppRunner;
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

                var gateCtx = WorkflowCapabilityPipeline.BuildGateContext(
                    Sail,
                    kit,
                    runner,
                    processInstanceId,
                    activityInstance,
                    runner.TaskId,
                    WorkflowOperation.Transfer);
                if (!WorkflowCapabilityPipeline.TryGate(Sail, kit, gateCtx, result))
                    return result;

                Event.EventExecutor.InvokeExternalEvent(
                    Sail, kit,
                    EventFireTypeEnum.OnProcessTransferring,
                    runner.EventSubscriptionList,
                    processInstanceId);

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

                Event.EventExecutor.InvokeExternalEvent(
                    Sail, kit,
                    EventFireTypeEnum.OnProcessTransferred,
                    runner.EventSubscriptionList,
                    processInstanceId);

                var hookCtx = WorkflowCapabilityPipeline.BuildHookContext(
                    Sail,
                    kit,
                    runner,
                    processInstanceId,
                    activityInstance,
                    runner.TaskId,
                    EventFireTypeEnum.OnProcessTransferred,
                    WorkflowOperation.Transfer);
                WorkflowCapabilityPipeline.AfterSuccess(
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
