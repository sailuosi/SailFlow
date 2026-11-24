using System;
using mooSQL.data;
using sailflow.lanes;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.factory;
using sailflow.hooks;
using sailflow.result;

namespace sailflow.lanes
{
    /// <summary>
    /// 驳回（极简）：不依赖办理人选目标/选人。
    /// <list type="bullet">
    /// <item>Terminate → <see cref="TerminateLane"/>（成功事件 OnProcessTerminated，ReasonCode=RejectModeTerminate）</item>
    /// <item>Previous / Starter / Specified → 自动解析落点与历史办理人 → CreateRuntimeInstanceReject</item>
    /// </list>
    /// </summary>
    public sealed class RejectLane : TransitionLaneBase
    {
        public RejectLane(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Reject;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessRejected;

        protected override string ErrorLogName => WfDefine.WF_PROCESS_REJECT_ERROR;

        /// <summary>
        /// Terminate 走 Lifecycle；其余走基类 Gate→Reject Runtime→OnProcessRejected。
        /// </summary>
        public new WfExecutedResult Execute()
        {
            EnsureControlSheet();
            var mode = RejectTargetResolver.ReadMode(Context);
            if (string.IsNullOrEmpty(mode))
            {
                var bad = WfExecutedResult.Default();
                bad.Status = WfExecutedStatus.Exception;
                bad.ReasonCode = RejectReasonCodes.RejectModeInvalid;
                bad.Message = "RejectMode invalid. Use Terminate|Previous|Starter|Specified.";
                return bad;
            }

            if (string.Equals(mode, RejectModeCodes.Terminate, StringComparison.OrdinalIgnoreCase))
                return ExecuteTerminateAsReject();

            return base.Execute();
        }

        WfExecutedResult ExecuteTerminateAsReject()
        {
            // 计划定稿：Terminate 模式发 Terminate 成功事件；Message/Reason 标明来自驳回配置
            var term = new TerminateLane(Sail, Kit, Context);
            var result = term.Execute();
            if (result.Status == WfExecutedStatus.Success)
            {
                result.ReasonCode = RejectReasonCodes.RejectModeTerminate;
                var msg = result.Message ?? "Terminate succeeded.";
                if (msg.IndexOf("RejectMode=Terminate", StringComparison.OrdinalIgnoreCase) < 0)
                    result.Message = msg.TrimEnd('.') + ". RejectMode=Terminate";
            }

            return result;
        }

        protected override bool TryLoadContext(out TransitionLaneContext ctx, ref WfExecutedResult result)
        {
            ctx = null!;
            EnsureControlSheet();

            var resolved = RejectTargetResolver.ResolveBackwardTarget(Sail, Context);
            if (!resolved.Ok)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ReasonCode = resolved.ReasonCode;
                result.Message = resolved.Message;
                return false;
            }

            // 覆盖客户端传入，保证极简、防篡改
            Context.NextActivityPerformers = new System.Collections.Generic.Dictionary<string, PerformerList>
            {
                [resolved.ActivityId!] = resolved.Performers!
            };

            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceReject(Context, ref result);
            if (result.Status == WfExecutedStatus.Exception || runtime == null)
                return false;

            ctx = ToContext(runtime);
            return true;
        }

        protected override void ExecuteCore(TransitionLaneContext ctx, WfExecutedResult result)
            => ExecuteRuntimeAndCopyResult(ctx, result);

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return Sail.Messages.GetEngineMessage("workflowservice.rejectprocess.error", error);
        }

        void EnsureControlSheet()
        {
            if (Context == null)
                return;
            Context.ControlParameterSheet ??= new ControlParameterSheet();
        }
    }
}
