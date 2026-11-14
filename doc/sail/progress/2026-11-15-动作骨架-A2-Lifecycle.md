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

## 未做

- Terminate / Discard / Cancel 迁入 Lifecycle  
- 产品码表 / Host 按钮接线（仍暂缓）
