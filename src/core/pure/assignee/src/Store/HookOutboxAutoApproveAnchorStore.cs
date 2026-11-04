using System.Text.Json;
using mooSQL.data;
using saiflow.engine.Client;
using saiflow.engine.Entity;
using saiflow.engine.Manager;

namespace saiflow.assignee.Store;

/// <summary>将自动审批锚点写入 <c>wf_hook_outbox</c>（Kind=<see cref="HookOutboxKinds.AssigneeAutoApprove"/>）。</summary>
public sealed class HookOutboxAutoApproveAnchorStore : IAutoApproveAnchorStore
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public void Enqueue(SailClient sail, IReadOnlyList<AutoApproveAnchor> anchors, SQLBuilder? kit = null)
    {
        ArgumentNullException.ThrowIfNull(sail);
        if (anchors == null || anchors.Count == 0)
            return;

        var ownKit = kit == null;
        kit ??= sail.UseRunSQL();
        try
        {
            if (ownKit)
                kit.beginTransaction();

            var mgr = new HookOutboxManager(sail);
            var now = DateTime.UtcNow;
            foreach (var a in anchors)
            {
                mgr.InsertPending(kit, new HookOutboxEntity
                {
                    Kind = HookOutboxKinds.AssigneeAutoApprove,
                    HandlerName = "Assignee.AutoApprove",
                    EventType = a.StrategyCode ?? "AutoApprove",
                    ProcessId = a.ProcessId,
                    ProcessInstanceId = a.ProcessInstanceId,
                    ActivityInstanceId = a.ActivityInstanceId,
                    TaskId = a.TaskId,
                    PayloadJson = JsonSerializer.Serialize(a, JsonOptions),
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }

            if (ownKit)
                kit.commit();
        }
        catch
        {
            // SQLBuilder 无独立 rollback；ownKit 未 commit 即 Dispose 即可
            throw;
        }
        finally
        {
            if (ownKit)
                kit.Dispose();
        }
    }
}
