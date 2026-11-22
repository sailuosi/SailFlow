# sailflow

> **致敬 [Slickflow](https://github.com/besley/slickflow)**  
> 本仓库基于开源工作流引擎 [Slickflow](https://github.com/besley/slickflow) 二次改造而来。感谢 Besley / 杭州砺工科技及社区的杰出贡献。原项目采用 MIT 协议，源授权说明见 [NOTICE](./NOTICE)。

**插件式 · 可插拔 · 多数据库 · .NET 6 / 8 / 10**

自研流程引擎，迭代中。**基础流转已测验通过**。相对上游 Slickflow 已完成数据层、结构与前端改造，此后**持续独立演进**，不再同步上游源码。

---

## 核心能力一览

| # | 能力 | 一句话 |
|---|------|--------|
| 1 | **插件式架构** | StartUp / `UseXxx` 挂载注册，扩展能力与灵活度拉满 |
| 2 | **清晰分层** | pure / ext / biz 职责分明，重量级依赖可插拔替换 |
| 3 | **数据库适配** | 依赖 **mooSQL**，轻松适配各类数据库 |
| 4 | **多框架兼容** | 目标框架 `net6.0;net8.0;net10.0` 同仓多靶 |
| 5 | **能力全面插件化** | 表单、文档、配置、规则、语言包、消息与事件均可按需挂载 |
| 6 | **中国式 OA 动词** | 通过 / 驳回 / 退回 / 加签三型 / 转办 / 沟通 / 抄送…引擎齐，办理条可对号 |
| 7 | **待办人插槽** | 内置多种策略 + 业务策略管道，奇葩算人场景也能接 |
| 8 | **事件与回调** | Gate / Hook / Notifier，同步 / 异步 / Outbox 多场景插槽 |
| 9 | **强化分表策略** | 冷热归档、按年冷表、热侧白名单分表，运维不再困难 |
| 10 | **示例与 Demo** | 多种插件样例，`UseXxx` 增强即用 |

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
    .AddHook(...)
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

### 6. 中国式 OA 办理动词

把办理台上常见按钮收成引擎可复用命令；码表 Id 与 `*Process` 一一对应。图例：**默认开** = 办理条默认露出；**默认关** = 引擎齐，节点 / 宿主显式打开；**引擎齐** = 有命令，产品按钮暂缓。

#### 日常办理（默认开）

| 动词 | 码表 Id | 引擎命令 | 一句话 |
|------|---------|----------|--------|
| 通过 / 同意 | `Approve` | `RunProcess` | 办结本票，按图推进（出不出节点看 Leave） |
| 驳回 | `Reject` | `RejectProcess` | 不同意；落点由设计期 `RejectMode` 决定 |
| 退回 | `SendBack` | `SendBackProcess` | 打回修改；目标 × 再发送两维正交 |
| 加签（前 / 后 / 并） | `SignForward` | `SignForwardProcess` | 办理中临时插人，不改流程图 |
| 减签 | `RemoveAssignee` | `RemoveAssigneeProcess` | 多人节点拿掉未办完的人 |
| 转办 | `Transfer` | `TransferProcess` | 本节点交人，**不还回** |
| 沟通 | `Consult` | `ConsultProcess` | 征询参与者；源待办硬锁至阅办/收回 |
| 阅办 | `Acknowledge` | `AcknowledgeProcess` | 被沟通人关闭自己的沟通待办 |
| 收回沟通 | `RecallConsult` | `RecallConsultProcess` | 发起人取消未反馈的沟通 |
| 抄送 | `Copyto` | Host `WfCopyto` | 知会，不进审批票数 |
| 撤回 | `Withdraw` | `WithdrawProcess` | 已办侧收回上一步 |
| 重发 / 返送 | `Resend` | `ResendProcess` | 退回后按规则再送（常「返送退回人」） |
| 跳转 | `Jump` | `JumpProcess` | 管理态跳到指定活动 |
| 关闭 | `Close` | `CloseProcess` | 管理向正常结束实例 |

#### 改签 / 管理向（默认关，可开）

| 动词 | 码表 Id | 引擎命令 | 一句话 |
|------|---------|----------|--------|
| 普通加人 | `AddAssignee` | `AddAssigneeProcess` | 扁节点追加办理人；≠ 并加签 |
| 委派 | `Delegate` | `DelegateProcess` | 代办完**自动回转**原人；≠ 转办 |
| 改派 | `ChangeAssignee` | `ChangeAssigneeProcess` | 管理态差量换当前节点名单 |
| 催办 | `Urge` | `UrgeProcess` | 提醒，不改待办状态 |
| 终止 | `Terminate` | `TerminateProcess` | 强行结束运行实例 |
| 作废 | `Discard` | `DiscardProcess` | 作废实例；≠ 关闭 / 终止 |
| 修订 | `Revise` | `ReviseProcess` | 修订类路径，按场景开启 |
| 反转 | `Reverse` | `ReverseProcess` | 回转类路径；≠ 退回 |

#### 生命周期（引擎齐，码表暂缓）

| 动词 | 引擎命令 | 说明 |
|------|----------|------|
| 挂起 / 恢复 | `SuspendProcess` / `ResumeProcess` | Lifecycle 族；产品按钮暂缓 |
| 取消 | `CancelProcess` | Lifecycle 族；产品按钮暂缓 |
| 发起 | `StartProcess` | 发起页能力，不是任务办理条按钮 |

#### 办理模式（非按钮）

会签 / 或签 / 抢办 / 顺序会签是节点 **Leave**，不是办理条键：`Race` · `Collaborate` · `GroupRace` · `GroupCollaborate` · `Captain`。

**选型口诀：** 不还回 → 转办；要还回 → 委派；管理换名单 → 改派；临时插人 → 加签；知情 → 抄送；征询 → 沟通。

简单流转初测（通过 / 加签三型 / 转办 / 沟通 / 驳回 / 退回 / 返送）已通；网关叠用与边界场景仍在清单上。详解：[解说-OA动词齐备与初测.md](./doc/helps/解说-OA动词齐备与初测.md) · [解说-办理动词.md](./doc/helps/解说-办理动词.md) · [特性说明-中国式OA审批要素.md](./doc/sail/特性说明-中国式OA审批要素.md)。

### 7. 强大的待办人插槽

独立包 `sailflow.assignee`：用户任务「谁来办 / 办完能否离开」走统一管道。

- **Resolve**（创建待办前算人）× **Leave**（流出判定）两维正交  
- 内置多种策略（自选、角色组、单位映射、职务相对、抢办 / 协办 / 队长 …）  
- 业务以 **string Type** 注册或同码覆盖，**禁止**改 AIM/Mediator 堆分支  
- 宿主主数据经 Lookup 钩子接入，引擎不塞组织中台 schema

详见 [特性说明-待办人管道.md](./doc/sail/特性说明-待办人管道.md)。

### 8. 事件特性 · 回调强化

Gate（校验拦截）/ Hook（生命周期副作用）/ Task Notifier（待办通知）三线分离；支持同步执行、异步 Outbox Drain，以及委托与 class 并存的注册形态。宿主一项能力一个 `UseXxx`，开关与建表内聚。使用教程见 [doc/helps](./doc/helps/README.md)；设计论证见 [特性设计-回调与事件.md](./doc/sail/特性设计-回调与事件.md)。

### 9. 强化的分表与归档策略

面向「十万发起 / 年」级运维场景，冷热分离与分表可配、默认可关：

- **热路径**只碰热表，引擎推进 / 待办无需附带路由负担  
- **冷表按年**（`*_hist_YYYY`），预留冷库连接  
- **热侧白名单分表**（轨迹 / 变量按流程定义，默认关）  
- 引擎日志契约化，默认 Sink 的库位置与分表粒度可配  
- `UseDataLifecycle` / `UseSailData` / 立即归档 Hook 等按需挂载

详见 [分析-流程运转表存储与分表策略.md](./doc/sail/talk/分析-流程运转表存储与分表策略.md)。

### 10. 多种示例 · 轻松 UseXxx 增强

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
