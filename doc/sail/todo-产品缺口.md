# 待办：产品缺口（活动项）

> 状态：**接线已归档 · 待联调 / 余债**  
> 已完成拍板与实现清单 → [progress/2026-11-04-产品缺口接线归档.md](./progress/2026-11-04-产品缺口接线归档.md)  
> 联调步骤 → [联调清单-产品缺口.md](./联调清单-产品缺口.md)  
> 关联：[特性说明-待办人管道.md](./特性说明-待办人管道.md) R4/R10；[特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md)

---

## 优先级（余下）

| 序 | 项 | 优先级 | 说明 |
|----|----|--------|------|
| 1 | 联调 A–J / Extra 场景 | P0 | 代码债已清；运行时场景见清单（环境就绪后勾） |
| 2 | ~~R10 收口~~ | ✅ | `LegacySkipDrain` 默认 false；仅 `LegacyDrain=true` 回滚 |
| 3 | ~~`BackSrcUserId`~~ | ✅ | Workbench 取 BackSrc.`EndedUser*` |
| 4 | 空人整单 Reject 选项 | P2 | 与默认 `=2` 分离；未拍板开做 |
| 5 | Cancel / Suspend；「其他」拆 view | 暂缓 | 产品未要 |

---

## 仍开放

### 联调（全部块共用）

见 [联调清单-产品缺口.md](./联调清单-产品缺口.md)。接线已归档；**烟雾 P1–P3 已通过**；**API 场景**已补验一批：

- [x] **A** 空人策略图 `sail-uat-a`：无办理人首跳失败；NodePolicy 空人=2 + 短超时可读（A5/A7–A12 全矩阵未逐条真人点完）
- [x] **B** `sail-uat-b` Terminate/Discard 显隐 + Terminate 成功
- [x] **C** `sail-uat-c` fieldModes editable/readonly/hidden/masked + signEnable
- [x] **F** 转办同节点交人（API：Transfer admin→zhangsan）
- [x] **G** `sail-uat-g` 双人工 SendBack 成功（返送/SB1/SB2 浏览器细点仍可补）
- [x] **J** SendBack 无目标 → `SendBackTargetMissing`
- [x] **H** Leave Create→Approve→Status=2（FormBiz 首跳默认办理人 + OnFinish 同事务 Kit）
- [x] **Extra E1** 撤回闸 `WithdrawNotAllowed`（AllowWithdrawTask 默认 false）
- [x] **R2** UAT-C `signEnable=true`（办理弹窗强制签名产品点验可补）
- [x] **CA1 / R1 / U1 / R3** 改派、减签、催办、常用语（API）

脚本：`sleveen-admin/doc/scripts/sail-uat-core.ps1`（2026-11-16 晚 fails=0）。

szkfpt 体验包：常用语 / 时间线意见·签名 / 附件面板 / 分类排序 API / 委托·常用语菜单已落地；分类列表本环境不足 2 条时 sort 烟跳过。

### 产品决策（未开做）

- [ ] 是否另增「空人则 Reject 结束整单」（与默认 `assignEmptyHandlerType=2` 分离）
- [ ] 会签「自动过」若业务需要：产品替代方案（现行 SignTogether **硬拒**零人自动过）

### 技术余债

| 项 | 说明 |
|----|------|
| ~~`__sail_policy_skip__` 兜底~~ | ✅ 默认关闭 LegacyDrain；紧急 `Sail:PolicySkip:LegacyDrain=true` |
| ~~`BackSrcUserId`~~ | ✅ 取 BackSrc 活动 `EndedUserId/Name` |
| ~~流程级退回默认~~ | ✅ Extra.`sendBackTarget`/`resubmitMode` 非空覆盖节点；空则节点优先 |
| ~~J 单测矩阵~~ | ✅ `SailSendBackPolicyTests` Normalize + Resolve + FilterPrevious |
| Cancel / Suspend | 引擎有 API，码表/办理条未产品化 |
| 「其他」再拆 `view` | 设计器分组暂缓 |

### A 边界备忘（勿回退）

- 空人默认 `=2` → R4 拒绝推进（**不是**整单 `RejectProcess`）。
- 自动通过主路径：锚点 → Job 苛刻复核；零 Performer + `RunProcessAuto`；会签零人自动过禁止。
- **待办级 SLA（串行同节点）：** `sf_task.activated_datetime` / `overdue_datetime`；时钟起点=可办（Ready），勿用 `created_datetime` 当响应耗时；算式在 Host `SailNodePolicyTimeout.ComputeOverdue`；Job 扫 `task.overdue_datetime`。
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
