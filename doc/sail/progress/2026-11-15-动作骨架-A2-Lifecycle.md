# 进度 · 动作骨架 A2（Lifecycle / Suspend·Resume）

> 日期：2026-11-15  
> 设计：[草案-非流转动作骨架.md](../草案-非流转动作骨架.md)

## 已落地

| 项 | 说明 |
|----|------|
| `FlowOperation.Suspend` / `Resume` | `HookEnums.cs` |
| `OnProcessSuspended` / `OnProcessResumed`（及 *ing） | `EventFireTypeEnum` |
| `LifecycleActionContext` / `LifecycleActionBase` | PI 为主；无 Forward；无默认 TaskLifecycle |
| `SuspendAction` / `ResumeAction` | Core 调 `ProcessInstanceManager.*(kit,…)` |
| Service | `SuspendProcess`/`ResumeProcess` → `WfExecutedResult` + Async；原 `bool` 重载保留为兼容 |
| `RunBuilder.Suspend()` / `Resume()` | 需 TaskId / ProcessInstanceId |

## 行为变化（相对旧 bool 短路径）

- 纳入 **事务 + 实例锁**（`RunAction`）
- 走 **Gate + 成功 Hook**（旧路径无）
- 写库用**同一 kit**（旧 `PIM.Suspend(id)` 曾自开连接）

## 续：Cancel / Discard / Terminate（同日）

| 项 | 说明 |
|----|------|
| `FlowOperation.Cancel` / `Discard` / `Terminate` | `HookEnums.cs` |
| `OnProcessCanceled` / `Discarded` / `Terminated`（及 *ing） | `EventFireTypeEnum` |
| `CancelAction` / `DiscardAction` / `TerminateAction` | Core 调 `PIM.*(kit,…)`；无自开事务 |
| Service / 接口 | 返回 `WfExecutedResult` + Async；旧 `bool` 签名移除 |
| `RunBuilder` | `Cancel()` / `Discard()` / `Terminate()` |
| Host（pxxt8） | `Terminate`/`Discard` Handler 与 Controller 改吃 `WfExecutedResult` |

## 行为变化（相对旧 bool 短路径）

- 纳入 **事务 + 实例锁**（`RunAction`）
- 走 **Gate + 成功 Hook**（旧路径无）
- 写库用**同一 kit**（旧 `PIM.Cancel/Discard/Terminate(runner)` 曾自开连接）

## 未做

- 产品码表默认开（仍默认关）  
- Terminate 级联待办的 TaskLifecycle 通知对账
