using System.Diagnostics;

namespace sailflow.engine.NextStep;

internal static class NextStepRouteDiagnostics
{
    const string Prefix = "[sailflow.engine.NextStep] ";

    internal static void OptionsSkip(string kind, string impl, string processCode, string activityCode) =>
        Trace.TraceInformation(
            Prefix + "{0} Options-skip impl={1} processCode={2} activityCode={3}",
            kind, impl, processCode ?? "", activityCode ?? "");

    internal static void IsMatchSkip(string kind, string impl) =>
        Trace.TraceInformation(Prefix + "{0} IsMatch-skip impl={1}", kind, impl);

    internal static void PrepareWrote(string impl, string key, string value) =>
        Trace.TraceInformation(
            Prefix + "Prepare write impl={0} key={1} value={2}",
            impl, key, value ?? "");

    internal static void EvaluatorNull(string impl) =>
        Trace.TraceInformation(Prefix + "Evaluator null→next impl={0}", impl);

    internal static void EvaluatorAllNull() =>
        Trace.TraceWarning(Prefix + "Evaluator chain all-null → false");

    internal static void FilterApplied(string impl) =>
        Trace.TraceInformation(Prefix + "Filter applied impl={0}", impl);

    internal static void AndSplitGuard(string activityId, string reason) =>
        Trace.TraceWarning(
            Prefix + "AndSplitGuard restore activityId={0} reason={1}",
            activityId ?? "", reason ?? "");

    internal static void ZeroCandidates() =>
        Trace.TraceWarning(Prefix + "Filter resulted in zero enabled candidates");
}
