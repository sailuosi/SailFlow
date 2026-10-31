# Phase 2 — Outbox Worker + msg 推送骨架 + Start Gate

**Date:** 2026-10-31  
**Design:** `doc/sail/特性设计-回调与事件.md` (§12 Phase 2)

## Implemented

### Outbox Worker (engine)

- **`HookOutboxStatus.Processing`** + CAS 认领：`HookOutboxManager.TryClaimPending`（Pending→Processing，`doUpdate` 带 `Status==Pending` 条件）。
- **Ops：** `ResetDead(kit, id)`、`QueryDead` / `QueryByStatus`。
- **`HookOutboxOptions`：** `MaxAttempts`（默认 5）、`BatchSize`（默认 50）。
- **`HookOutboxWorker`：** 认领单独短事务；每条 dispatch + `MarkSucceeded`/`MarkFailed` 各自短事务；`ProcessPendingAsync(CancellationToken)`；认领失败 `Trace.TraceWarning` 并抛出。
- **`WorkflowCapabilityDispatcher`：** `DispatchPendingOutbox` 使用 `TryClaimPending`；`DispatchClaimedOutbox(Async)` 逐条处理；死信 `Trace.TraceWarning`。
- **Notifier + Outbox 语义：** `TaskNotifyChannelBase` 默认吞异常（`RethrowFailures=false`）。`DefaultMailTaskNotifier` 继承该行为 → **`NotifyAsync` 无 throw 时 Outbox 记 Succeeded**，即使 SMTP 失败也只走 `OnFailed` 日志。若需 Outbox 重试，实现类设 `RethrowFailures => true` 或不用骨架吞异常。

### msg 骨架

- **`TaskNotifyMessageIntent`** + `TaskNotifyMessageIntentMapping.FromKind`（Open: Created/Entrusted；Close: Completed/Cancelled）。
- **`TaskNotifyChannelBase`：** `GetIntent` 辅助、`ITaskNotifyFailureHandler` 属性、结构化 `OnSucceeded`/`OnFailed` Trace（Kind/ProcessCode/TaskId）。
- **`SampleHttpTaskNotifier`**（`msg/src/Notify/Samples/`）：POST JSON 回调样例。
- **biz：** `UseBizTaskNotify` → `UseDefaultTaskMail`（与 `UseBizTaskMail` 等价）。

### Gate on Start

- **`WorkflowService.Start(kit)`：** `CreateRuntimeInstanceStartup` 成功后、`Execute` 前：`RunGates`（`WorkflowOperation.Start`，开始节点 ActivityId/Code）；Reject → `ApplyGateReject`。
- **`OnProcessStarted` 成功：** Sync hooks + Async Outbox（与 Run 的 `OnProcessContinued` 模式一致）。

### 文档样例（engine）

- `Hooks/Samples/FinancePreSendGate.cs`、`FinanceAggregateHook.cs`（注册示例类，非自动挂载）。

## Usage

```csharp
var worker = new HookOutboxWorker(client, new HookOutboxOptions
{
    MaxAttempts = 5,
    BatchSize = 50,
});

// 同步 drain 一批
var n = worker.ProcessPending();

// 异步 + 取消
var n2 = await worker.ProcessPendingAsync(cancellationToken: ct);

// 运维：死信重放
var om = new HookOutboxManager(client);
using var kit = client.UseRunSQL();
om.ResetDead(kit, deadRowId);
kit.commit();
```

## Finance Gate / Hook 注册示例

```csharp
SailClient.CreateBuilder()
    .UseDatabase(db)
    .UseWorkflowGate(new FinancePreSendGate(), o => o.ProcessCodes = new[] { "EXPENSE" })
    .UseWorkflowHook(new FinanceAggregateHook(), o =>
    {
        o.ProcessCodes = new[] { "EXPENSE" };
        o.Events = new[] { EventFireTypeEnum.OnProcessContinued };
        o.Mode = HookMode.AsyncOutbox;
    })
    .UseDefaultTaskMail()
    .Build();
```

## Files touched

| Area | Paths |
|------|--------|
| Outbox | `HookOutboxEntity.cs`, `HookOutboxManager.cs`, `HookOutboxWorker.cs`, `HookOutboxOptions.cs` |
| Dispatcher | `WorkflowCapabilityDispatcher.cs` |
| Start Gate | `WorkflowService.ProcessExecution.cs` |
| Samples | `Hooks/Samples/Finance*.cs` |
| msg | `TaskNotifyChannelBase.cs`, `TaskNotifyMessageIntent.cs`, `ITaskNotifyFailureHandler.cs`, `Notify/Samples/SampleHttpTaskNotifier.cs` |
| biz | `BizMsgSailExtensions.cs` |
| Docs | `特性设计-回调与事件.md`, this file |

## Build

```text
dotnet build src/Sailflow.sln -f net8.0
```

## Remaining gaps (Phase 3+)

- **Hosted scheduler**（BackgroundService / 定时 `ProcessPendingAsync`）未内置。
- **Processing 僵尸行**：无 `leased_until` 列；崩溃后 Processing 需运维改回 Pending 或后续加 lease 回收。
- **Gate** 未接 Withdraw / SendBack / Resend 等其它 `WorkflowOperation`。
- Sync hook 超时 / metrics；`ValidationRejected` 状态细化。
- Outbox 死信管理 API/UI；payload 体积治理。
- `SampleHttpTaskNotifier` 未接集成测试。
