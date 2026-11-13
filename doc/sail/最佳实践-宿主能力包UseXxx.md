# 最佳实践：宿主能力包（Builder · UseXxx）

> 状态：现行约定（2026-11-01）  
> 适用：在 **宿主** 或 **biz** 中挂 Gate / Hook / Notifier / Assignee / Outbox Drain 等可插拔能力  
> 关联：[业务管道特性设计.md](./业务管道特性设计.md) · [特性设计-回调与事件.md](./特性设计-回调与事件.md) · [架构设计-项目依赖与分层.md](./架构设计-项目依赖与分层.md)  
> 宿主落点示例：`sleveen-admin/doc/sailflow-host-capability-pack.md`

---

## 1. 一句话

**一项可开关的能力 = 一个 `UseXxx` 扩展方法**；在 `SailClientBuilder` 链式调用里声明；**启用判定、建表、注册 Hook/Notifier、启 Drain 都收进扩展内部**，宿主 `Program` / HostSetup **不要**再散落 `if (enabled) { client.use… }`。

---

## 2. 为什么

| 反模式 | 问题 |
|--------|------|
| `Build()` 后大段 `if` + `useFlowHook` / `useTaskNotifier` | 组合根膨胀；开关、事件列表、建表散落，难复用、难测 |
| 把业务 Hook 写进引擎 pure | 违反分层；宿主主数据/HTTP 无法干净依赖 |
| 只挂 `client.useXxx`、从不走 Builder | 与引擎 pending Apply / PostBuild 生命周期脱节；同类能力风格分裂 |

正模式：与 `UseDatabase` / `UseBizAI` / `UseFlowHook` 同一管道语言——**Builder 声明 → Build 时按 Phase 落地**。

---

## 3. 双轨 API（引擎已提供）

| 时机 | 命名风格 | 机制 |
|------|----------|------|
| **构建中（推荐默认）** | `builder.UseXxx` / `services.UseXxx` | 写入 `ISailCollection` pending 或 `SailFeatureDescriptor`；`Build()` 时 Apply |
| **构建后（补挂）** | `client.useXxx` | 已有 `SailClient` 上即时注册 |

Gate / Hook / Notifier 引擎侧：

- Builder：`UseFlowGate` / `UseFlowHook` / `UseTaskNotifier`
- Client：`useFlowGate` / `useFlowHook` / `useTaskNotifier`
- pending 在 `Build()` 中 `ApplyPendingRegistrations`（早于或穿插 ConfigureClient；以代码为准）

宿主能力包应 **优先实现 Builder 扩展**；可选保留同文件 `client.useXxx` 薄封装，便于测试或热补挂。

---

## 4. Build 生命周期（注册落点）

```text
ConfigureServices   → UseDatabase / UseSchema 配置 / UseHookOutboxDrain 选项等
Assemble SailClient
ApplyPending        → UseFlowHook / UseTaskNotifier 等 pending 注册
ConfigureClient     → Locator、Drain 绑到 client 等
PostBuild           → EnsureTables、Seed、依赖 client 的 Notifier 实例化 + useTaskNotifier
```

| 能力形态 | 推荐 Phase |
|----------|------------|
| 无运行时依赖的 Hook（如调试 Console） | `UseFlowHook` pending（ConfigureServices 之前挂到 Builder 即可） |
| 需要 `SailClient` 才能 `new Service(client)` / EnsureTable | **PostBuild** 里 `EnsureTable` + `client.useFlowHook` / `useTaskNotifier` |
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
        // builder.UseFlowHook(new MyHook(), o => { … });

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

## 修订记录

| 日期 | 说明 |
|------|------|
| 2026-11-01 | 初版：固化 Builder 能力包最佳实践，对齐 sleveen-admin HostSetup 现行写法 |
