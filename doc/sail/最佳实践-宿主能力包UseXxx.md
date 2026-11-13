# 最佳实践：宿主能力包（Builder · UseXxx）

> 状态：现行约定（2026-11-13）  
> 适用：在 **宿主** 或 **biz** 中挂 Gate / Hook / Notifier / Assignee / Outbox Drain 等可插拔能力  
> 关联：[业务管道特性设计.md](./业务管道特性设计.md) · [特性设计-回调与事件.md](./特性设计-回调与事件.md) · [架构设计-项目依赖与分层.md](./架构设计-项目依赖与分层.md) · [目录-扩展插件.md](./目录-扩展插件.md)  
> 宿主落点示例：`sleveen-admin/doc/sailflow-host-capability-pack.md`  
> **用法案例与骨干实现见 §9**

---

## 1. 一句话

**一项可开关的能力 = 一个 `UseXxx` 扩展方法**；在 `SailClientBuilder` 链式调用里声明；**启用判定、建表、注册 Hook/Notifier、启 Drain 都收进扩展内部**，宿主 `Program` / HostSetup **不要**再散落 `if (enabled) { client.use… }`。

---

## 2. 为什么

| 反模式 | 问题 |
|--------|------|
| `Build()` 后大段 `if` + `addHook` / `addNotifier` | 组合根膨胀；开关、事件列表、建表散落，难复用、难测 |
| 把业务 Hook 写进引擎 pure | 违反分层；宿主主数据/HTTP 无法干净依赖 |
| 只挂 `client.useXxx`、从不走 Builder | 与引擎 pending Apply / PostBuild 生命周期脱节；同类能力风格分裂 |

正模式：与 `UseDatabase` / `UseBizAI` / `AddHook` 同一管道语言——**Builder 声明 → Build 时按 Phase 落地**。

---

## 3. 双轨 API（引擎已提供）

| 时机 | 命名风格 | 机制 |
|------|----------|------|
| **构建中（推荐默认）** | `builder.UseXxx`（能力包）/ `AddXxx`（可多次追加） | 写入 `ISailCollection` pending 或 `SailFeatureDescriptor`；`Build()` 时 Apply |
| **构建后（补挂）** | `client.addXxx` / `client.useXxx` | 已有 `SailClient` 上即时注册（追加项用 `add*`） |

命名约定：`UseXxx` = 启用一项能力包；`AddXxx` = 可多次追加的注册项（Gate/Hook/Notifier、ActionMethod、RuleEngine 等）。

Gate / Hook / Notifier 引擎侧：

- Builder：`AddGate` / `AddHook` / `AddNotifier`
- Client：`addGate` / `addHook` / `addNotifier`
- pending 在 `Build()` 中 `ApplyPendingRegistrations`（早于或穿插 ConfigureClient；以代码为准）

宿主能力包应 **优先实现 Builder 扩展**；可选保留同文件 `client.addXxx` 薄封装，便于测试或热补挂。

---

## 4. Build 生命周期（注册落点）

```text
ConfigureServices   → UseDatabase / UseSchema 配置 / UseHookOutboxDrain 选项等
Assemble SailClient
ApplyPending        → AddHook / AddNotifier 等 pending 注册
ConfigureClient     → Locator、Drain 绑到 client 等
PostBuild           → EnsureTables、Seed、依赖 client 的 Notifier 实例化 + addNotifier
```

| 能力形态 | 推荐 Phase |
|----------|------------|
| 无运行时依赖的 Hook（如调试 Console） | `AddHook` pending（ConfigureServices 之前挂到 Builder 即可） |
| 需要 `SailClient` 才能 `new Service(client)` / EnsureTable | **PostBuild** 里 `EnsureTable` + `client.addHook` / `addNotifier` |
| Outbox Drain | `UseHookOutboxDrain(autoStart: …)`（内部 ConfigureClient + 可选 PostBuild 启动） |
| 依赖宿主 MS DI（`IOptions` / `HttpClient` / `ILogger`） | Builder 扩展参数接收 `IServiceProvider`（或具体依赖），闭包捕获到 PostBuild |

---

## 5. 能力包扩展模板

```csharp
public static class MyCapabilityExtensions
{
    const string FeatureName = "host.my.capability";

    /// <summary>构建期入口：自行读开关；未启用则 no-op。</summary>
    public static SailClientBuilder UseMyCapability(
        this SailClientBuilder builder,
        IServiceProvider? sp = null) // 无 DI 依赖时可省略
    {
        ArgumentNullException.ThrowIfNull(builder);

        // 1) 自行判定（配置 / Options / 编译符号）
        // if (!enabled) return builder;

        // 2a) 无 client 依赖：直接 pending
        // builder.AddHook(new MyHook(), o => { … });

        // 2b) 需要 client / EnsureTable / DI：
        builder.Services.AddFeature(new SailFeatureDescriptor
        {
            Name = FeatureName,
            Phase = SailFeaturePhase.PostBuild,
            Order = 2xx,
            // 若依赖 Outbox / Json 等，声明 DependsOn，Build 缺依赖即失败
            // DependsOn = new[] { SailFeatureNames.Json },
            PostBuild = (client, _) => Register(client, sp)
        });

        // 3) 旁路能力一并内聚（如 Drain）；Drain 本身 DependsOn json，须先 UseBizJson
        // builder.UseHookOutboxDrain(autoStart: true);

        return builder;
    }

    static void Register(SailClient client, IServiceProvider? sp) { /* EnsureTable + useXxx */ }
}
```

**规则：**

1. **开关在扩展内**：宿主只写 `.UseMyCapability(sp)`，不写外层 `if`。  
2. **一事一包**：调试 Hook、请假 Status、门户推送分三个 `Use*`，组合根只剩链式清单。  
3. **不在扩展里 `Build()`**。  
4. **幂等**：同名 Feature / Hook `Name` 重复注册策略与引擎一致（后覆盖 + 可观测）。  
5. **三线分离**：Assignee ≠ Hook ≠ Task Notifier；抄送另通道（见待办人管道 R5）。  
6. **依赖显式**：硬前置能力写 `DependsOn`（常量见 `SailFeatureNames`）；Property 插槽用 `ProvideFeature`；启动后用 `ProbeInstalledFeatures` / `client.HasFeature` 验收。

---

## 6. 宿主组合根应长什么样

```csharp
var client = CreateBuilder(configuration)
    .UseBizJson()
    .UseBizRules()
    .UseBizAssignees()
    .UseBizRuntimeDefaults()            // LINQ 条件 Evaluator 等 pure 默认扩展
    .UseNextStepRoute(r =>              // 下一跳写袋 / 求值 / 候选 Filter（可选）
    {
        // r.IfCondition(...); r.AddFilter<T>(...);
    })
    .UseDebugConsoleHook()              // 可选能力包
    .UseLeaveStatusHook()               // 样例包
    .UsePortalScheduleTaskNotifier(sp)  // 自带 Enabled 判定
    .ConfigureServices(s =>
    {
        s.UseDatabase(…);
        s.UseSchema(…);
        // 不要在此再 if(portal) UseHookOutboxDrain — 已收进能力包
    })
    .Build();

// Build 后仅保留：必须依赖「已构建 client」且不宜进包的极少项
// 例如宿主 Assignee Lookup 覆盖（若尚未 Builder 化）
client.UseSleveenOrgLookups();
```

反例：在 `Build()` 后复制事件数组、手动 `new Notifier(…)`、外层 `if (portalEnabled)`。

---

## 7. 与 Gate / Hook / Notifier 语义对齐

| 轨 | 默认同异步 | 能力包注意 |
|----|------------|------------|
| Gate | 仅 Sync | 短路径；Reject 不当事故异常 |
| Hook | Sync 或 AsyncOutbox | 调试/状态回写常用 Sync；重活出 Outbox |
| Notifier | 默认 AsyncOutbox | 启用时常 **连带** `UseHookOutboxDrain`；失败隔离 |

详见 [特性设计-回调与事件.md](./特性设计-回调与事件.md)。

---

## 8. 验收清单（Code Review）

- [ ] 新能力有独立 `UseXxx`，宿主组合根无大段注册逻辑  
- [ ] 配置开关在扩展内判定；关闭时无副作用（不建表、不启 Drain、不注册 Handler）  
- [ ] 需要 `SailClient` 的注册落在 PostBuild（或 ConfigureClient），而非臆造 client  
- [ ] 未把宿主 HTTP / SysUser 逻辑塞进 `sailflow` pure  
- [ ] Hook `Name` / Feature `Name` 稳定可观测  

---

## 9. UseXxx / AddXxx 全面用法案例（含被注册类骨干）

> 索引对照：[目录-扩展插件.md](./目录-扩展插件.md)。  
> 本节代码为**骨干骨架**（可编译心智），正式路径以仓库实现为准；样例类型默认 NS：`sailflow.external.*` / 宿主 `SleveenAdmin.Sailflow.*`。

### 9.0 速查：谁用 `Use`、谁用 `Add`

| 形态 | 命名 | 典型 API | 被注册契约 |
|------|------|----------|------------|
| 能力包（一事一开） | `UseXxx` | `UseBizJson` · `UseAssign` · `UseNextStepRoute` · `UseHookOutboxDrain` · `UsePortalScheduleTaskNotifier` | Feature / 管道总装 |
| 可多次追加 | `AddXxx` | `AddGate` · `AddHook` · `AddNotifier` · `AddResolver` · `AddFilter` · `AddActionMethod` | 具体插件实例 |
| 构建后补挂 | `client.add*` / `use*` | `addHook` · `useAssigneeResolver` · `UseSleveenOrgLookups` | 同契约，即时生效 |
| 宿主 MS DI | `AddSail*` | `AddSailFormBiz` · `AddSailOperationHandlers` · `AddSailNodePolicyJobs` | Scoped Handler / HostedService |

---

### 9.1 Gate / Hook / Notifier（`UseFlowAspects` + `Add*`）

#### 挂载

```csharp
builder
    .UseBizJson()                          // AsyncOutbox / Drain 依赖 JSON
    .UseFlowAspects(b =>
    {
        b.AddGate<FinancePreSendGate>(o =>
        {
            o.ProcessCodes = new[] { "EXPENSE" };   // 默认 Whitelist，必配
            // Gate 恒 Sync；不必填 Events（固定 OnValidating）
        });

        b.AddHook(new FinanceAggregateHook(), o =>
        {
            o.ProcessCodes = new[] { "EXPENSE" };
            o.Mode = HookMode.AsyncOutbox;          // 默认即此；短路径用 Sync
            o.Events = new[] { EventFireTypeEnum.OnProcessContinued };
        });

        b.AddNotifier(new SampleHttpTaskNotifier("https://example/callback"), o =>
        {
            o.Match = HookMatch.All;
            o.TaskKinds = new[]
            {
                TaskNotifyKind.Created,
                TaskNotifyKind.Completed
            };
        });
    })
    .UseHookOutboxDrain(autoStart: true);  // Notifier / Async Hook 需要 Drain
```

无参泛型糖：`AddGate<T>` / `AddHook<T>` / `AddNotifier<T>`（`T : new()`）。有构造依赖时用实例重载。

构建后补挂：

```csharp
client.addGate(new FinancePreSendGate(), o => o.ProcessCodes = new[] { "EXPENSE" });
client.addHook(hook, o => { o.Match = HookMatch.All; o.Events = …; o.Mode = HookMode.Sync; });
client.addNotifier(notifier, o => { o.Match = HookMatch.All; o.TaskKinds = …; });
```

#### 被注册类骨干

```csharp
// IFlowGate — 推进落库前同步校验；Reject ≠ 事故异常
public sealed class FinancePreSendGate : IFlowGate
{
    public string Name => "finance.pre-send";

    public HookResult Validate(FlowHookContext ctx)
    {
        // 读 ctx.Operation / ActivityCode / 变量；禁止写流程变量、打慢外部网关
        // return HookResult.Reject("AMOUNT_TOO_HIGH", "超额需特批");
        return HookResult.Ok();
    }
}

// IFlowHook — 推进后副作用；Sync=同事务 FailFast；AsyncOutbox=入 wf_hook_outbox
public sealed class FinanceAggregateHook : IFlowHook
{
    public string Name => "finance.aggregate";

    public void Handle(FlowHookContext ctx)
    {
        if (ctx.EventType != EventFireTypeEnum.OnProcessContinued)
            return;
        // 写业务汇总 / 调外部；Async 时由 Worker 调同一方法
    }
}

// ITaskNotifier — 待办旁路；强制 AsyncOutbox
// 推荐继承 TaskNotifyChannelBase / ReliableTaskNotifierBase
public sealed class SampleHttpTaskNotifier : TaskNotifyChannelBase
{
    readonly Uri _uri;
    public SampleHttpTaskNotifier(string callbackUrl) =>
        _uri = new Uri(callbackUrl, UriKind.Absolute);

    public override string Name => "sample.http";

    protected override async Task SendCoreAsync(TaskNotifyContext ctx, CancellationToken ct)
    {
        // POST JSON；默认吞异常（通道隔离）
        // Reliable 档：继承 ReliableTaskNotifierBase（RethrowFailures=true）
    }
}
```

**BestEffort Hook**（可丢、Channel 串行；注册须 `Mode = Sync`）：

```csharp
public sealed class FlowEventFileLogHook : BestEffortFlowHookBase<FileLogPayload>
{
    public override string Name => "host.flow.event.filelog";

    protected override bool TryCreatePayload(FlowHookContext ctx, out FileLogPayload payload)
    {
        payload = new FileLogPayload { /* 映射关键字段 */ };
        return true;
    }

    protected override Task ConsumeAsync(FileLogPayload payload, CancellationToken ct)
    {
        // 写文件 / 打点；异常由泵吞掉
        return Task.CompletedTask;
    }
}

// 挂载：builder.AddHook(new FlowEventFileLogHook(), BestEffortHookRegistration.Configure);
// 或宿主能力包：builder.UseFlowEventFileLog(path);
```

---

### 9.2 待办人管道（`UseAssignees` / `UseAssign` → `AddResolver` / `AddFilter` / `AddEmptyAction`）

#### 挂载

```csharp
builder
    .UseBizAssignees()                     // 内置 Resolve/Leave + biz 样例
    // 或细粒度：
    .UseAssign(a =>
    {
        a.AddResolver<MyDeptBossResolver>();                 // 同 Type 后写覆盖
        a.AddResolver("FixedExtra", ctx => { /* 委托 */ return new PerformerList(); });
        a.AddLeaveJudge<MyCaptainLeaveJudge>();
        a.AddFilter<DedupUsersFilter>(o => o.Order = 0);     // 注册序=执行序
        a.AddEmptyAction<MySkipEmptyAction>();
        a.UseEmptyActionSelector(ctx => ctx.PolicyBag?["emptyKey"] as string);
        a.UsePolicyBag(ctx => /* 读节点策略袋 */ null);
        a.UseAnchorStore(new HookOutboxAutoApproveAnchorStore());
        // a.UseAutoApproveJob(o => o.AutoStart = true);    // 与宿主 JobWorker 勿双开
    });

// Build 后宿主覆盖 Lookup（组织主数据）
client.UseSleveenOrgLookups(configuration);
client.UseSleveenNodePolicyAssignee(configuration);
```

单点补挂：

```csharp
client.useAssigneeResolver(new MyDeptBossResolver());
client.useAssigneeFilter(new SameAsStarterFilter(client), order: 10);
client.useAssigneeEmptyAction(new MySkipEmptyAction());
client.useLeaveJudge(new MyCaptainLeaveJudge());
client.useAssigneeBusinessCallback("ClassStudents", ctx => { /* PerformerList */ });
```

#### 被注册类骨干

```csharp
// 时刻 A：按 ConfigPair.Type 算人（内置实现禁止 sealed，便于宿主继承覆盖）
public class MyDeptBossResolver : IAssigneeResolver
{
    public virtual string Type => "deptBoss";   // 与 BPMN ConfigPair.Type 对齐

    public virtual PerformerList Resolve(AssigneeResolveContext ctx)
    {
        var list = new PerformerList();
        // 读 ctx.Request / ctx.Config.Params；查组织主数据
        // list.Add(new Performer { UserId = "…" });
        return list;
    }
}

// 时刻 A′：有人后置（改人 / 打锚点）；禁止内嵌套 RunProcess
public sealed class SameAsStarterFilter : IAssigneeResolveFilter
{
    readonly SailClient _client;
    public SameAsStarterFilter(SailClient client) => _client = client;

    public void Apply(AssigneeFilterContext ctx)
    {
        // 读策略：同人=3 → 调 Registry 再 Resolve（如 deptBoss）
        // Skip 只打锚点、不落占位人
    }
}

// 空集处置：按 Key 选型（reject / skip / admin / users …）
public sealed class MySkipEmptyAction : IAssigneeEmptyAction
{
    public string Key => "skip";

    public EmptyActionResult Handle(AssigneeFilterContext ctx)
    {
        // 打 AutoApprove 锚点后返回空人通过，或：
        // return EmptyActionResult.Reject();
        return new EmptyActionResult { Performers = new PerformerList() };
    }
}

// 时刻 B：能否流出
public class MyCaptainLeaveJudge : IActivityLeaveJudge
{
    public virtual string Type => "Captain";

    public virtual LeaveJudgeResult CanLeave(LeaveJudgeContext ctx)
    {
        // return LeaveJudgeResult.Passed(…);
        // return LeaveJudgeResult.Waiting(…);
        // return LeaveJudgeResult.NotPassed(…);
        return LeaveJudgeResult.Waiting("Pending", "waiting");
    }
}
```

---

### 9.3 下一跳管道（`UseNextStepRoute` → `IfCondition` / `AddConditionEvaluator` / `AddFilter`）

#### 挂载

```csharp
builder
    .UseBizRuntimeDefaults()               // 默认挂 LINQ Evaluator
    .UseNextStepRoute(r =>
    {
        // Prepare：写 Conditions 袋
        r.IfCondition<SampleAmountPrepare>(o =>
        {
            o.ProcessCodes = new[] { "EXPENSE" };
        });
        r.IfCondition("amount", "0");      // 字面量糖；未配流程维时默认 Match=All
        r.IfCondition(c => c
            .ForProcess("EXPENSE")
            .ForActivity("Submit")
            .IfCondition("dept", "FIN"));

        // Evaluator：连线条件语言（可多次；Python 不随 UseIronPython）
        // r.AddConditionEvaluator<PythonConditionEvaluator>();

        // Filter：演算后裁候选（Disabled + ReasonCode）
        r.AddFilter<SamplePermissionHideBranchFilter>(o =>
        {
            o.ProcessCodes = new[] { "EXPENSE" };
        });
    });
```

#### 被注册类骨干

```csharp
// Prepare：推荐继承 NextStepPrepare
public sealed class SampleAmountPrepare : NextStepPrepare
{
    public SampleAmountPrepare() => IfCondition("amount", "0"); // 静态兜底

    protected override void Contribute(NextStepRouteContext ctx)
    {
        // 读单据覆盖 ctx.Conditions["amount"]
        if (ctx?.Conditions == null) return;
        if (!ctx.Conditions.ContainsKey("amount"))
            ctx.Conditions["amount"] = "0";
    }
}

// Filter：隐藏/禁用候选支；AndSplit 必达支会被管道 Guard 恢复
public sealed class SamplePermissionHideBranchFilter : INextStepFilter
{
    public string HideActivityCode { get; set; } = "finance_approve";
    public string ReasonCode { get; set; } = "NoPermission";

    public bool IsMatch(NextStepRouteContext ctx) => true;

    public void Filter(NextStepRouteContext ctx, NextStepCandidateList candidates)
    {
        foreach (var c in candidates.Items)
        {
            if (!string.Equals(c.Activity?.ActivityCode, HideActivityCode,
                    StringComparison.OrdinalIgnoreCase))
                continue;
            c.Disabled = true;
            c.ReasonCode ??= ReasonCode;
        }
    }
}
```

---

### 9.4 基础设施插槽（`ConfigureServices` 内 `Use*`）

```csharp
builder.ConfigureServices(s =>
{
    s.UseDatabase(dbFactory);                    // 或连接串工厂重载
    s.UseConfig(cfg => { /* SailConfig */ });
    s.UseConfigFromAppSettings();                // biz：从 IConfiguration 灌入
    s.UseCache(cache);
    s.UseJsonSerializer(newtonsoft);             // 或 builder.UseBizJson()
    s.UseMessageQueue(mq);                       // 或 builder.UseBizMsg(o => …)
    s.UseSchema(ensure => { /* 建表清单 */ });
    s.UseResource(resourceService);
    s.UseForm(formService);
    s.UseLocalize();                             // 或 UseBizLocalize()
    s.UseDataLifecycle(o => { /* 归档选项 */ }); // 或宿主 UseDataLifecycleFromConfig
});
```

| API | 作用 | 被注册/落地 |
|-----|------|-------------|
| `UseDatabase` | ORM / SQL 工厂 | `SailDBFactory` |
| `UseJsonSerializer` / `UseBizJson` | JSON 插槽 | `IJsonSerializer`；Drain `DependsOn` |
| `UseCache` | 缓存 | `ISooCache` |
| `UseMessageQueue` / `UseRabbitMq` / `UseBizMsg` | MQ | `IMessageQueueService` |
| `UseSchema` | 启动建表 | Schema 回调 |
| `UseForm` / `UseResource` / `UseLocalize` | 表单/资源/多语言 | 对应 `I*Service` |
| `UseDataLifecycle` | 归档/清理选项 | `SailDataLifecycleOptions` |
| `UseBizAI` | AI 节点执行器 + 可选建表 | `DefaultAiNodeExecutor` |
| `UseIronPython` / `UseBizAction` | Python Action/Script | Handler 注册 |
| `UseBizRuleEngines` / `UseBizRules` | NRules | `IRuleEngine` |
| `UseSailData` / `UseBizData` | 数据扩展包 | 日志 Sink 等 |
| `UseImmediateProcessArchiveHook` | 即时归档 Hook | `ImmediateProcessArchiveHook` |
| `UseInstanceLockCleaner` | 实例锁清理 | 后台清理循环 |
| `UseDefaultTaskMail` / `UseBizTaskMail` | 默认邮件 Notifier | `DefaultMailTaskNotifier` |

---

### 9.5 Action / Script / Service / RuleEngine（`Add*` 多次追加）

```csharp
builder.ConfigureServices(s =>
{
    s.AddActionMethod(new WebApiActionHandler());
    s.AddScriptMethod(new SqlScriptHandler());
    s.AddServiceMethod(new LocalServiceServiceHandler());
    s.AddRuleEngine(myNRulesEngine);
});

// 语言包总开（内含上述 Handler 注册）
builder.UseBizAction();   // → UseIronPython
builder.UseBizRules();    // → UseBizRuleEngines
```

骨干（自定义 Handler 时）：

```csharp
public sealed class MyActionHandler : IActionMethodHandler
{
    public string Method => "MyBiz.DoSomething";  // 与图内 Action Method 对齐
    public void Execute(/* 引擎传入参数 */) { /* … */ }
}
```

---

### 9.6 Outbox Drain / 数据生命周期 / 工厂替换

```csharp
builder
    .UseBizJson()
    .UseHookOutboxDrain(o =>
    {
        o.BatchSize = 50;
        o.MaxAttempts = 8;
    }, autoStart: true)                 // DependsOn Json；PostBuild 启循环
    .UseDataLifecycle(o => { /* … */ })
    .UseRuntimeFactory(() => new MyRuntime())   // 可选：替换引擎工厂
    .UseMediatorFactory(() => …)
    .UseProcessModelFactory(() => …)
    .UseManagerFactory(() => …);
```

biz 门面：`UseBizHookOutboxDrain`；宿主配置绑定：`UseDataLifecycleFromConfig(configuration)`。

---

### 9.7 宿主能力包模板（开关内聚 + PostBuild）

对齐 §5 模板；完整对照：`PortalScheduleTaskNotifier` / `UseFormBizLifecycleHook`。

```csharp
public static class UsePortalScheduleTaskNotifierExtensions
{
    const string FeatureName = "host.portal.schedule.notifier";

    public static SailClientBuilder UsePortalScheduleTaskNotifier(
        this SailClientBuilder builder, IServiceProvider sp)
    {
        var opts = sp.GetRequiredService<IOptions<PortalOptions>>().Value;
        if (!opts.Enabled) return builder;          // 开关在扩展内

        builder.UseHookOutboxDrain(autoStart: true);

        builder.Services.AddFeature(new SailFeatureDescriptor
        {
            Name = FeatureName,
            Phase = SailFeaturePhase.PostBuild,
            Order = 200,
            PostBuild = (client, _) =>
            {
                // EnsureTable…
                client.addNotifier(new PortalScheduleTaskNotifier(client, sp), o =>
                {
                    o.Match = HookMatch.All;
                    o.Mode = HookMode.AsyncOutbox;
                    o.TaskKinds = new[]
                    {
                        TaskNotifyKind.Created,
                        TaskNotifyKind.Completed,
                        TaskNotifyKind.Cancelled
                    };
                });
            }
        });
        return builder;
    }
}

// Reliable Notifier 骨干
public sealed class PortalScheduleTaskNotifier : ReliableTaskNotifierBase
{
    public override string Name => "portal.schedule";

    protected override async Task SendCoreAsync(TaskNotifyContext ctx, CancellationToken ct)
    {
        // 调门户 API；失败须抛出 → Outbox MarkFailed / 退避
        // 禁止「入队即成功」
    }
}
```

调试 / 生命周期 Hook 能力包：

```csharp
// pending 直接挂（无 client 依赖）
builder.UseDebugConsoleHook();
// → AddHook(new SailDebugConsoleHook(), o => { Match=All; Mode=Sync; Events=… })

// 需要 EnsureTable：PostBuild + addHook
builder.UseFormBizLifecycleHook();
```

```csharp
public sealed class SailFormBizLifecycleHook : IFlowHook
{
    readonly ISailFormBizHandler[] _handlers;
    public SailFormBizLifecycleHook(IEnumerable<ISailFormBizHandler> handlers) =>
        _handlers = handlers.ToArray();

    public string Name => "sleveen.formbiz.lifecycle";

    public void Handle(FlowHookContext ctx)
    {
        // OnProcessCompleted / Rejected / Closed → 按 FormCode 调 handler.OnFinish
    }
}
```

超时戳 Hook：

```csharp
builder.UseNodePolicyTimeoutStamp();
// PostBuild → addHook(SailNodePolicyTimeoutStampHook)
// 写 wf_task.ActivatedDateTime / OverdueDateTime
```

---

### 9.8 宿主 MS DI：`AddSail*`（与 Builder `Use*` 分工）

```csharp
// Program / HostSetup — MS DI
services.AddSailFormBiz();              // Scoped ISailFormBizHandler + Registry + Host
services.AddSailOperationHandlers();    // Scoped ISailOperationHandler + Dispatcher
services.AddSailNodePolicyJobs();       // HostedService：超时扫描 + 锚点消费

// Builder — 引擎侧挂生命周期 Hook
builder.UseFormBizLifecycleHook();
```

#### FormBiz Handler 骨干

```csharp
public interface ISailFormBizHandler
{
    string FormCode { get; }
    void OnInit(SailFormBizContext ctx);
    void OnQuery(SailFormBizContext ctx);
    void OnSubmit(SailFormBizContext ctx);   // 落业务表 + AppInstanceId / Conditions
    void OnStarted(SailFormBizContext ctx);
    void OnConsent(SailFormBizContext ctx);
    void OnBackToModify(SailFormBizContext ctx);
    void OnCancel(SailFormBizContext ctx);
    void OnFinish(SailFormBizContext ctx);   // 终态回写
}

public sealed class LeaveFormBizHandler : ISailFormBizHandler
{
    public string FormCode => "Leave";
    public void OnSubmit(SailFormBizContext ctx) { /* insert leave + 写 Conditions */ }
    public void OnFinish(SailFormBizContext ctx) { /* 更新请假状态 */ }
    // 其余生命周期按需空实现或抛 NotSupported
    public void OnInit(SailFormBizContext ctx) { }
    public void OnQuery(SailFormBizContext ctx) { }
    public void OnStarted(SailFormBizContext ctx) { }
    public void OnConsent(SailFormBizContext ctx) { }
    public void OnBackToModify(SailFormBizContext ctx) { }
    public void OnCancel(SailFormBizContext ctx) { }
}
```

#### Operation Handler 骨干

```csharp
public interface ISailOperationHandler
{
    IReadOnlyList<string> SupportedButtonIds { get; }
    Task<WfExecutedResult> ExecuteAsync(SailOperationContext ctx, CancellationToken ct = default);
}

public sealed class TransferOperationHandler : ISailOperationHandler
{
    public IReadOnlyList<string> SupportedButtonIds { get; } = new[] { "Transfer" };

    public async Task<WfExecutedResult> ExecuteAsync(SailOperationContext ctx, CancellationToken ct)
    {
        // 调引擎转交 API；返回 WfExecutedResult
        return WfExecutedResult.Success();
    }
}
```

---

### 9.9 完整组合根（对照清单）

```csharp
var client = CreateBuilder(configuration)
    .UseConfigFromAppSettings()
    .UseBizJson()
    .UseBizRules()
    .UseBizAction()
    .UseBizAssignees()
    .UseBizData()
    .UseBizRuntimeDefaults()
    .UseNextStepRoute(r =>
    {
        r.IfCondition<SampleAmountPrepare>(o => o.ProcessCodes = new[] { "EXPENSE" });
        r.AddFilter<SamplePermissionHideBranchFilter>(o => o.ProcessCodes = new[] { "EXPENSE" });
    })
    .UseFlowAspects(b =>
    {
        b.AddGate<FinancePreSendGate>(o => o.ProcessCodes = new[] { "EXPENSE" });
        b.AddHook(new FinanceAggregateHook(), o =>
        {
            o.ProcessCodes = new[] { "EXPENSE" };
            o.Events = new[] { EventFireTypeEnum.OnProcessContinued };
        });
    })
    .UseDebugConsoleHook()
    .UseFlowEventFileLog()
    .UseFormBizLifecycleHook()
    .UseNodePolicyTimeoutStamp()
    .UsePortalScheduleTaskNotifier(sp)     // 内含 Enabled + Drain
    .UseDataLifecycleFromConfig(configuration)
    .ConfigureServices(s =>
    {
        s.UseDatabase(…);
        s.UseBizLocalize();
        s.UseResource(…);
        s.UseForm(…);
        s.UseSchema(…);
        s.UseBizAI();
        s.UseSleveenOrgResource();         // 组织人员契约（与 Assignee Lookup 分离）
    })
    .Build();

client.UseSleveenOrgLookups(configuration);
client.UseSleveenNodePolicyAssignee(configuration);

// MS DI（与 SailClient 并行）
services.AddSailFormBiz();
services.AddSailOperationHandlers();
services.AddSailNodePolicyJobs();
```

---

### 9.10 选型决策（加新插件时）

| 要做的事 | 挂法 | 实现骨干 |
|----------|------|----------|
| 发送前校验 | `AddGate` / `UseFlowAspects` | `IFlowGate.Validate` → `HookResult` |
| 推进后业务副作用（可靠） | `AddHook` + 可选 Drain | `IFlowHook.Handle`；重活用 AsyncOutbox |
| 可丢副作用（文件日志） | `BestEffortFlowHookBase` + `AddHook`(Sync) | `TryCreatePayload` + `ConsumeAsync` |
| 待办通知 | `AddNotifier` + `UseHookOutboxDrain` | `TaskNotifyChannelBase` / `ReliableTaskNotifierBase` |
| 新算人 Type | `UseAssign`→`AddResolver` 或 `useAssigneeResolver` | `IAssigneeResolver` |
| 空人/同人策略 | `AddEmptyAction` / `AddFilter` | `IAssigneeEmptyAction` / `IAssigneeResolveFilter` |
| 流出判定 | `AddLeaveJudge` / `useLeaveJudge` | `IActivityLeaveJudge` |
| 连线写袋 / 求值 / 裁支 | `UseNextStepRoute` | `NextStepPrepare` / `IConditionEvaluator` / `INextStepFilter` |
| 图内 Action 语言 | `AddActionMethod` 或 `UseIronPython` | `IActionMethodHandler` |
| 业务表单生命周期 | `AddSailFormBiz` + `UseFormBizLifecycleHook` | `ISailFormBizHandler` |
| 办理按钮 | `AddSailOperationHandlers` | `ISailOperationHandler` |
| 可开关的一整包 | 自定义 `UseXxx` Feature | §5 / §9.7 模板 |

---

## 修订记录

| 日期 | 说明 |
|------|------|
| 2026-11-01 | 初版：固化 Builder 能力包最佳实践，对齐 sleveen-admin HostSetup 现行写法 |
| 2026-11-13 | 追加 §9：UseXxx/AddXxx 全面用法案例 + Gate/Hook/Notifier/Assignee/NextStep/FormBiz/Operation 骨干实现 |
