using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using sailflow.engine.Common;
using sailflow.engine.Result;
using sailflow.events;
using sailflow.run;
using sailflow.engine.Client;
using mooSQL.data;

using sailflow.lanes;

namespace sailflow.engine.Service
{
    /// <summary>
    /// Workflow Service - Process Execution
    /// 工作流服务 - 流程执行部分
    /// </summary>
    public partial class WorkflowService : IWorkflowService
    {
        /// <summary>
        /// 无状态：开事务执行，成功才 commit。
        /// 提交后 / 未提交失败时唤起事务生命周期钩子（BestEffort）。
        /// </summary>
        private WfExecutedResult CommitOnSuccess(
            FlowCommandContext runner,
            Func<SQLBuilder, WfExecutedResult> action)
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            WfExecutedResult result = null;
            try
            {
                result = action(kit);
                if (result.Status == WfExecutedStatus.Success)
                {
                    kit.commit();
                    FireTxLifecycle(runner, EventFireTypeEnum.OnTransactionCommitted, result, null);
                }
                else
                {
                    FireTxLifecycle(runner, EventFireTypeEnum.OnTransactionFailed, result, null);
                }
                return result;
            }
            catch (Exception ex)
            {
                FireTxLifecycle(runner, EventFireTypeEnum.OnTransactionFailed, result, ex);
                throw;
            }
        }

        private async Task<WfExecutedResult> CommitOnSuccessAsync(
            FlowCommandContext runner,
            Func<SQLBuilder, Task<WfExecutedResult>> action)
        {
            using var kit = Sail.UseRunSQL();
            kit.beginTransaction();
            WfExecutedResult result = null;
            try
            {
                result = await action(kit).ConfigureAwait(false);
                if (result.Status == WfExecutedStatus.Success)
                {
                    kit.commit();
                    FireTxLifecycle(runner, EventFireTypeEnum.OnTransactionCommitted, result, null);
                }
                else
                {
                    FireTxLifecycle(runner, EventFireTypeEnum.OnTransactionFailed, result, null);
                }
                return result;
            }
            catch (Exception ex)
            {
                FireTxLifecycle(runner, EventFireTypeEnum.OnTransactionFailed, result, ex);
                throw;
            }
        }

        void FireTxLifecycle(
            FlowCommandContext runner,
            EventFireTypeEnum eventType,
            WfExecutedResult result,
            Exception failureException)
        {
            var dispatcher = Sail.CapabilityDispatcher;
            if (dispatcher == null || Sail.Capabilities?.Hooks == null || Sail.Capabilities.Hooks.Count == 0)
                return;

            var ctx = dispatcher.BuildTxLifecycleContext(runner, eventType, result, failureException);
            dispatcher.RunTxLifecycleHooks(ctx, eventType);
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
            return await CommitOnSuccessAsync(runner, async kit =>
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
            CommitOnSuccess(runner, kit =>
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
        /// <summary>在已有事务 kit 上启动（带锁）。Transition 族 <see cref="StartAction"/>。</summary>
        public WfExecutedResult Start(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new StartAction(Sail, kit, runner).Execute();
        }

        /// <summary>Start process — Transition 族 <see cref="StartAction"/>。</summary>
        public WfExecutedResult StartProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new StartAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> StartProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new StartAction(Sail, kit, r).Execute());

        #endregion

        #region Process Startup by Message
        /// <summary>
        /// Start by Message
        /// 消息启动流程
        /// </summary>

        public WfExecutedResult StartByMessage(SQLBuilder kit, FlowCommandContext runner)
        {
            BindMessageStartup(runner);
            return Start(kit, runner);
        }

        /// <summary>消息启动：解析定义后走同一 <see cref="StartAction"/>。</summary>
        public WfExecutedResult StartProcessByMessage(FlowCommandContext runner)
            => RunAction(runner, (kit, r) =>
            {
                BindMessageStartup(r);
                return new StartAction(Sail, kit, r).Execute();
            });

        public Task<WfExecutedResult> StartProcessByMessageAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) =>
            {
                BindMessageStartup(r);
                return new StartAction(Sail, kit, r).Execute();
            });

        void BindMessageStartup(FlowCommandContext runner)
        {
            var entity = Sail.ManagerFactory.Processes.GetByMessage(runner.MessageTopic);
            runner.ProcessId = entity.ProcessId;
            runner.ProcessCode = entity.ProcessCode;
            runner.Version = entity.Version;
            runner.UserId = WfDefine.SYSTEM_INTERNAL_USER_ID;
            runner.UserName = WfDefine.SYSTEM_INTERNAL_USER_NAME;
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
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new RunAction(Sail, kit, runner).Execute();
        }

        /// <summary>Process Run — Transition 族 <see cref="RunAction"/>。</summary>
        public WfExecutedResult RunProcessApp(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new RunAction(Sail, kit, r).Execute());

        public WfExecutedResult RunProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new RunAction(Sail, kit, r).Execute());

        /// <summary>Run async：可取消等锁；Factory 仍 IO-await，Gate/Hook 走 RunAction.ExecuteWithRuntime。</summary>
        public async Task<WfExecutedResult> RunProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await CommitOnSuccessAsync(runner, async kit =>
            {
                using (await InstanceExecutionLock.AcquireAsync(ResolveInstanceLockKey(runner), cancellationToken)
                    .ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var (runtime, factoryResult) = await Runtimes
                        .CreateRuntimeInstanceAppRunningAsync(runner, kit, cancellationToken)
                        .ConfigureAwait(false);
                    return new RunAction(Sail, kit, runner).ExecuteWithRuntime(runtime, factoryResult);
                }
            }).ConfigureAwait(false);
        }

        #endregion

        #region Process Run Auto
        /// <summary>在已有事务 kit 上自动推进（带锁，无 Gate）。Transition 族 <see cref="RunAutoAction"/>。</summary>
        public WfExecutedResult RunAuto(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new RunAutoAction(Sail, kit, runner).Execute();
        }

        public WfExecutedResult RunProcessAuto(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new RunAutoAction(Sail, kit, r).Execute());

        /// <summary>RunAuto async：Factory 预载后走 <see cref="TransitionActionBase.ExecuteWithRuntime"/>（仍 SkipGate）。</summary>
        public async Task<WfExecutedResult> RunProcessAutoAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await CommitOnSuccessAsync(runner, async kit =>
            {
                using (await InstanceExecutionLock.AcquireAsync(ResolveInstanceLockKey(runner), cancellationToken)
                    .ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var (runtime, factoryResult) = await Runtimes
                        .CreateRuntimeInstanceRunAutoAsync(runner, kit, cancellationToken)
                        .ConfigureAwait(false);
                    return new RunAutoAction(Sail, kit, runner).ExecuteWithRuntime(runtime, factoryResult);
                }
            }).ConfigureAwait(false);
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
        public WfExecutedResult Resend(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new ResendAction(Sail, kit, runner).Execute();
        }

        public WfExecutedResult ResendProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new ResendAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> ResendProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new ResendAction(Sail, kit, r).Execute());

        #endregion

        #region Process Revise
        public WfExecutedResult Revise(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new ReviseAction(Sail, kit, runner).Execute();
        }

        public WfExecutedResult ReviseProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new ReviseAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> ReviseProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new ReviseAction(Sail, kit, r).Execute());

        #endregion

        #region Process Reverse
        public WfExecutedResult Reverse(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new ReverseAction(Sail, kit, runner).Execute();
        }

        public WfExecutedResult ReverseProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new ReverseAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> ReverseProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new ReverseAction(Sail, kit, r).Execute());

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
        public WfExecutedResult SignForward(SQLBuilder kit, FlowCommandContext runner)
        {
            using (InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner)))
                return new SignForwardAction(Sail, kit, runner).Execute();
        }

        public WfExecutedResult SignForwardProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new SignForwardAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> SignForwardProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new SignForwardAction(Sail, kit, r).Execute());

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

        /// <summary>A4：减签（Assignee 族）；事务+锁由 <see cref="RunAction"/>。Host 办理条待产品码表。</summary>
        public WfExecutedResult RemoveAssigneeProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new RemoveAssigneeAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> RemoveAssigneeProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new RemoveAssigneeAction(Sail, kit, r).Execute());

        /// <summary>A4 续：改派（Assignee 族）。Host 办理条待产品。</summary>
        public WfExecutedResult ChangeAssigneeProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new ChangeAssigneeAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> ChangeAssigneeProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new ChangeAssigneeAction(Sail, kit, r).Execute());

        /// <summary>加人（Assignee 族）：扁节点追加普通办理人；不压加签栈。</summary>
        public WfExecutedResult AddAssigneeProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new AddAssigneeAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> AddAssigneeProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new AddAssigneeAction(Sail, kit, r).Execute());

        /// <summary>A4 续：催办（Assignee 族）；仅 Notifier/Hook，不改待办状态。</summary>
        public WfExecutedResult UrgeProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new UrgeAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> UrgeProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new UrgeAction(Sail, kit, r).Execute());

        /// <summary>委派（Assignee 族）；受托人办结经 Mediator 自动回转原人。</summary>
        public WfExecutedResult DelegateProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new DelegateAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> DelegateProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new DelegateAction(Sail, kit, r).Execute());

        /// <summary>沟通（Assignee 族）：建参与者待办并锁定发起人审批。</summary>
        public WfExecutedResult ConsultProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new ConsultAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> ConsultProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new ConsultAction(Sail, kit, r).Execute());

        /// <summary>阅办（Assignee 族）：参与者关闭自己的沟通待办。</summary>
        public WfExecutedResult AcknowledgeProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new AcknowledgeAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> AcknowledgeProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new AcknowledgeAction(Sail, kit, r).Execute());

        /// <summary>收回沟通（Assignee 族）。</summary>
        public WfExecutedResult RecallConsultProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new RecallConsultAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> RecallConsultProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new RecallConsultAction(Sail, kit, r).Execute());

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

        /// <summary>取消（Lifecycle 族）；事务+锁由 <see cref="RunAction"/>；写库走同一 kit。</summary>
        public WfExecutedResult CancelProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new CancelAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> CancelProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new CancelAction(Sail, kit, r).Execute());

        /// <summary>作废（Lifecycle 族）。</summary>
        public WfExecutedResult DiscardProcess(FlowCommandContext runner)
            => RunAction(runner, (kit, r) => new DiscardAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> DiscardProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
            => RunActionAsync(runner, cancellationToken, (kit, r) => new DiscardAction(Sail, kit, r).Execute());

        /// <summary>终止（Lifecycle 族）；PIM 级联收口待办并发 Cancelled Notifier。</summary>
        public WfExecutedResult TerminateProcess(FlowCommandContext terminator)
            => RunAction(terminator, (kit, r) => new TerminateAction(Sail, kit, r).Execute());

        public Task<WfExecutedResult> TerminateProcessAsync(FlowCommandContext terminator, CancellationToken cancellationToken = default)
            => RunActionAsync(terminator, cancellationToken, (kit, r) => new TerminateAction(Sail, kit, r).Execute());

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
