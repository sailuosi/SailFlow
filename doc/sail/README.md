# sailflow · doc/sail

引擎侧设计与能力说明索引。

**用户指南 / 使用教程**见 [../helps/](../helps/)。  
**只读审计**见 [../audit/](../audit/)（结论归档，决策后再行动）。

## 特性说明

| 文档 | 用途 |
|------|------|
| [特性说明-引擎能力基线.md](./特性说明-引擎能力基线.md) | **演进基准**：现行命令、查询、节点、Assignee、回调、插槽与 Host 边界 |
| [特性说明-待办人管道.md](./特性说明-待办人管道.md) | **Assignee 专项**：两大时刻、R1–R10、Filter/EmptyAction/锚点 Job（§11）、`UseAssign`、扩展红线 |
| [特性说明-下一节点过滤器.md](./特性说明-下一节点过滤器.md) | **下一跳节点计算管道（已落地 S0–S2）**：`UseNextStepRoute` = Prepare + `IConditionEvaluator` + Filter |
| [特性说明-副作用可靠性.md](./特性说明-副作用可靠性.md) | **旁路可靠性**：Outbox / Channel / 对账分档；批量推送削峰 |
| [特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md) | **OA 动词**：场景/名称、Sail 编码、执行含义；含候选未入码表项 |

## OA 产品专题 · [OA/](./OA/)

中国式 OA 办理策略专题（配置 + 引擎契约 + 宿主接线），见 [OA/README.md](./OA/README.md)。

| 文档 | 用途 |
|------|------|
| [专题-同人重复与跳过.md](./OA/专题-同人重复与跳过.md) | 同发起人 / 流程去重 / 自动跳过（含 approveType·空人） |
| [专题-委托代办.md](./OA/专题-委托代办.md) | 单笔委派 / 时段·单流程代批 / 委托链 |

## 内部专题 · [theme/](./theme/)

非对外特性的内部功能设计，见 [theme/README.md](./theme/README.md)。

| 文档 | 用途 |
|------|------|
| [专题-流转动作骨架.md](./theme/专题-流转动作骨架.md) | **已落地**：三族动作骨架（含沟通/委派）；Host 减签办理条 / Copyto 独立 |
| [专题-对象生命周期与复用边界.md](./theme/专题-对象生命周期与复用边界.md) | **规范**：A/B/C/H、四槽、入参袋 vs 编排袋（D3 + D4-a/b 已落地） |
| [专题-主键与设计键发号契约.md](./theme/专题-主键与设计键发号契约.md) | **P0+P1 已落地**：`ISailIdSource` / `SailId` AutoKey + BPMN codec |
| [专题-办理人权重与委托体系.md](./theme/专题-办理人权重与委托体系.md) | Weight / EffectiveWeight / 委托代批；与转办·委派划界 |
| [专题-流程实例标题与业务字段.md](./theme/专题-流程实例标题与业务字段.md) | 流程实例标题与业务字段契约 |

## 目录

| 文档 | 用途 |
|------|------|
| [目录-扩展插件.md](./目录-扩展插件.md) | pure/ext/biz + sleveen-admin 的 UseXxx / Resolver / Filter / Hook / Handler |

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
