using System.Collections.Generic;
using sailflow.engine.Client;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Xpdl;

namespace sailflow.engine.NextStep;

/// <summary>下一跳管道运行时入口（Prepare 冲刷条件袋）。</summary>
public static class NextStepRouteRuntime
{
    /// <summary>
    /// 在结构演算前对条件袋执行 Client 级 Prepare（同袋原地写）。
    /// 未挂管道时原样返回（必要时新建可写袋）。
    /// </summary>
    public static IDictionary<string, string> ApplyPrepare(
        SailClient sail,
        IDictionary<string, string> conditions,
        Activity currentActivity,
        ProcessEntity processEntity,
        NextStepRoutePhase phase,
        FlowCommandContext runner = null,
        string activityInstanceId = null)
    {
        var bag = EnsureWritableBag(conditions);
        var pipeline = sail?.NextStepRoute;
        if (pipeline == null)
            return bag;

        var ctx = new NextStepRouteContext
        {
            Sail = sail,
            Runner = runner,
            ProcessId = processEntity?.ProcessId,
            ProcessCode = processEntity?.ProcessCode,
            Version = processEntity?.Version,
            CurrentActivityId = currentActivity?.ActivityId,
            CurrentActivityCode = currentActivity?.ActivityCode,
            ActivityInstanceId = activityInstanceId,
            Conditions = bag,
            Phase = phase
        };
        if (runner != null)
        {
            ctx.AppInstanceId = runner.AppInstanceId;
            ctx.UserId = runner.UserId;
            if (string.IsNullOrEmpty(ctx.ProcessId))
                ctx.ProcessId = runner.ProcessId;
            if (string.IsNullOrEmpty(ctx.Version))
                ctx.Version = runner.Version;
        }

        pipeline.Prepare(ctx);
        return bag;
    }

    static IDictionary<string, string> EnsureWritableBag(IDictionary<string, string> conditions)
    {
        if (conditions == null)
            return new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
        if (conditions is Dictionary<string, string>)
            return conditions;
        // 非可变实现：拷贝一份再写
        return new Dictionary<string, string>(conditions, System.StringComparer.OrdinalIgnoreCase);
    }
}
