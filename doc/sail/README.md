# sailflow · doc/sail

引擎侧设计与能力说明索引。

**用户指南 / 使用教程**见 [../helps/](../helps/)。

| 文档 | 用途 |
|------|------|
| [特性说明-引擎能力基线.md](./特性说明-引擎能力基线.md) | **演进基准**：现行命令、查询、节点、Assignee、回调、插槽与 Host 边界 |
| [特性说明-待办人管道.md](./特性说明-待办人管道.md) | **Assignee 专项**：两大时刻、R1–R10、Filter/EmptyAction/锚点 Job（§11）、`UseAssign`、扩展红线 |
| [分析-流程运转表存储与分表策略.md](./分析-流程运转表存储与分表策略.md) | **存储策略**：同表生命周期 vs NFlow RU/HI；10 万发起/年容量与归档/分表建议 |
| [架构设计-项目依赖与分层.md](./架构设计-项目依赖与分层.md) | 程序集依赖与分层哲学 |
| [业务管道特性设计.md](./业务管道特性设计.md) | SailClient / Builder / UseXxx |
| [特性设计-回调与事件.md](./特性设计-回调与事件.md) | Gate / Hook / Notifier / Outbox 设计 |
| [草案-挂起与统一Resume.md](./草案-挂起与统一Resume.md) | **草案引子**：Bookmark 思想 → WaitToken / ResumeWaiting；不改 UserTask |
| [草案-命令执行管道.md](./草案-命令执行管道.md) | **草案引子**：Elsa Middleware 思想 → Command Pipeline；不替换 Gate/Hook |
| [草案-引擎异步化.md](./草案-引擎异步化.md) | **草案专题（0–2 首包已落地）**：假 Async 禁令 + Clip/`ct` + 锁/Manager async；下一刀 Mediator 深链 |
| [目录-扩展插件.md](./目录-扩展插件.md) | **扩展目录**：pure/ext/biz + sleveen-admin 的 UseXxx / Resolver / Filter / Hook / Handler 与核心代码 |
| [todo-产品缺口.md](./todo-产品缺口.md) | **活动缺口**：联调 + 余债；已接线见 [progress/2026-11-04-产品缺口接线归档.md](./progress/2026-11-04-产品缺口接线归档.md) |
| [存疑参考功能点.md](./存疑参考功能点.md) | **存疑对照**：外来引擎功能点（如 AntFlow 承办）；不默认纳入基线 |
| [特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md) | **OA 动词**：场景/名称、Sail 编码、执行含义；含候选未入码表项 |
| [progress/](./progress/) | 分期落地记录（Hook、Feature DependsOn 等） |

产品壳（菜单、Host API、办理 UX）见宿主仓 `sleveen-admin/doc/sailflow-features.md`。
