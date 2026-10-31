# Phase 3b — 抛光（Sync 超时 / Outbox 宿主 / 死信运维 / 单测）

**Date:** 2026-10-31  
**Design:** `doc/sail/特性设计-回调与事件.md`

## Implemented

### 1. Sync Hook 硬超时

- **`WorkflowCapabilityOptions.SyncHookTimeoutMs`**（默认 **2000**；**0** = 不限制）。
- **`SailClient.CapabilityOptions`**（默认新实例）。
- **`WorkflowCapabilityDispatcher.RunSyncHooks`**：超时路径用 **后台 Thread + `Join(timeoutMs)`**（避免 ASP.NET 同步上下文上的 sync-over-async 死锁）；超时抛 **`WorkflowSyncHookTimeoutException`**（FailFast）。
- **`IWorkflowCapabilityMetrics.IncrementSyncHookTimeout`**（Null / InMemory 已实现）。

```csharp
client.CapabilityOptions.SyncHookTimeoutMs = 2000; // 0 禁用
client.CapabilityMetrics = new InMemoryWorkflowCapabilityMetrics();
```

### 2. `UseHookOutboxDrain` 宿主

- **`SailHookOutboxDrainExtensions.UseHookOutboxDrain`** on `ISailCollection` / `SailClientBuilder`。
- **ConfigureClient**：合并选项到 **`SailClient.HookOutboxOptions`**。
- **`autoStart: true`**（opt-in）：**PostBuild** 调用 `HookOutboxDrainLoop.StartBackground`，CTS 存 **`SailClient.HookOutboxDrainCts`**（shutdown 时 `Cancel`）。
- **`HookOutboxWorker` / `HookOutboxDrainLoop`**：`options` 参数为空时优先 **`client.HookOutboxOptions`**。
- **Biz 别名：** `UseBizHookOutboxDrain`（`sailflow.biz.Hosting`）。

```csharp
builder.UseHookOutboxDrain(o => o.PollInterval = TimeSpan.FromSeconds(3), autoStart: true);
// shutdown: client.HookOutboxDrainCts?.Cancel();
```

### 3. 死信批量运维

**`HookOutboxManager`**

| Method | 说明 |
|--------|------|
| `ResetDeadBatch(kit, ids)` | 按 Id 批量复活，返回成功数 |
| `ResetAllDead(kit, limit=500)` | 最多复活 limit 条 Dead → Pending |
| `QueryDead(kit, limit, processCode?, handlerName?)` | 可选内存过滤 |
| `CountByStatus(kit, status)` | 按状态计数 |

**`HookOutboxAdmin`**：同上方法的薄门面。

### 4. 单元测试

- 工程：**`src/tests/sailflow.engine.tests/`**（net8.0, xunit），已加入 **`Sailflow.sln`**（`tests` 文件夹）。
- **`InternalsVisibleTo`** → `sailflow.engine.tests`（测 `WorkflowCapabilityMatch`）。

| 测试类 | 覆盖 |
|--------|------|
| `WorkflowCapabilityMatchTests` | All / Whitelist / ActivityCodes |
| `HookResultWfExecutedResultTests` | Reject、`ApplyGateReject`、`IsValidationReject` |
| `TaskNotifyMessageIntentMappingTests` | `FromKind` |

### 5. Perf smoke（软断言）

- **`WorkflowCapabilityMatchPerfTests`**：100 条白名单 × 10 000 次 ≈ **1 000 000** 次 `MatchesProcess`+`MatchesActivity`。
- **本机 baseline（2026-10-31）：** ~**41 ms**（阈值 **&lt; 50 ms**）。

### 6. 设计文档

- `doc/sail/特性设计-回调与事件.md` 状态行已增加 **Phase 3b** 链接。

## Build / test

```text
dotnet build G:\coding\gitee\my\sailflow\src\Sailflow.sln -f net8.0   # OK
dotnet test  src\tests\sailflow.engine.tests\sailflow.engine.tests.csproj -f net8.0
# Passed: 10, Failed: 0
```

## Files touched

| Area | Paths |
|------|--------|
| Sync timeout | `WorkflowCapabilityOptions.cs`, `WorkflowSyncHookTimeoutException.cs`, `WorkflowCapabilityDispatcher.cs`, `IWorkflowCapabilityMetrics.cs`, `SlickClient.cs` |
| Outbox drain hosting | `SailHookOutboxDrainExtensions.cs`, `SailBuilderKeys.cs`, `HookOutboxWorker.cs`, `HookOutboxDrainLoop.cs`, `BizHookOutboxSailExtensions.cs` |
| Dead-letter ops | `HookOutboxManager.cs`, `HookOutboxAdmin.cs` |
| Tests | `src/tests/sailflow.engine.tests/*`, `sailflow.engine.csproj`, `Sailflow.sln` |
| Docs | `doc/sail/特性设计-回调与事件.md`, this file |

## Remaining gaps

| Item | Notes |
|------|--------|
| **死信 UI / 管理台** | 设计文档仍列为待办；API 已有 `HookOutboxAdmin` / Manager 批量方法 |
| **Sync Hook 超时后线程** | Thread 超时后仍可能在后台跑完（未 Abort）；文档化；若需强杀需另行设计 |
| **Sync 超时单测** | 需可控慢 Hook + 短超时（易 flaky）；未加 |
| **Outbox 集成测** | 仍无 DB 夹具；Manager 批量方法未做 DB 测试 |
| **Prometheus / 正式 metrics 导出** | 仍仅接口 + InMemory |
| **CI 跑 tests** | 需在流水线中加入 `sailflow.engine.tests`（若尚未配置） |
