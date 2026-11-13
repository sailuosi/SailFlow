# Phase 1 — Gate / Hook / Notifier MVP

**Date:** 2026-10-31  
**Design:** `doc/sail/特性设计-回调与事件.md` (§12 Phase 1, §14)

## Implemented

### Engine (`sailflow.engine`)

- **Contracts** (`src/core/pure/engine/src/Hooks/`): `IFlowGate`, `IFlowHook`, `ITaskNotifier`, `HookResult`, `HookValidationItem`, `FlowHookContext`, `TaskNotifyContext`, enums (`TaskNotifyKind`, `HookMatch`, `HookMode`, `FlowOperation`), `HookRegistrationOptions`, registration DTOs, `FlowCapabilityRegistry`, `FlowCapabilityDispatcher`, `HookOutboxWorker`.
- **Client:** `SailClient.Capabilities` / `Hooks`, `CapabilityDispatcher`, `addGate` / `addHook` / `addNotifier`.
- **Hosting:** `SailFlowCapabilityExtensions` — `AddGate` / `AddHook` / `AddNotifier` (class + delegate) on `ISailCollection` and `SailClientBuilder`; `SailBuilderKeys.FlowCapabilities`; Build applies pending registrations.
- **Run path:** `WorkflowService.ProcessExecution.Run` — Gate after `CreateRuntimeInstanceAppRunning`, before `Execute`; Reject → `ApplyGateReject` (no `runprocess.error` wrap). On `OnProcessContinued` success: Sync hooks + Async Outbox enqueue.
- **Task path:** `TaskManager` — removed `SendTaskEMails` / `SendEmailFlag`; `FireTaskLifecycle` on create/complete (Sync hooks, Notifier Outbox, `Subscribe` via `EventExecutor`).
- **Outbox:** `HookOutboxEntity` (`wf_hook_outbox`), `HookOutboxManager`, schema in `SailSchemaInitializer.EnsureCoreTables`.
- **Result:** `WfExecutedResult.ValidationCode`, `ValidationDetails`, `ApplyGateReject`.
- **Events:** `EventFireTypeEnum.OnValidating = 50`, `OnTaskCreated = 60` (typo removed); `EventExecutor` expanded for Process *ing/*ed and task events (documented in source).

### Msg (`sailflow.msg`)

- `TaskNotifyChannelBase`, `DefaultMailTaskNotifier`, `UseDefaultTaskMail` (`TaskMailSailExtensions`).
- Biz: `UseBizTaskMail` → `UseDefaultTaskMail` in `BizMsgSailExtensions`.

### Removed

- `SailConfig.SendEmailFlag`, `JobAdminConfig.DefaultSendEmailFlag`, biz config binding for SendEmailFlag.

## Usage (minimal)

```csharp
var builder = SailClient.CreateBuilder()
    .UseDatabase(db)
    .AddGate("finance.pre-send", ctx =>
        HookResult.Ok(), o => { o.ProcessCodes = new[] { "EXPENSE" }; })
    .AddHook("biz.after-run", ctx => { }, o =>
    {
        o.ProcessCodes = new[] { "EXPENSE" };
        o.Events = new[] { EventFireTypeEnum.OnProcessContinued };
        o.Mode = HookMode.Sync;
    })
    .UseDefaultTaskMail();

var client = builder.Build();
// Optional: new HookOutboxWorker(client).ProcessPending();
```

Gate Reject returns `WfExecutedResult` with `Status=Failed`, `Message`, `ValidationCode`, `ValidationDetails`.

## Files touched (summary)

| Area | Paths |
|------|--------|
| New Hooks | `engine/src/Hooks/*.cs` |
| Outbox | `HookOutboxEntity.cs`, `HookOutboxManager.cs` |
| Client/Hosting | `SlickClient.cs`, `SailClientBuilder.cs`, `SailBuilderKeys.cs`, `SailFlowCapabilityExtensions.cs` |
| Service/Events | `WorkflowService.ProcessExecution.cs`, `EventFireTypeEnum.cs`, `EventExecutor.cs`, `WfExecutedResult.cs` |
| Task | `TaskManager.cs` |
| Config | `SlickConfig.cs`, `JobAdminConfig.cs`, `SailConfigBizExtensions.cs` |
| Schema | `SailSchemaInitializer.cs` |
| Msg | `msg/src/Notify/*`, `msg/src/Hosting/TaskMailSailExtensions.cs`, `BizMsgSailExtensions.cs` |

## Build

```text
dotnet build src/Sailflow.sln -f net8.0
→ 0 errors (warnings pre-existing)
```

## Remaining gaps (Phase 2+)

- Outbox Worker: no hosted scheduler / lease / CAS; manual `HookOutboxWorker.ProcessPending()`.
- Gate on `Start()` and other operations (Withdraw, SendBack, …) not wired.
- Sync hook timeout / metrics; `ValidationRejected` status enum.
- Outbox dead-letter ops UI/API; payload size governance.
- Full integration tests for whitelist, Gate Reject, multi-notifier isolation.
- `EventContext.TaskId` not added (Subscribe task handlers use activity scope only).
