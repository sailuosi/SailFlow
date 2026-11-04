# SailFlow 事件体系完整使用教程（Gate / Hook / Notifier / Outbox）

> 状态：对照 **Phase 1–3b 已落地代码**（`saiflow.engine.Hooks` + `saiflow.msg`）整理的使用教程，非设计草案。  
> 设计全文：[特性设计-回调与事件.md](../sail/特性设计-回调与事件.md) · 能力基线：[特性说明-引擎能力基线.md](../sail/特性说明-引擎能力基线.md)  
> 源码主目录：`src/core/pure/engine/src/Hooks/` · Outbox 表实体：`HookOutboxEntity`（`wf_hook_outbox`）

---

## 前置说明

- **按需开启**：不调用 `UseFlowGate` / `UseFlowHook` / `UseTaskNotifier`（或总包 `UseFlowAspects`）时，主路径不跑业务回调；默认邮件须显式 `UseDefaultTaskMail()`。
- **分层**：契约与分发在 **pure 引擎**（`saiflow.engine`）；待办推送骨架与默认 SMTP 在 **ext/msg**（`saiflow.msg`）。不是「整套事件只在 ext」——扩展实现挂在引擎插槽上。
- **执行顺序（主路径）**：

```
Gate（推进落库前，可 Reject）
  → 内核流转写库
  → Sync Hook（同事务；异常 FailFast 回滚）
  → Async Hook / Notifier 写入 wf_hook_outbox（同事务）
  → 返回 WfExecutedResult
后台 Drain
  → 认领 Pending → 调 Handler → Succeeded / 重试 / Dead
```

- **核心上下文**：
  - Gate / Hook：`FlowHookContext`
  - Notifier：`TaskNotifyContext`
  - 二者均无「万能 FlowEventContext」；业务金额等请自行从业务库按 `AppInstanceId` / 实例 Id 读取。

---

## 一、四组件职责与选型

| 组件 | 时机 | 能否阻断流转 | 典型用途 | 异常影响 |
|------|------|--------------|----------|----------|
| **Gate**（`IFlowGate`） | 落库推进前 | ✅ `HookResult.Reject` | 金额/权限/单据状态/合规卡控 | 映射到 `WfExecutedResult`，不当事故异常；无推进副作用 |
| **Hook**（`IFlowHook`） | 成功后 | ❌（Async）/ ⚠️ Sync 可回滚 | 回写业务表、汇总、结束出单 | **Sync**：异常或超时 → FailFast 回滚主事务；**AsyncOutbox**：失败进 Outbox 重试，不拖垮已提交流程 |
| **Notifier**（`ITaskNotifier`） | 待办生命周期旁路 | ❌ | 邮件 / IM / HTTP 推送 | 默认走 Outbox；通道失败可隔离（见 `TaskNotifyChannelBase`） |
| **Outbox**（`wf_hook_outbox`） | Async Hook / Notifier 入队 | ❌ | 可靠补调、重试、死信复活 | 写入与流转同库事务；消费由 Drain 独立执行 |

**重要区分**

- Gate = **流转前业务校验**；待办人解析是 **Assignee / Selector 管道**，二者不是一回事（见 [特性说明-待办人管道.md](../sail/特性说明-待办人管道.md)）。
- Notifier ≠ 抄送；抄送应独立通道，不要塞进待办推送。

---

## 二、启用事件能力（宿主初始化）

推荐用总包 `UseFlowAspects` 收拢，内层仍调用分轨 API（真实方法名如下，**没有** `opt.AddGate` 这种单一 Options 对象写法）。

```csharp
using saiflow.engine.Client;
using saiflow.engine.Event;
using saiflow.engine.Hosting;
using saiflow.engine.Hooks;
using saiflow.msg.Hosting;

var client = SailClient.CreateBuilder(configuration)
    .UseDatabase(/* ... */)
    .UseFlowAspects(b =>
    {
        // Gate：推进前校验（仅 Sync）
        b.UseFlowGate(new FinancePreSendGate(), o =>
        {
            o.ProcessCodes = new[] { "EXPENSE", "PAYMENT" };
            o.ActivityCodes = new[] { "finance_leader_approve" }; // 可选
            o.Order = 10;
        });
        // 无参构造糖：b.AddGate<MyGate>(o => { ... });

        // Hook：须声明 Events；默认 Mode = AsyncOutbox
        b.UseFlowHook(new FinanceAggregateHook(), o =>
        {
            o.ProcessCodes = new[] { "EXPENSE" };
            o.Events = new[] { EventFireTypeEnum.OnProcessContinued };
            o.Mode = HookMode.AsyncOutbox;
            o.Order = 20;
        });

        // 本地强一致回写：显式 Sync
        b.UseFlowHook("biz.writeback", ctx =>
        {
            // 按 ctx.AppInstanceId / ProcessInstanceId 回写业务表
        }, o =>
        {
            o.Match = HookMatch.All;
            o.Mode = HookMode.Sync;
            o.Events = new[]
            {
                EventFireTypeEnum.OnProcessContinued,
                EventFireTypeEnum.OnProcessRejected,
                EventFireTypeEnum.OnProcessSendBacked,
            };
        });

        // Notifier：须声明 TaskKinds；强制 AsyncOutbox（配 Sync 也会被改回）
        b.UseTaskNotifier(new SampleHttpTaskNotifier("https://example/callback"), o =>
        {
            o.Match = HookMatch.All;
            o.TaskKinds = new[]
            {
                TaskNotifyKind.Created,
                TaskNotifyKind.Completed,
                TaskNotifyKind.Entrusted,
            };
        });
        // 无参构造糖：b.AddNotifier<MyNotifier>(...);
    })
    // 默认 SMTP 待办邮件（显式开启；内部 UseTaskNotifier）
    .UseDefaultTaskMail()
    // Outbox 后台消费（opt-in；autoStart 才会 PostBuild 拉起循环）
    .UseHookOutboxDrain(o =>
    {
        o.MaxAttempts = 5;
        o.BatchSize = 50;
        o.PollInterval = TimeSpan.FromSeconds(5);   // 默认 5s
        o.LeaseSeconds = 120;
        o.ProcessingStaleSeconds = 300;
        o.SucceededRetentionDays = 7;
        o.DeadRetentionDays = 30;
    }, autoStart: true)
    .Build();

// Sync Hook 硬超时（毫秒；默认 2000；0 = 不限制）
client.CapabilityOptions.SyncHookTimeoutMs = 2000;

// 可选指标
client.CapabilityMetrics = new InMemoryFlowCapabilityMetrics();

// 进程退出时取消 Drain
// client.HookOutboxDrainCts?.Cancel();
```

**注册要点**

| 项 | 现状 |
|----|------|
| Gate / Hook / Notifier 顺序 | 按 `HookRegistrationOptions.Order` 排序执行 |
| 默认 `Match` | `Whitelist`——必须配 `ProcessCodes` / `ProcessIds`，否则永远不命中；全局用 `Match = HookMatch.All` |
| Hook 默认 `Mode` | `AsyncOutbox` |
| Hook 未配 `Events` | **不会执行**（空数组视为不匹配） |
| Notifier 未配 `TaskKinds` | **不会推送** |
| 同名后注册 | 覆盖先前注册，并 `TraceWarning` |
| Build 后补挂 | `client.useFlowGate` / `useFlowHook` / `useTaskNotifier`（小写）仍可用，优先在 Builder 声明 |

Biz 宿主别名：`UseBizHookOutboxDrain`、`UseBizTaskMail`（等价上述 Drain / 默认邮件）。

---

## 三、Gate 开发（前置拦截）

### 接口（真实）

```csharp
public interface IFlowGate
{
    string Name { get; }
    HookResult Validate(FlowHookContext ctx);   // 同步；非 Task
}
```

### `HookResult`

```csharp
HookResult.Ok();
HookResult.Reject(string message, string code = null, IEnumerable<HookValidationItem> details = null);
```

Reject 经 `WfExecutedResult.ApplyGateReject`：`Status = Failed`，带上 `Message` / `ValidationCode` / `ValidationDetails`。前端可用 `result.IsValidationReject()`（`Failed` 且 `ValidationCode != null`）区分业务拒绝与引擎事故。

### 示例：金额上限 Gate

上下文**没有**流程变量字典；从业务服务按单据 Id 查金额：

```csharp
public sealed class AmountLimitGate : IFlowGate
{
    private readonly IBizBillService _bills;

    public AmountLimitGate(IBizBillService bills) => _bills = bills;

    public string Name => "biz.amount-limit";

    public HookResult Validate(FlowHookContext ctx)
    {
        // 可选：只卡发起 / 继续
        if (ctx.Operation is not (FlowOperation.Start or FlowOperation.Run))
            return HookResult.Ok();

        var amount = _bills.GetAmount(ctx.AppInstanceId);
        if (amount > 100_000m)
        {
            return HookResult.Reject(
                "单笔金额不能超过10万，请走专项审批通道",
                code: "AMOUNT_LIMIT",
                details: new[]
                {
                    new HookValidationItem { Field = "Amount", Message = "超过上限 100000" }
                });
        }

        return HookResult.Ok();
    }
}
```

引擎内置文档样例：`saiflow.engine.Samples.FinancePreSendGate`。

**适合**：Start / Run / Reject / SendBack / Transfer 等推进前规则。  
**不要**：Gate 内打慢外部 HTTP / MQ（会阻塞审批；超时体验差）。  
**日志**：当前**没有**独立 `GateRecord` 表；Reject 次数可走 `IFlowCapabilityMetrics.IncrementGateReject`，明细靠业务自记或 API 返回。

委托注册：

```csharp
builder.UseFlowGate("finance.pre-send", ctx => HookResult.Ok(), o =>
{
    o.ProcessCodes = new[] { "EXPENSE" };
});
```

---

## 四、Hook 开发（流转后业务）

### 接口（真实）

```csharp
public interface IFlowHook
{
    string Name { get; }
    void Handle(FlowHookContext ctx);   // 同步；Async 时由 Outbox Worker 调同一方法
}
```

### Mode 语义（务必分清）

| Mode | 主路径行为 | 失败 |
|------|------------|------|
| `HookMode.Sync` | 当场 `Handle`；受 `SyncHookTimeoutMs` 约束 | 异常 / 超时 → **回滚**流程事务（FailFast） |
| `HookMode.AsyncOutbox`（默认） | 只插入 `wf_hook_outbox` | Worker 重试；超 `MaxAttempts` → `Dead`；**不**回滚已提交流转 |

用户若需要「流程已成功、业务副作用失败可补调」，必须用 **AsyncOutbox**，不要默认当成「Hook 永远不影响流程」。

### 示例：按操作更新单据

```csharp
public sealed class UpdateBillStatusHook : IFlowHook
{
    private readonly IBizBillService _bills;

    public UpdateBillStatusHook(IBizBillService bills) => _bills = bills;

    public string Name => "biz.update-bill-status";

    public void Handle(FlowHookContext ctx)
    {
        var bizId = ctx.AppInstanceId;
        switch (ctx.EventType)
        {
            case EventFireTypeEnum.OnProcessContinued:
                _bills.SetAuditPass(bizId);
                break;
            case EventFireTypeEnum.OnProcessRejected:
                _bills.SetAuditReject(bizId);
                break;
            case EventFireTypeEnum.OnProcessSendBacked:
                _bills.SetReturned(bizId);
                break;
        }
    }
}
```

注册时务必带上对应 `Events`：

```csharp
builder.UseFlowHook(new UpdateBillStatusHook(bills), o =>
{
    o.Match = HookMatch.All;
    o.Mode = HookMode.Sync; // 与流程同事务回写
    o.Events = new[]
    {
        EventFireTypeEnum.OnProcessContinued,
        EventFireTypeEnum.OnProcessRejected,
        EventFireTypeEnum.OnProcessSendBacked,
    };
});
```

引擎样例：`FinanceAggregateHook`（过滤 `OnProcessContinued`）。

**建议**

1. 本地 DB 强一致回写 → Sync Hook（短逻辑）。  
2. 跨服务 / 慢调用 → AsyncOutbox，或放到 Notifier。  
3. Async 消费路径按 `ProcessInstanceId` + `EventType`（或业务幂等键）做幂等。

---

## 五、Notifier（待办推送旁路）

### 接口（真实）

```csharp
public interface ITaskNotifier
{
    string Name { get; }
    Task NotifyAsync(TaskNotifyContext ctx, CancellationToken ct);
}
```

引擎在待办创建/完成等路径调用 `FireTaskLifecycle` → `EnqueueTaskNotifiers`（写入 Outbox）。注册时若写 `Mode = Sync`，注册表会**强制改回** `AsyncOutbox`。

### 推荐：继承 `TaskNotifyChannelBase`（`saiflow.msg`）

```csharp
public sealed class DingTalkTaskNotifier : TaskNotifyChannelBase
{
    public override string Name => "dingtalk.todo";

    // 需要 Outbox 感知失败并重试时改为 true（默认 false 会吞异常）
    protected override bool RethrowFailures => true;

    protected override async Task SendCoreAsync(TaskNotifyContext ctx, CancellationToken ct)
    {
        if (ctx.Kind != TaskNotifyKind.Created)
            return;

        await DingApi.SendTodoAsync(new
        {
            ReceiverId = ctx.AssignedUserId,
            Title = "待审批",
            Content = $"单据 {ctx.AppInstanceId} 等待您审批",
            InstanceId = ctx.ProcessInstanceId,
            TaskId = ctx.TaskId,
        }, ct);
    }
}
```

```csharp
builder.UseTaskNotifier(new DingTalkTaskNotifier(), o =>
{
    o.Match = HookMatch.All;
    o.TaskKinds = new[] { TaskNotifyKind.Created, TaskNotifyKind.Completed, TaskNotifyKind.Entrusted };
});
```

现成能力：

| API | 说明 |
|-----|------|
| `UseDefaultTaskMail()` / `UseBizTaskMail()` | 注册 `DefaultMailTaskNotifier`（`Name = default.mail`），`TaskKinds = Created, Completed`，`Match = All` |
| `SampleHttpTaskNotifier` | POST JSON 到回调 URL 的样例 |

**说明**：引擎侧**没有**通用 `IOutboxPublisher.PublishAsync(topic, payload)`。Outbox 专用于已注册 Hook/Notifier 的上下文序列化投递；跨服务自定义主题请在业务侧另建消息表，或在 Notifier/Hook 内发 MQ（仍建议 Async）。

---

## 六、Outbox（事务可靠补调）

### 原理

1. Async Hook / Notifier 命中后，向 `wf_hook_outbox` 插入 `Pending`（与流转 **同一 SQLBuilder 事务**）。  
2. `UseHookOutboxDrain(..., autoStart: true)` 或手动 `HookOutboxWorker` / `HookOutboxDrainLoop` 轮询。  
3. CAS 认领 → `Processing`（带 `leased_until`）→ 调 Handler。  
4. 成功 → `Succeeded`；失败 → `Attempt++`，指数退避 `NextRetryAt`，再回 `Pending`；`Attempt >= MaxAttempts` → `Dead`。  
5. 语义：**至少一次**投递（at-least-once），消费端必须幂等。

### 表：`wf_hook_outbox`（`HookOutboxEntity`）

| 列 / 属性 | 含义 |
|-----------|------|
| `Id` | 主键 |
| `Kind` | `Hook` / `Notify`（`HookOutboxKinds`） |
| `HandlerName` | 注册的 `Name` |
| `EventType` | 事件或 `TaskNotifyKind` 字符串 |
| `ProcessId` / `ProcessCode` / `ProcessInstanceId` / `ActivityInstanceId` / `TaskId` | 关联上下文 |
| `PayloadJson` | 序列化的 `FlowHookContext` 或 `TaskNotifyContext` |
| `Status` | `Pending` / `Processing` / `Succeeded` / `Failed` / `Dead` |
| `Attempt` / `NextRetryAt` / `LastError` | 重试 |
| `CreatedAt` / `UpdatedAt` / `LeasedUntil` | 时间与租约 |

死信**不是**独立 `DeadLetter` 表，而是同行 `Status = Dead`。

### 运维 API

```csharp
var admin = new HookOutboxAdmin(client);
using var kit = client.UseRunSQL();

var dead = admin.QueryDead(kit, limit: 50, processCode: "EXPENSE", handlerName: "dingtalk.todo");
admin.ResetDeadBatch(kit, dead.Select(d => d.Id.Value)); // Dead → Pending
// 或 admin.ResetAllDead(kit, limit: 500);

admin.CountByStatus(kit, HookOutboxStatus.Pending);
admin.PurgeExpired(kit, client.HookOutboxOptions); // TTL 清理 Succeeded/Dead
```

手动 drain（无后台循环时）：

```csharp
new HookOutboxWorker(client).ProcessPending();
// 或
client.CapabilityDispatcher.DispatchPendingOutbox(kit, limit: 50, maxAttempts: 5);
```

### `HookOutboxOptions` 常用字段

| 属性 | 默认 | 说明 |
|------|------|------|
| `MaxAttempts` | 5 | 超过进 Dead |
| `BatchSize` | 50 | 单轮认领上限 |
| `PollInterval` | 5s | Drain 间隔 |
| `LeaseSeconds` | 120 | 认领租约 |
| `ProcessingStaleSeconds` | 300 | 无有效 lease 的 Processing 回收 |
| `SucceededRetentionDays` / `DeadRetentionDays` | 7 / 30 | TTL；≤0 不清理 |

---

## 七、同步 vs 异步（生产建议）

| 模式 | 配置 | 行为 |
|------|------|------|
| 全同步偏演示 | Hook `Mode=Sync`；不启 Drain | Gate + Sync Hook 在请求线程；Notifier 仍入 Outbox，**不启 Drain 则消息堆积不发** |
| 生产推荐 | 短 Sync 回写 + 重活 AsyncOutbox + `UseHookOutboxDrain(autoStart: true)` | 审批接口稳定；推送/外部调用可重试、可死信复活 |

Gate **始终 Sync**，忽略 `Mode`。

---

## 八、触发时机清单

### Gate：`FlowOperation`（`ctx.Operation`）

引擎在以下命令路径会 `TryGate`（落库前）：

`Start` · `Run` · `Withdraw` · `SendBack` · `Resend` · `Revise` · `Reverse` · `Reject` · `Close` · `Jump` · `SignForward` · `Transfer`

Gate 构建上下文时 `EventType = OnValidating`。

### Hook：须订阅的成功事件（`Events`）

| 操作 | 成功事件（节选） |
|------|------------------|
| 发起 | `OnProcessStarted` |
| 继续/同意 | `OnProcessContinued` |
| 撤回 | `OnProcessWithdrawn` |
| 退回 | `OnProcessSendBacked` |
| 再发送 | `OnProcessResent` |
| 修订 | `OnProcessRevised` |
| 返签等 | `OnProcessReversed` |
| 拒绝 | `OnProcessRejected` |
| 关闭 | `OnProcessClosed` |
| 跳转 | `OnProcessJumped` |
| 加签 | `OnProcessSignForwarded` |
| 转办 | `OnProcessTransferred` |
| 待办生命周期 | `OnTaskCreated` / `OnTaskCompleted`（`FireTaskLifecycle`） |

在 `Handle` 内用 `ctx.EventType` / `ctx.Operation` 做分支过滤。

### Notifier：`TaskNotifyKind`

`Created` · `Completed` · `Cancelled` · `Entrusted`

---

## 九、上下文字段速查

### `FlowHookContext`

`AppName` · `AppInstanceId` · `ProcessId` · `ProcessCode` · `Version` · `ProcessInstanceId` · `ActivityId` · `ActivityCode` · `ActivityInstanceId` · `TaskId` · `AssignedUserId` · `AssignedUserName` · `Operation` · `EventType`

### `TaskNotifyContext`

同上业务键 + `TaskId`（非空）+ `Kind`（`TaskNotifyKind`）。

**现状没有**：TenantId、流程变量字典、流转前后快照、统一 TraceId、独立 GateRecord/HookRecord。链路排查以实例 Id + Outbox `Id`/`HandlerName`/`LastError` + metrics 为主。

---

## 十、按流程 / 节点过滤（已内置）

全部写在 `HookRegistrationOptions`：

```csharp
o.Match = HookMatch.Whitelist;           // 默认
o.ProcessCodes = new[] { "EXPENSE" };    // 或 ProcessIds
o.ActivityCodes = new[] { "N2" };      // 空 = 不限节点
o.Events = new[] { EventFireTypeEnum.OnProcessContinued }; // Hook 必填才命中
o.TaskKinds = new[] { TaskNotifyKind.Created };            // Notifier 必填才命中
o.Order = 10;
o.Mode = HookMode.Sync;                  // 仅 Hook 有意义
```

- `Match = All`：忽略流程白名单。  
- `Whitelist` 且未配任何 ProcessCode/Id：**永不命中**（常见踩坑）。

---

## 十一、排查与观测

| 手段 | 说明 |
|------|------|
| `WfExecutedResult.IsValidationReject()` | Gate 业务拒绝 |
| `wf_hook_outbox` | Async 队列、重试、死信 |
| `HookOutboxAdmin` | 查询 Dead、批量复活、按状态计数、TTL 清理 |
| `IFlowCapabilityMetrics` | GateReject / SyncHook / OutboxEnqueue / Succeeded / Failed / Dead / NotifierDispatch 等 |
| `FlowSyncHookTimeoutException` | Sync Hook 超时 |

尚无独立 Gate/Hook 执行日志表；需要审计时在业务 Handler 内自写，或消费 metrics。

---

## 十二、常见踩坑

1. **Gate 打远程接口** → 第三方超时直接卡审批；推送一律 Notifier + Outbox。  
2. **以为 Hook 失败永不影响流程** → 仅 AsyncOutbox 如此；Sync 异常/超时会回滚。  
3. **Whitelist 未配 ProcessCodes** 或 Hook 未配 `Events` / Notifier 未配 `TaskKinds` → 「注册了从不执行」。  
4. **开启 Notifier 却未 `UseHookOutboxDrain`** → Outbox 只堆积不消费。  
5. **`TaskNotifyChannelBase` 默认不抛** → Outbox 会当成成功；要重试请 `RethrowFailures = true`。  
6. **消费端无幂等** → at-least-once 会重复推送/重复写业务。  
7. **把选人写进 Gate** → 选人走 Assignee 管道。  
8. **强依赖多个 Hook 的相对顺序** → 可用 `Order`，但仍建议业务幂等、避免隐式顺序耦合。  
9. **Drain 间隔过小 + 大批量** → 默认 5s/50 条可按库压调大 `PollInterval`、控制 `BatchSize`。  
10. **在 Notifier 写复杂业务回写** → 回写放 Hook；Notifier 只组装并发送。

---

## 十三、与旧 Subscribe / 单一回调模型的差异

| | `WorkflowService.Subscribe` | Gate / Hook / Notifier |
|--|----------------------------|-------------------------|
| 生命周期 | 单次 `WfAppRunner` | `SailClient` 级，启动注册 |
| 发送前拒绝 | 弱（易与异常混淆） | Gate + `HookResult` → 结构化 `WfExecutedResult` |
| 过滤 | 无 | Process / Activity / Events / TaskKinds |
| 可靠补调 | 无 | `wf_hook_outbox` + Drain + 死信复活 |
| 去留 | **保留兼容** | 新业务优先本体系 |

相对「单一批回调、前后混用、无内置重试」的旧式模型：SailFlow 将拦截、业务、通知、可靠投递拆开，并落库补调，更适合高并发 OA。

---

## 附录 A · 关键真实 API 索引

| 用途 | API |
|------|-----|
| 总包 | `UseFlowAspects` |
| Gate | `UseFlowGate` / `AddGate<T>` / `useFlowGate` |
| Hook | `UseFlowHook` / `AddHook<T>` / `useFlowHook` |
| Notifier | `UseTaskNotifier` / `AddNotifier<T>` / `useTaskNotifier` |
| 默认邮件 | `UseDefaultTaskMail` / `UseBizTaskMail` |
| Outbox Drain | `UseHookOutboxDrain` / `UseBizHookOutboxDrain` |
| 结果 | `HookResult` · `WfExecutedResult.ApplyGateReject` · `IsValidationReject` |
| 运维 | `HookOutboxAdmin` · `HookOutboxManager` · `HookOutboxWorker` · `HookOutboxDrainLoop` |
| 选项 | `HookRegistrationOptions` · `HookOutboxOptions` · `FlowCapabilityOptions` |

## 附录 B · 可续写专题

若需要可再补文档或示例工程：

- **A.** 最小可运行控制台：DI 注册 → Start/Run → Gate Reject → Sync Hook 回写 → 邮件/HTTP Notifier + Drain  
- **B.** `wf_hook_outbox` 建表 / 索引 SQL（亦可由 Schema 初始化创建）  
- **C.** 多租户或按业务线拆分多个 Notifier 的宿主拼装样例  

---

*文档与源码不一致时以 `src/core/pure/engine/src/Hooks` 与 `WorkflowService.ProcessExecution` 为准。*
