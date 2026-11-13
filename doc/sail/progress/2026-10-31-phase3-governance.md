# Phase 3 — 体验与治理（Outbox lease / Gate 全覆盖 / drain / metrics）

**Date:** 2026-10-31  
**Design:** `doc/sail/特性设计-回调与事件.md` (§12 Phase 3, §13 验收)

## Implemented

### Outbox lease + 僵尸 Processing 回收

- **`HookOutboxEntity.LeasedUntil`**（`leased_until`）。
- **`HookOutboxOptions`：** `ProcessingStaleSeconds`（默认 300）、`LeaseSeconds`（默认 120）、`PollInterval`（默认 5s）。
- **`TryClaimOne`：** Pending→Processing，写入 lease。
- **`ReclaimStaleProcessing(kit, staleBeforeUtc)`：** lease 过期或无 lease 且 `UpdatedAt` 过旧 → Pending。
- **`ResetProcessing(kit, id)`：** 运维强制回 Pending。
- **`HookOutboxWorker`：** 认领事务内先 reclaim 再 claim。

**已有库：** 需跑 `EnsureCoreTables` / schema 迁移以加列 `leased_until`（`SailSchemaInitializer` 实体驱动 alter）。

### Gate + 成功 Hook（全操作）

- **`FlowCapabilityPipeline`：** `BuildGateContext` / `BuildHookContext` / `TryGate` / `AfterSuccess`。
- **已接线：** Start、Run、Withdraw、SendBack、Resend、Revise、Reverse、Reject、Close、Jump、SignForward（Create 成功 → Gate → Execute；成功回调 Sync + Async Outbox）。

### Hosted drain（无 MS Hosting 硬依赖）

- **`HookOutboxDrainLoop.RunAsync`** + **`StartBackground(client, options)`** 返回 `CancellationTokenSource`。

```csharp
var cts = HookOutboxDrainLoop.StartBackground(client, new HookOutboxOptions
{
    PollInterval = TimeSpan.FromSeconds(5),
    LeaseSeconds = 120,
    ProcessingStaleSeconds = 300,
});
// shutdown: cts.Cancel();
```

### 轻量 metrics

- **`IFlowCapabilityMetrics`** / **`NullFlowCapabilityMetrics`** / **`InMemoryFlowCapabilityMetrics`**。
- **`SailClient.CapabilityMetrics`**（默认 Null）。
- Dispatcher / Worker 路径递增：GateReject、SyncHook、OutboxEnqueue/Succeeded/Failed/Dead、NotifierDispatch。

### 校验拒绝识别（§14 #9）

- **未**新增 `WfExecutedStatus` 枚举值。
- **`WfExecutedResult.IsValidationReject()`** / **`IsBusinessValidationFailure()`**（`Failed` + `ValidationCode != null`）。

### msg 骨架 ↔ 死信

- **`TaskNotifyChannelBase.FailureHandler`** 文档说明与 Outbox Dead 分离；`RethrowFailures` 控制 Outbox 重试语义（Phase 2 行为不变）。

## Ops 示例

```csharp
var om = new HookOutboxManager(client);
using var kit = client.UseRunSQL();
kit.beginTransaction();
om.ReclaimStaleProcessing(kit, DateTime.UtcNow.AddSeconds(-300));
om.ResetProcessing(kit, stuckId);
om.ResetDead(kit, deadId);
kit.commit();
```

## Gate 按操作过滤

注册 Gate 时可通过 **`FlowHookContext.Operation`** 区分场景，例如仅拦截 Withdraw：

```csharp
.AddGate("no-withdraw", ctx =>
    ctx.Operation == FlowOperation.Withdraw
        ? HookResult.Reject("WITHDRAW_BLOCKED", "不允许撤回")
        : HookResult.Ok(),
    o => o.ProcessCodes = new[] { "EXPENSE" })
```

## Files touched

| Area | Paths |
|------|--------|
| Outbox | `HookOutboxEntity.cs`, `HookOutboxManager.cs`, `HookOutboxWorker.cs`, `HookOutboxOptions.cs`, `HookOutboxDrainLoop.cs` |
| Pipeline | `FlowCapabilityPipeline.cs` |
| Metrics | `IFlowCapabilityMetrics.cs` |
| Wire | `WorkflowService.ProcessExecution.cs`, `FlowCapabilityDispatcher.cs`, `SlickClient.cs`, `WfExecutedResult.cs` |
| msg | `TaskNotifyChannelBase.cs` (doc) |
| Docs | `特性设计-回调与事件.md`, this file |

## Build

```text
dotnet build G:\coding\gitee\my\sailflow\src\Sailflow.sln -f net8.0
```

## Remaining gaps

- 正式性能基准（无注册 / 白名单未命中 / Gate Reject / Sync 命中）。
- Outbox 死信管理 UI / 批量 API。
- Sync Hook 可配置硬超时。
- `UseHookOutboxDrain` 管道扩展（当前仅 `StartBackground` 手动启动）。
- 集成测试（HTTP Notifier、lease 回收并发）。
