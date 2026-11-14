# 进度 · 动作骨架 A4（Assignee / RemoveAssignee）

> 日期：2026-11-15  
> 设计：[草案-非流转动作骨架.md](../草案-非流转动作骨架.md)

## 已落地（引擎侧）

| 项 | 说明 |
|----|------|
| `FlowOperation.RemoveAssignee` | `HookEnums.cs` |
| `OnAssigneeRemoving` / `OnAssigneeRemoved` | `EventFireTypeEnum` |
| `TaskManager.RemoveAssignees` | Closed 待移除未办待办 + `Cancelled` Notifier；刷新 AssignedUser*；禁减至 0 |
| `RemoveAssigneeAction` | Assignee 族；`FireTaskLifecycleAfterCore=false` |
| Service / IF / RunBuilder | `RemoveAssigneeProcess*` / `RemoveAssignee()` |

## 调用约定

- `TaskId`：定位当前活动实例（任意该活动上的任务即可）  
- `NextActivityPerformers`：待移除办理人（与 Transfer 目标袋同形）  
- 至少保留 1 名 Waiting/Reading 待办  

## 续：ChangeAssignee（同日）

| 项 | 说明 |
|----|------|
| `FlowOperation.ChangeAssignee` | + `OnAssigneeChanged` |
| `TaskManager.ChangeAssignees` | 差量：不在新集合 → Closed+Cancelled；缺开待办 → Insert+Created；刷新 AssignedUser* |
| `ChangeAssigneeAction` / Service / RunBuilder | 同减签约定：`TaskId` 定位；`NextActivityPerformers` = **新全集** |

## 续：Urge（同日）

| 项 | 说明 |
|----|------|
| `TaskNotifyKind.Urged` + `OnTaskUrged` / `OnProcessUrged` | Dispatcher 对 Urged 走独立事件（不再误映射 Completed） |
| `TaskManager.UrgeOpenTasks` | 不改 TaskState；可选 userId 过滤 |
| `UrgeAction` / `UrgeProcess*` | Assignee 族；空 Performers=催本活动全部打开待办 |

## 续：Delegate（同日，办完自动回转）

| 项 | 说明 |
|----|------|
| 拍板 | 委派办结**必须**自动回转原人；否则用 Transfer |
| `TaskManager.Delegate` | 关原人 + 建受托待办（`EntrustedTaskId`）；不刷新 AssignedUser* |
| `TryCompleteAsDelegationReturn` | 受托人 Complete → 回转原人 Waiting；Mediator **不**完结 AI / 不 ContinueForward |
| 挂点 | `NodeMediatorTask` / MultiSignTogether / MultiSignForward / Revise |
| `DelegateAction` / `DelegateProcess*` | Assignee 族 |

## 未做（产品面 / 候选）

- Host Handler + 办理条码表（减签/改派/催办/委派均未挂）  
- Host 侧注册 `TaskKinds` 含 `Urged` 的 Notifier 通道  
- `Copyto` 保持 Host 独立体系
