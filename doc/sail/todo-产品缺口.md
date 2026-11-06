# 待办：产品缺口（活动项）

> 状态：**接线已归档 · 待联调 / 余债**  
> 已完成拍板与实现清单 → [progress/2026-11-04-产品缺口接线归档.md](./progress/2026-11-04-产品缺口接线归档.md)  
> 联调步骤 → [联调清单-产品缺口.md](./联调清单-产品缺口.md)  
> 关联：[特性说明-待办人管道.md](./特性说明-待办人管道.md) R4/R10；[特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md)

---

## 优先级（余下）

| 序 | 项 | 优先级 | 说明 |
|----|----|--------|------|
| 1 | 联调 A–J | P0 | 清单勾选；通过后回写本文件 |
| 2 | R10 收口：去掉魔法 skip 兜底 | P1 | 主路径已是锚点 + 零 Performer |
| 3 | `BackSrcUserId` / 退回单测 | P1 | §G/J 余债 |
| 4 | 空人整单 Reject 选项 | P2 | 与默认 `=2` 分离；未拍板开做 |
| 5 | Cancel / Suspend；「其他」拆 view | 暂缓 | 产品未要 |

---

## 仍开放

### 联调（全部块共用）

见 [联调清单-产品缺口.md](./联调清单-产品缺口.md)。通过后在本表勾选：

- [ ] **A** 空人/同人/超时（含 A5 锚点/`RunProcessAuto`、A10 approveType）
- [ ] **B** 终止/作废显隐与调用
- [ ] **C** 字段 mode（含 masked）
- [ ] **F** 转办同节点交人 + 轨迹
- [ ] **G/J** 返送落点；ForwardNormal 沿出边；旧图 merge 默认
- [ ] **H** 请假发起→同意→Status=通过

### 产品决策（未开做）

- [ ] 是否另增「空人则 Reject 结束整单」（与默认 `assignEmptyHandlerType=2` 分离）
- [ ] 会签「自动过」若业务需要：产品替代方案（现行 SignTogether **硬拒**零人自动过）

### 技术余债

| 项 | 说明 |
|----|------|
| `__sail_policy_skip__` 兜底 | JobWorker 仍扫遗留魔法待办；目标只留锚点 Job |
| `BackSrcUserId` | 退回人身份勿只靠 `CreatedUser*` |
| 流程级退回默认 | 覆盖节点 NodePolicy 的流程级策略 |
| J 单测矩阵 | 两维 × 边界；非法组合 Reason |
| Cancel / Suspend | 引擎有 API，码表/办理条未产品化 |
| 「其他」再拆 `view` | 设计器分组暂缓 |

### A 边界备忘（勿回退）

- 空人默认 `=2` → R4 拒绝推进（**不是**整单 `RejectProcess`）。
- 自动通过主路径：锚点 → Job 苛刻复核；零 Performer + `RunProcessAuto`；会签零人自动过禁止。
- **待办级 SLA（串行同节点）：** `wf_task.activated_datetime` / `overdue_datetime`；时钟起点=可办（Ready），勿用 `created_datetime` 当响应耗时；算式在 Host `SailNodePolicyTimeout.ComputeOverdue`；Job 扫 `task.overdue_datetime`。
- 细则与实现索引见归档文档 §A 与 [特性说明-待办人管道.md](./特性说明-待办人管道.md) §11.6。

---

## 已归档索引

| 块 | 归档要点 |
|----|----------|
| A | EmptyAction/Filter、超时 Job、R10 锚点/零 Performer/SignTogether 边界 |
| B | 待办/其他分组；Terminate/Discard |
| C | 四码 FieldModes |
| D | 宿主/引擎文档对齐 |
| F | TransferProcess + 办理条 |
| G/J | `sendBackTarget` × `resubmitMode` 落盘与办理台 |
| H | FormBiz Handler（Leave/Dynamic） |
| I | ButtonsOperation Dispatcher |

历史录入与拍板日期见归档文头；烟雾探针：`sleveen-admin/doc/scripts/sail-probe-smoke.ps1`。
