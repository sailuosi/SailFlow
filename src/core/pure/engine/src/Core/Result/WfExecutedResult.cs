using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.engine.Common;
using sailflow.engine.Runtime;
using sailflow.engine.Hooks;
using sailflow.engine.Core;

namespace sailflow.engine.Result
{
    /// <summary>
    /// 执行结果对象封装
    /// </summary>
    public class WfExecutedResult
    {
        public WfExecutedStatus Status { get; set; }
        public String Message { get; set; }

        /// <summary>R4：办理人管道等业务拒绝码（如 <c>AssigneeConfigMissing</c> / <c>AssigneeEmpty</c>）。</summary>
        public string ReasonCode { get; set; }

        /// <summary>Gate 业务拒绝码（非引擎事故）。</summary>
        public string ValidationCode { get; set; }

        /// <summary>Gate 结构化校验明细。</summary>
        public IList<HookValidationItem> ValidationDetails { get; set; }
        /// <summary>Optional AI reply from RAG/LLM node (e.g. ai_response variable). Used by workflow_completed so clients get business reply instead of engine status.</summary>
        public String AiResponse { get; set; }
        public String ExceptionType { get; set; }

        /// <summary>
        /// New instance Id returned by process initiation
        /// 流程启动返回的新实例Id
        /// </summary>
        public string ProcessInstanceIdStarted { get; set; }

        /// <summary>
        /// When the process is rolled back, the recipient information of the returned task
        /// 流程做回退处理时，返回的任务接收人信息
        /// </summary>
        public WfBackwardTaskReceiver BackwardTaskReceiver { get; set; }

        /// <summary>
        /// Return parameters during process rollback
        /// 流程回退时返回参数
        /// </summary>
        public ReturnDataContext ReturnDataContext { get; set; }  
        public WfExecutedResult()
        {
            Status = WfExecutedStatus.Default;
            Message = string.Empty;
        }

        /// <summary>
        /// Default
        /// 缺省方法
        /// </summary>
        /// <returns></returns>
        public static WfExecutedResult Default()
        {
            return new WfExecutedResult();
        }

        /// <summary>应用 Gate <see cref="HookResult.Reject"/>，不包 runprocess.error 模板。</summary>
        public void ApplyGateReject(HookResult gateResult)
        {
            if (gateResult == null || gateResult.IsOk)
                return;

            Status = WfExecutedStatus.Failed;
            Message = gateResult.Message ?? string.Empty;
            ValidationCode = gateResult.Code;
            ValidationDetails = gateResult.Details != null
                ? new List<HookValidationItem>(gateResult.Details)
                : null;
        }

        /// <summary>Gate 业务拒绝（§14 #9：Status=Failed 且带 ValidationCode，非新枚举值）。</summary>
        public bool IsValidationReject() =>
            Status == WfExecutedStatus.Failed && ValidationCode != null;

        /// <summary>同 <see cref="IsValidationReject"/>，供 API 语义命名。</summary>
        public bool IsBusinessValidationFailure() => IsValidationReject();

        /// <summary>R4：办理人管道拒绝（带 <see cref="ReasonCode"/>）。</summary>
        public bool IsAssigneeReject() =>
            !string.IsNullOrEmpty(ReasonCode)
            && (Status == WfExecutedStatus.Failed || Status == WfExecutedStatus.Exception);

        /// <summary>写入办理人拒绝结果。</summary>
        public void ApplyAssigneeException(WfAssigneeException ex)
        {
            if (ex == null) return;
            Status = WfExecutedStatus.Failed;
            Message = ex.Message ?? string.Empty;
            ReasonCode = ex.ReasonCode;
            ExceptionType = ex.ReasonCode;
        }
    }

    /// <summary>
    /// Status execution enumeration type
    /// 状态执行枚举类型
    /// </summary>
    public enum WfExecutedStatus
    {
        /// <summary>
        /// Default
        /// </summary>
        Default = 0,

        /// <summary>
        /// Success
        /// </summary>
        Success = 1,

        /// <summary>
        /// Failed
        /// </summary>
        Failed = 2,

        /// <summary>
        /// Exception
        /// </summary>
        Exception = 3
    }

    /// <summary>
    /// Workflow Exception Type
    /// 异常类型类
    /// </summary>
    public class WfExceptionType
    {
        //流程启动异常信息
        //Process startup exception information
        public const string Started_IsRunningAlready = "Started_IsRunningAlready";
        public const string Started_NoneExactlyProcessId = "Started_NoneExactlyProcessId";

        //流程运行异常信息
        //Process running exception information
        public const string RunApp_ErrorArguments = "RunApp_ErrorArguments";
        public const string RunApp_HasNoTask = "RunApp_HasNoTask";
        public const string RunApp_OverTasks = "RunApp_OverTasks";
        public const string RunApp_RuntimeError = "RunApp_RuntimeError";
        public const string RunApp_NotInRunning = "RunApp_NotInRunning";
        public const string RunApp_VariableRequiredFailed = "RunApp_VariableRequiredFailed";

        //流程跳转异常信息
        //Process jump execption information
        public const string Jump_ErrorArguments = "Jump_ErrorArguments";
        public const string Jump_OverOneStep = "Jump_OverOneStep";
        public const string Jump_NotActivityBackCompleted = "Jump_NotActivityBackCompleted";
        public const string Jump_OtherError = "Jump_OtherError";

        //流程撤销异常信息
        //Process withdraw exception information
        public const string Withdraw_ErrorArguments = "Withdraw_ErrorArguments";
        public const string Withdraw_NotInReady = "Withdraw_NotInReady";
        public const string Withdraw_NotCreatedByMine = "Withdraw_NotCreatedByMine";
        public const string Withdraw_HasTooMany = "Withdraw_HasTooMany";
        public const string Withdraw_PreviousIsEndNode = "Withdraw_PreviousIsEndNode";
        public const string Withdraw_SignTogetherNotAllowed = "Withdraw_SignTogetherNotAllowed";
        public const string Withdraw_IsLoopNode = "Withdraw_IsLoopNode";
        
        //流程退回异常信息
        //Process sendback exception information
        public const string Sendback_NotTaskNode = "Sendback_NotTaskNode";
        public const string Sendback_IsLoopNode = "Sendback_IsLoopNode";
        public const string Sendback_NotInRunning = "Sendback_NotInRunning";
        public const string Sendback_NotMineTask = "NotMineTask";
        public const string Sendback_IsNull = "Sendback_IsNull";
        public const string Sendback_IsTooManyPrevious = "Sendback_IsTooManyPrevious";
        public const string Sendback_NotContainedInPreviousOrStartNode = "Sendback_NotContainedInPreviousOrStartNode";
        public const string Sendback_IsStartNode = "Sendback_IsStartNode";
        public const string Sendback_HasTooManyRunningParallel = "Sendback_HasTooManyRunningParallel";

        //流程返送异常信息
        //Process resend exception information
        public const string Resend_NotTaskNode = "Resend_NotTaskNode";
        public const string Resend_WithoutBackSourceNode = "Resend_WithoutBackSourceNode";

        //流程返签异常信息
        //Process reverse exception information
        public const string Reverse_NotInCompleted = "Reverse_NotInCompleted";

        //流程加签异常信息
        //Process sign forward exception information
        public const string SignForward_ErrorArguments = "SignForward_ErrorArguments";
        public const string SignForward_NoneSigners = "SignForward_NoneSigners";
        public const string SignForward_RuntimeError = "SignForward_RuntimeError";

        // 转办
        public const string Transfer_ErrorArguments = "Transfer_ErrorArguments";
        public const string Transfer_NoneTargets = "Transfer_NoneTargets";
        public const string Transfer_NotMineTask = "Transfer_NotMineTask";
        public const string Transfer_NotInRunning = "Transfer_NotInRunning";
        public const string Transfer_RuntimeError = "Transfer_RuntimeError";
    }
}

