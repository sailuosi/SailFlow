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

## 未做（产品面）

- Host Handler + 办理条码表（默认关，待产品立项）  
- `ChangeAssignee`（改派）仍候选  
