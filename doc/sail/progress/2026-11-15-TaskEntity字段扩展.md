# Progress · TaskEntity / 活动办理对象统一 Sign*

> 日期：2026-11-15 · **无历史债务**：不保留旧列名/旧属性别名，存量库重建或迁移后即新口径。

## 口径

| 层 | 字段 |
|----|------|
| 任务 `wf_task` | `SignId`/`SignName`/`SignKind`；时钟 `SendAt`/`GotAt`/`ActiveAt`/`DueAt`/`CompletedAt`；`Idx`/`IsRead`/`ReadAt`/`Msg`/`RowPara` |
| 活动 `wf_act_instance` | 汇总 `SignIds`/`SignNames`（列 `ai_sign_ids`/`ai_sign_names`） |
| Hook/Notify 上下文 | `SignId`/`SignName` |
| 方法 | `GenerateActivitySignInfo`、`GenerateActivitySignIds`/`Names`、`RefreshActivitySignFromOpenTasks` |

办理对象列段一律 **`sign`**（`ai_sign_*` / `tk_sign_*`），不用 `asgn`。`AssigneeRole` 仍表示办理角色（常规/参与者），与 Sign* 身份键正交。
