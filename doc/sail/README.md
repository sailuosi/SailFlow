# sailflow · doc/sail

引擎侧设计与能力说明索引。

**用户指南 / 使用教程**见 [../helps/](../helps/)。  
**只读审计**见 [../audit/](../audit/)（结论归档，决策后再行动）。

## 特性说明

| 文档 | 用途 |
|------|------|
| [特性说明-引擎能力基线.md](./特性说明-引擎能力基线.md) | **演进基准**：现行命令、查询、节点、Assignee、回调、插槽与 Host 边界 |
| [特性说明-待办人管道.md](./特性说明-待办人管道.md) | **Assignee 专项**：两大时刻、R1–R10、Filter/EmptyAction/锚点 Job（§11）、`UseAssign`、扩展红线 |
| [特性说明-下一节点过滤器.md](./特性说明-下一节点过滤器.md) | **下一跳节点计算管道（已落地 S0–S2）**：`UseNextStepRoute` = Prepare + `IConditionEvaluator` + Filter；产品细表见 [OA/专题-流转分支条件.md](./OA/专题-流转分支条件.md) |
| [特性说明-副作用可靠性.md](./特性说明-副作用可靠性.md) | **旁路可靠性**：Outbox / Channel / 对账分档；批量推送削峰；产品细表见 [OA/专题-待办推送.md](./OA/专题-待办推送.md) |
| [特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md) | **OA 动词**：场景/名称、Sail 编码、执行含义；含候选未入码表项 |

## OA 产品专题 · [OA/](./OA/)

中国式 OA 办理策略专题（配置 + 引擎契约 + 宿主接线），见 [OA/README.md](./OA/README.md)。

| 文档 | 用途 |
|------|------|
| [专题-同人重复与跳过.md](./OA/专题-同人重复与跳过.md) | 同发起人 / 流程去重 / 自动跳过（含 approveType·空人） |
| [专题-委托代办.md](./OA/专题-委托代办.md) | 单笔委派 / 时段·单流程代批 / 委托链 |
| [专题-退回.md](./OA/专题-退回.md) | 退回两维 · SendBack/Resend · 与驳回/撤回划界 |
| [专题-改签.md](./OA/专题-改签.md) | 加签三子型 · 减签 · 改派 · 与转办/加批划界 |
| [专题-转办.md](./OA/专题-转办.md) | 本节点交人、不还回 · Transfer · 与委派/改派划界 |
| [专题-待办推送.md](./OA/专题-待办推送.md) | TaskNotifier · Outbox/Reliable · 门户日程/邮件 · ≠ 抄送 |
| [专题-多人办理模式.md](./OA/专题-多人办理模式.md) | Leave：会签/或签/抢办/组/队长 · 时刻 B · ≠ 按钮 |
| [专题-流转分支条件.md](./OA/专题-流转分支条件.md) | 连线条件 · 网关 · 条件袋 · UseNextStepRoute |
| [专题-超前选人.md](./OA/专题-超前选人.md) | 发起预选后续人 · Preselect · ≠ FutureAssignee 动词 |
| [专题-业务状态回写.md](./OA/专题-业务状态回写.md) | FormBiz · Sync 终态 Status · ≠ 待办推送 |
| [专题-沟通.md](./OA/专题-沟通.md) | Consult/阅办/收回 · 参与者硬锁 · ≠ 加签/抄送 |

## 内部专题 · [theme/](./theme/)

非对外特性的内部功能设计，见 [theme/README.md](./theme/README.md)。

| 文档 | 用途 |
|------|------|
| [专题-事务一致性.md](./theme/专题-事务一致性.md) | **规范**：流转动词严格同事务；kit/锁/Sync·Outbox 信封；钩子与插件派生规约 |
| [专题-流转动作骨架.md](./theme/专题-流转动作骨架.md) | **已落地**：三族动作骨架（含沟通/委派）；Host 减签办理条 / Copyto 独立 |
| [专题-对象生命周期与复用边界.md](./theme/专题-对象生命周期与复用边界.md) | **规范**：A/B/C/H、四槽、入参袋 vs 编排袋（D3 + D4-a/b 已落地） |
| [专题-主键与设计键发号契约.md](./theme/专题-主键与设计键发号契约.md) | **P0+P1 已落地**：`ISailIdSource` / `SailId` AutoKey + BPMN codec |
| [专题-办理人权重与委托体系.md](./theme/专题-办理人权重与委托体系.md) | Weight / EffectiveWeight / 委托代批；与转办·委派划界 |
| [专题-节点实例与任务.md](./theme/专题-节点实例与任务.md) | ActInstance ↔ Task 职责、基数、主/子与 OA 原语落库 |
| [专题-Task办理主体.md](./theme/专题-Task办理主体.md) | Task 多维办理主体：SignKind+SignId；精确人 vs 模糊部门/角色/池 |
| [专题-节点实例临时子规则.md](./theme/专题-节点实例临时子规则.md) | **已落地**：AI Leave 快照；加签改写子 Leave；TryCanLeave 统一判定 |
| [专题测试.md](./theme/专题测试.md) | OA 办理真人点验（一文档）：通过/加签/沟通/驳回/退回/返送已过 |
| [专题-流程实例标题与业务字段.md](./theme/专题-流程实例标题与业务字段.md) | 流程实例标题与业务字段契约 |
| [专题-钩子治理与同事务可观测.md](./theme/专题-钩子治理与同事务可观测.md) | **P0+P1 已落地**：Log 契约、Sync/CreateHook 软超时、Notifier/Outbox 日志；P2 可选 |
| [专题-Bug治理-Sync钩子同事务自锁.md](./theme/专题-Bug治理-Sync钩子同事务自锁.md) | **案例库**：第二连接自锁指纹 / 评审检查单 |
| [专题-对象解析器.md](./theme/专题-对象解析器.md) | **本切片已完成**（A0–A6 / B0–B3）；附录[范围注册清单](./theme/专题-对象解析器-范围注册清单.md) |
| [专题-IStorage.md](./theme/专题-IStorage.md) | **已完工**：十二件套 + pure 净；宿主经槽位自定义（非欠债） |

## 目录

| 文档 | 用途 |
|------|------|
| [目录-扩展插件.md](./目录-扩展插件.md) | pure/ext/biz + sleveen-admin 的 UseXxx / Resolver / Filter / Hook / Handler |
| [目录-能力成熟度.md](./目录-能力成熟度.md) | **梳理快照**：办理动词 / 算人 / 回调存储 / 文档仓健康度一览 |

## 设计与规范（现行）

| 文档 | 用途 |
|------|------|
| [架构设计-项目依赖与分层.md](./架构设计-项目依赖与分层.md) | 程序集依赖与分层哲学 |
| [业务管道特性设计.md](./业务管道特性设计.md) | SailClient / Builder / UseXxx |
| [特性设计-回调与事件.md](./特性设计-回调与事件.md) | Gate / Hook / Notifier / Outbox 设计 |
| [最佳实践-宿主能力包UseXxx.md](./最佳实践-宿主能力包UseXxx.md) | 宿主能力包挂载约定 |
| [dev/开发手册-OA命令执行链路.md](./dev/开发手册-OA命令执行链路.md) | Host → WorkflowService → Factory → Runtime → Mediator/Manager → DB |

## 活动清单

| 文档 | 用途 |
|------|------|
| [todo-产品缺口.md](./dev/todo-产品缺口.md) | 活动缺口；已接线见 [progress/2026-11-04-产品缺口接线归档.md](./progress/2026-11-04-产品缺口接线归档.md) |
| [联调清单-产品缺口.md](./dev/联调清单-产品缺口.md) | 联调勾选 |
| [../helps/解说-OA委托动词.md](../helps/解说-OA委托动词.md) | 转办 / 委派 / 委托代批（候选） |

## 讨论稿 · [talk/](./talk/)

未升格正式文档的草案 / 分析 / 存疑对照，见 [talk/README.md](./talk/README.md)。

## 归档

| 路径 | 用途 |
|------|------|
| [progress/](./progress/) | 分期落地记录 |
| [done/](./done/) | 已吸收草案 / 分析归档 / 残旧口径 |

产品壳（菜单、Host API、办理 UX）见宿主仓 `sleveen-admin/doc/sailflow-features.md`。
