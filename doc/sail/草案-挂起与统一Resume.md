# 草案 · 挂起与统一 Resume（Bookmark 思想引子）

> 状态：**草案 / 未立项**（2026-11-02）  
> 用途：从 Elsa Bookmark / Stimulus 对照中抽出**可吸收的运行时原语**，供后续演进挂钩；**不**纳入现行能力基线，**禁止**据此改 UserTask / 办理命令。  
> 灵感来源：elsa-core `Bookmark` + `Stimulus` + `RunTask`；对照 Sail `NodeMediator*Timer*|Msg*|Signal*`、任务表、JobTimer。  
> 关联：[特性说明-引擎能力基线.md](./特性说明-引擎能力基线.md) · [特性设计-回调与事件.md](./特性设计-回调与事件.md) · [草案-命令执行管道.md](./草案-命令执行管道.md) · [存疑参考功能点.md](./存疑参考功能点.md) · Elsa 对照画布（会话产物）

---

## 1. 一句话

把「非办理条的等待」从各 Mediator / 作业各自为政，收敛成：**挂起登记 → 按 Kind+Key 匹配 → 统一 Resume 续 AIM**。  
人机审批（UserTask + Assignee + OA 动词）**不在本草案范围**。

---

## 2. 为什么要记一笔（问题）

Sail 已有多种「等」：

| 场景 | 现行落点（摘要） |
|------|------------------|
| 用户办理 | `wf_task` + `Run/Reject/SendBack/…` |
| Timer | 活动实例 `OverdueDateTime` / `JobTimer*` + 作业扫表 |
| Message / Signal | 各 `NodeMediator*Msg*|Signal*` + MQ 委托 |
| 会签未齐 | 节点内 `WaitingForCompletedMore` |
| 子流程 | 父活动 Suspend，等子完成 |

痛点不在「能不能等」，而在：

1. **恢复入口分裂**：超时作业、消息回调、外部系统回报、子流程结束，没有共同的 Resume 通道。  
2. **扩展贵**：每加一种「等外部」（电子签、支付、门户任务回报）易再造 Mediator / 字段 / 作业。  
3. **横切难**：幂等、审计、指标、失败重试要在多条路径上重复补。

Elsa 用 Bookmark（停在哪）+ Stimulus Hash（凭什么醒）统一了长跑挂起；Sail 要的是**思想**，不是搬类型名与 Store。

---

## 3. 边界（红线）

| 做 | 不做 |
|----|------|
| 服务自动节点 / 边界事件 / 外部协作等待 | **不**用 Resume 替代 `Approve/Reject/SendBack/Transfer/…` |
| 与活动实例定位对齐（ProcessInstanceId + ActivityInstanceId） | **不**重写 Assignee Resolve/Leave |
| 可先只抽象契约 + Timer 一条试点 | **不**为对齐 Elsa 引入 Actor / BookmarkStore 全家桶 |
| 与 Gate/Hook/Outbox **并列**：事中挂起 ≠ 事后补调 | **不**把抄送 / Notifier 塞进 Wait Token |

与 [存疑参考功能点.md](./存疑参考功能点.md) 同态度：外来概念先记边界，默认不进码表、不进基线命令面。

---

## 4. 目标形状（概念，非 API 定稿）

```text
登记挂起 WaitToken
  Kind   : Timer | Message | Signal | ExternalCallback | SubProcess | …
  Key    : 稳定业务键（或由其算出的 Hash）
  定位   : ProcessInstanceId + ActivityInstanceId [+ Transition?]
  载荷   : 可选 JSON（恢复时输入）
  状态   : Waiting → Resumed | Cancelled | Expired
  策略   : 一次性消费（类 AutoBurn）/ 是否允许重复刺激

恢复 ResumeWaiting(kind, key, input?)
  → 查找 Waiting token
  → 幂等（已 Resumed 则短路）
  → 续 AIM / 对应 Continue Mediator
  → 标记 Resumed + 审计/指标
```

命名可叫 `WaitToken` / `IWaitResume` / `ResumeWaiting`——**待立项时再定**；本文只固定语义三角：**Kind · Key · 定位**。

### 4.1 与现行机制的映射设想

| Kind | 今日事实来源 | 未来收敛方向（设想） |
|------|--------------|----------------------|
| Timer | `JobTimer*` + Overdue | 作业只负责「到点发刺激」，真正推进走 ResumeWaiting |
| Message / Signal | Catch Mediator + MQ | 订阅回调 → ResumeWaiting(Message/Signal, name/…) |
| ExternalCallback | （缺统一） | Host/门户回报 taskId → ResumeWaiting(External, taskId) |
| SubProcess | 父 Suspend | 子完成事件 → ResumeWaiting(SubProcess, childInstanceId) |
| UserTask | 任务表 | **永不**映射到 WaitToken |

### 4.2 与 Gate / Hook / Outbox

| 机制 | 时机 |
|------|------|
| Gate | 推进**前**可拒 |
| Hook / Outbox | 推进**后**业务 / 旁路补调 |
| Wait + Resume | 推进**中途**挂起，外部条件满足后再推进 |

三者正交；Resume 成功后的路径上仍可走既有 Gate/Hook。

---

## 5. 建议分期（仅路线图，未排期）

| 阶段 | 内容 | 验收直觉 |
|------|------|----------|
| **D0 本稿** | 文档引子 + 红线 | 评审认同边界 |
| **D1 契约** | pure 内 `WaitKind` + `ResumeWaiting` 骨架；默认 no-op / 未挂载明确失败 | 编译与单测形状 |
| **D2 Timer 试点** | 仅中间 Timer：登记 token；作业改调 Resume | 一条短时 Timer 端到端 |
| **D3 Message 对齐** | 一条 MsgCatch 走同一 Resume | 与 MQ 插槽联调 |
| **D4 外部回调** | Host `UseExternalWait` 样例（门户/签章） | 不进 OA 按钮码表 |
| **Dx** | 子流程、指标、探针 `ProbeWaiting` | 按债优先级 |

任一阶段若与 Assignee / 办理动词纠缠，**停**并退回本红线。

---

## 6. 存储直觉（未选型）

可选路径（立项时二选一或渐进）：

1. **零新表**：先用活动实例现有字段 + 约定 Key 编码（适合 D2 试点）。  
2. **薄表** `wf_wait_token`（或等价）：Kind/Key/Hash/实例定位/状态/载荷——便于探针与过期清理。

禁止为 Wait 再建一套「并行任务办理表」去冒充待办。

---

## 7. 开放问题（留给立项）

1. Key 规范：全局唯一 vs 实例内唯一？是否强制 Hash？  
2. 与 `StartProcessByMessage` 的关系：启动刺激 vs 中途 Resume 是否共用 Hasher？  
3. 多 Bookmark 同活动（如多边界事件）时的匹配优先级。  
4. Resume 是否必须带 `SQLBuilder kit`（与现有事务约定对齐）。  
5. 宿主作业（超时、策略跳过）是否逐步迁到 Resume，还是长期双轨。

---

## 8. 文档地位

- **不是**引擎能力基线章节；基线仍以现行 Mediator / 命令为准。  
- 立项并 D2 验收后，再考虑写入基线「等待与恢复」小节，并改本状态为「设计中 / 已落地」。  
- 实施时同步 `progress/` 分期记录；纯讨论修订只改本文。

---

## 修订记录

| 日期 | 说明 |
|------|------|
| 2026-11-02 | 初稿：Elsa Bookmark 思想 → Sail WaitToken/ResumeWaiting 引子；红线与分期 |
