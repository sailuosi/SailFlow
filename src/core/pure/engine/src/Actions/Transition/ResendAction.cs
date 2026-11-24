using System;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.factory;
using sailflow.hooks;
using sailflow.result;

namespace sailflow.lanes
{
    public sealed class ResendAction : TransitionLaneBase
    {
        public ResendAction(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Resend;
        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessResent;
        protected override string ErrorLogName => WfDefine.WF_PROCESS_RESEND_ERROR;

        protected override bool TryLoadContext(out TransitionActionContext ctx, ref WfExecutedResult result)
        {
            ctx = null!;
            var runtime = Sail.RuntimeFactory.CreateRuntimeInstanceResend(Context, ref result);
            if (result.Status == WfExecutedStatus.Exception || runtime == null)
                return false;
            ctx = ToContext(runtime);
            return true;
        }

        protected override void ExecuteCore(TransitionActionContext ctx, WfExecutedResult result)
            => ExecuteRuntimeAndCopyResult(ctx, result);

        protected override string FormatFailedMessage(Exception e)
        {
            var frame = FlattenExceptionFrames(e);
            var error = string.IsNullOrEmpty(frame) ? (e.InnerException?.Message ?? e.Message) : frame;
            return Sail.Messages.GetEngineMessage("workflowservice.resendprocess.error", error);
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
    }
}
