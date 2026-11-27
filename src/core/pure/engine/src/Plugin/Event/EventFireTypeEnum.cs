using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sailflow.events
{
    /// <summary>
    /// External event interaction trigger type
    ///  1.  Process event:
    /// Process Start ->Process Run ->Process End
    ///  2.  Event:
    /// Activity creation ->Activity execution ->Activity completion
    ///  3.  Task:
    /// Allocation ->Complete
    ///  外部事件交互触发类型
    ///  1. 流程事件：
    ///  流程启动->流程运行->流程结束
    ///  2. 活动事件：
    ///  活动创建->活动执行->活动完成
    ///  3. 任务：
    ///  分配->完成
    /// </summary>
    public enum EventFireTypeEnum
    {
        None = 0,

        OnProcessStarting = 1,

        OnProcessStarted = 2,

        OnProcessRunning = 3,

        OnProcessContinued = 4,

        OnProcessCompleted = 5,

        OnProcessWithdrawing = 10,

        OnProcessWithdrawn = 11,

        OnProcessSendBacking = 12,

        OnProcessSendBacked = 13,

        OnProcessResending = 14,

        OnProcessResent = 15,

        OnProcessReversing = 16,

        OnProcessReversed = 17,

        OnProcessJumping = 18,

        OnProcessJumped = 19,

        OnProcessSignForwarding = 20,

        OnProcessSignForwarded = 21,

        OnProcessRevising = 22,

        OnProcessRevised = 23,

        OnProcessRejecting = 24,

        OnProcessRejected = 25,

        OnProcessClosing = 26,

        OnProcessClosed = 27,

        OnProcessTransferring = 28,

        OnProcessTransferred = 29,

        OnProcessSuspending = 30,

        OnProcessSuspended = 31,

        OnProcessResuming = 32,

        OnProcessResumed = 33,

        OnProcessCanceling = 34,

        OnProcessCanceled = 35,

        OnProcessDiscarding = 36,

        OnProcessDiscarded = 37,

        OnProcessTerminating = 38,

        OnProcessTerminated = 39,

        OnAssigneeRemoving = 104,

        OnAssigneeRemoved = 105,

        OnAssigneeTakingOn = 120,

        OnAssigneeTakeOn = 121,

        OnAssigneeChanging = 106,

        OnAssigneeChanged = 107,

        OnAssigneeAdding = 118,

        OnAssigneeAdded = 119,

        OnProcessUrging = 108,

        OnProcessUrged = 109,

        OnProcessDelegating = 110,

        OnProcessDelegated = 111,

        OnProcessConsulting = 112,

        OnProcessConsulted = 113,

        OnTaskAcknowledging = 114,

        OnTaskAcknowledged = 115,

        OnConsultRecalling = 116,

        OnConsultRecalled = 117,

        OnActivityCreated = 40,

        OnActivityExecuting = 41,

        OnActivityExecuted = 42,

        OnActivityCompleted = 43,

        /// <summary>活动变为 Ready（可办）；串行会签下一人就绪时触发。</summary>
        OnActivityReady = 44,

        /// <summary>发送/Run 等推进落库前；仅 Gate。</summary>
        OnValidating = 50,

        OnTaskCreated = 60,

        OnTaskCompleted = 61,

        OnTaskUrged = 62,

        /// <summary>抄送创建（产品通道；与待办 Notifier 分离）。</summary>
        OnCopytoCreated = 70,

        /// <summary>
        /// Service 自管事务 <c>commit</c> 成功之后（AfterCommit）。
        /// 仅 Sync BestEffort；<c>ctx.Kit</c> 恒空；异常不改已返回的 Success。
        /// </summary>
        OnTransactionCommitted = 80,

        /// <summary>
        /// Service 自管事务未提交：异常，或 <c>Status != Success</c>（含 Gate Reject）。
        /// 仅 Sync BestEffort；不二次抛、不改原结果/异常。
        /// </summary>
        OnTransactionFailed = 81,

        OnSuccess = 100,

        OnError = 101,

        OnWarning = 102
    }
}
