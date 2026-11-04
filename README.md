# sailflow

> **致敬 [Slickflow](https://github.com/besley/slickflow)**  
> 本仓库基于开源工作流引擎 [Slickflow](https://github.com/besley/slickflow) 二次改造而来。感谢 Besley / 杭州砺工科技及社区的杰出贡献。原项目采用 MIT 协议，源授权说明见 [NOTICE](./NOTICE)。

**插件式 · 可插拔 · 多数据库 · .NET 6 / 8 / 10**

自研流程引擎，迭代中。暂勿生产中使用，可能有 break change。相对上游 Slickflow 已完成数据层、结构与前端改造，此后**持续独立演进**，不再同步上游源码。

---

## 核心能力一览

| # | 能力 | 一句话 |
|---|------|--------|
| 1 | **插件式架构** | StartUp / `UseXxx` 挂载注册，扩展能力与灵活度拉满 |
| 2 | **清晰分层** | pure / ext / biz 职责分明，重量级依赖可插拔替换 |
| 3 | **数据库适配** | 依赖 **mooSQL**，轻松适配各类数据库 |
| 4 | **多框架兼容** | 目标框架 `net6.0;net8.0;net10.0` 同仓多靶 |
| 5 | **能力全面插件化** | 表单、文档、配置、规则、语言包、消息与事件均可按需挂载 |
| 6 | **待办人插槽** | 内置多种策略 + 业务策略管道，奇葩算人场景也能接 |
| 7 | **事件与回调** | Gate / Hook / Notifier，同步 / 异步 / Outbox 多场景插槽 |
| 8 | **强化分表策略** | 冷热归档、按年冷表、热侧白名单分表，运维不再困难 |
| 9 | **示例与 Demo** | 多种插件样例，`UseXxx` 增强即用 |

---

### 1. 插件式架构 · StartUp 式挂载

对齐 ASP.NET Core 的「能力包启用」心智：一项可开关能力 = 一个 `UseXxx`。在 `SailClientBuilder` 上链式声明，启用判定、建表、注册 Hook/Notifier 均内聚在扩展内部。

```csharp
var client = SailClient.CreateBuilder(configuration)
    .UseDatabase(...)
    .UseBizAssignees()
    .UseBizRules()
    .UseBizMsg()
    .UseBizLocalize()
    .UseSailData()          // 归档 / 分表 / 日志 Sink
    .UseFlowHook(...)
    .Build();
```

构建期 `builder.UseXxx` 与构建后 `client.useXxx` 双轨并存；宿主也可自写能力包扩展，与引擎管道同一套语言。详见 [最佳实践-宿主能力包UseXxx.md](./doc/sail/最佳实践-宿主能力包UseXxx.md)。

### 2. 分层清晰 · 重依赖可拔插

```text
Host  →  biz（组合根 / UseBiz*）
           →  core/ext（可选实现 + IronPython / RabbitMQ / NRules …）
                 →  core/pure（引擎骨架 + 契约，禁止重型 NuGet）
                       →  mooSQL
```

- **pure**：流程语义、管道骨架、跨切契约  
- **ext**：可替换实现；重依赖只允许出现在此层  
- **biz**：产品默认门面（`UseBizJson` / `UseBizAssignees` …）

仅引用程序集而不 `UseXxx`，插槽仍为空——显式启用，避免隐式拖入重依赖。详见 [架构设计-项目依赖与分层.md](./doc/sail/架构设计-项目依赖与分层.md)。

### 3. 强化的数据库适配

数据访问经 **mooSQL**（`sailflow.basis`），方言与连接可配，便于在 SQL Server、MySQL、PostgreSQL、SQLite 等库间切换。Schema 初始化、业务库与引擎库分离均可由宿主 `UseDatabase` / `UseSchema` 声明。

### 4. .NET 6 / 8 / 10 兼容

核心与扩展包统一多靶：`net6.0;net8.0;net10.0`。宿主可按现网运行时选型，无需为版本分裂维护多份引擎源码。

### 5. 横切能力全面插件化

| 能力 | 代表扩展 | 落点 |
|------|----------|------|
| 表单 | `UseForm` | `sailflow.form` |
| 文档输出 | biz.docs | `sailflow.biz.docs` |
| 配置 / Schema | `UseSchema` | biz 组合根 |
| 业务规则 | `UseBizRules` / `UseBizRuleEngines` | `sailflow.bizRule`（NRules） |
| 语言包 | `UseLocalize` / `UseBizLocalize` | `sailflow.localize` |
| 消息队列 | `UseBizMsg` / `UseRabbitMq` | `sailflow.msg` |
| 脚本动作 | `UseBizAction` | `sailflow.action`（IronPython） |
| 数据生命周期 | `UseSailData` / `UseBizData` | `sailflow.data` |
| AI 节点 | `UseBizAI` | `sailflow.biz.AI` |

### 6. 强大的待办人插槽

独立包 `sailflow.assignee`：用户任务「谁来办 / 办完能否离开」走统一管道。

- **Resolve**（创建待办前算人）× **Leave**（流出判定）两维正交  
- 内置多种策略（自选、角色组、单位映射、职务相对、抢办 / 协办 / 队长 …）  
- 业务以 **string Type** 注册或同码覆盖，**禁止**改 AIM/Mediator 堆分支  
- 宿主主数据经 Lookup 钩子接入，引擎不塞组织中台 schema

详见 [特性说明-待办人管道.md](./doc/sail/特性说明-待办人管道.md)。

### 7. 事件特性 · 回调强化

Gate（校验拦截）/ Hook（生命周期副作用）/ Task Notifier（待办通知）三线分离；支持同步执行、异步 Outbox Drain，以及委托与 class 并存的注册形态。宿主一项能力一个 `UseXxx`，开关与建表内聚。使用教程见 [doc/helps](./doc/helps/README.md)；设计论证见 [特性设计-回调与事件.md](./doc/sail/特性设计-回调与事件.md)。

### 8. 强化的分表与归档策略

面向「十万发起 / 年」级运维场景，冷热分离与分表可配、默认可关：

- **热路径**只碰热表，引擎推进 / 待办无需附带路由负担  
- **冷表按年**（`*_hist_YYYY`），预留冷库连接  
- **热侧白名单分表**（轨迹 / 变量按流程定义，默认关）  
- 引擎日志契约化，默认 Sink 的库位置与分表粒度可配  
- `UseDataLifecycle` / `UseSailData` / 立即归档 Hook 等按需挂载

详见 [分析-流程运转表存储与分表策略.md](./doc/sail/分析-流程运转表存储与分表策略.md)。

### 9. 多种示例 · 轻松 UseXxx 增强

仓库内置 Gate / Hook / Notifier 等样例（如财务预发送 Gate、聚合 Hook、HTTP 待办通知），biz 层提供默认 `UseBiz*` 门面；宿主可复制能力包模板自建插件。用户指南见 [doc/helps](./doc/helps/README.md)；设计与进度见 [doc/sail](./doc/sail/README.md)。

---

## 快速感知

```text
src/
├── core/pure/     engine · baseHub · basis · assignee · AI
├── core/ext/      form · msg · bizRule · action · localize · data · …
├── biz/           host（组合根）· query · docs · AI
└── tests/
```

引擎能力基准（命令、查询、节点、插槽边界）：[特性说明-引擎能力基线.md](./doc/sail/特性说明-引擎能力基线.md)。  
产品壳（菜单、Host API、办理 UX）见宿主仓 **sleveen-admin**。

---

## License

本仓库采用 [MIT License](./LICENSE) 开源。`LICENSE` 同时保留上游（Hangzhou Ligong Technology Co., Ltd.）与本仓库（sleveen / sailflow contributors）版权声明。溯源与改造说明见 [NOTICE](./NOTICE)。

**分支版本锁定（上游溯源）：** `Slickflow.Engine` **3.5.0** · **MIT**
