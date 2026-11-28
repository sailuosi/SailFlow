# 专题 · 办理缓急与提醒（Hurry / Tips / RowPara）

> 状态：已接线引擎字段与命令入参（2026-11-28）  
> 关联：[特性说明-中国式OA审批要素.md](../特性说明-中国式OA审批要素.md)

## 字段

| 属性 | 列 | 语义 |
|------|-----|------|
| `Idx` | `tk_idx` | **会签队列办理序号**（非缓急） |
| `Hurry` | `tk_hurry` | 缓急：0无 1平急 2加急 3特急 4特提 |
| `Tips` | `tk_tips` (500) | 提醒内容；打开待办可见 |
| `RowPara` | `tk_row_para` (1000) | 扩展 querystring；提醒方式键 `remindWay` |

## 传递

`FlowCommandContext` 一等字段 `Hurry` / `Tips` / `RowPara` → POST 命令体 → `TaskManager` 建待办写入。  
**不要**放进 `Conditions`。意见仍走 `Conditions.comment` → `Msg`。

## RowPara

格式 `key=val&key2=val2`（URL 编码）。例：`remindWay=sms,mail`。合并用 `RowParaHelper`。
