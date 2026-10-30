# 09 · SailClient 外层管道设计（Builder / Collection / UseXxx）

> 状态：**Phase A/B/C（Form）已落地**（Resource 解耦仍待 Phase C 余量）  
> 对齐对象：ASP.NET Core `WebApplicationBuilder` / `IServiceCollection` / `UseXxx` 扩展管道  
> 关联现状：`SailClient`、`SailClientAssembler`、`SailClientAccess`、`saiflow.biz` 引导、`AiNodeExecutorLocator` / `FormServiceLocator`

---

## 1. 目标与原则

### 1.1 目标

`SailClient` 是业务侧的**应用级根实例**（类比 ASP.NET 的 `WebApplication` / 顶级 App）：

- **在业务侧构建**，不在引擎内部隐式 new。
- 构建时机由业务决定：
  - **积极构建**：Startup / `Program.cs` 显式 `Build()` 并 `SailClientAccess.Use(client)`。
  - **懒构建**：首次访问时由 `SailClientHost` / `SailClientAccess` 触发 `Build()`。
- 组件与服务的注册采用 **Collection + Fluent Builder + 扩展方法**，形如：

```csharp
builder.Services.UseBizAI()
                .UseBizDocs()
                .UseForm()
                .UseSchema(o => o.Seed = true);
```

### 1.2 原则

| 原则 | 说明 |
|------|------|
| 引擎只提供管道骨架 | `ISailCollection` / `SailClientBuilder` / 核心插槽契约在 `engine`（或 `basis`） |
| 业务包只写扩展 | `UseBizAI` / `UseForm` 等在各自程序集，对 `ISailCollection` 扩展 |
| Build 一次、只读运行 | `Build()` 之后集合冻结；运行期通过 `SailClient` / Locator 解析 |
| 可与 MS DI 共存 | Collection 可桥接 `IServiceCollection`，但不强制宿主使用 MS DI |
| 兼容现状 | 保留 `SailClientAssembler` 一段时间，内部改为调用 Builder；`SailClientAccess` 仍为进程内入口 |

### 1.3 非目标（本阶段不做）

- 不重写引擎内部 `Manager` 的取库方式（仍可用 `SailClientAccess.UseRunSQL()`）。
- 不把 `SailClient` 变成完整 IoC 容器替代品；它是**应用根 + 注册总线**，解析可委托给简单 Locator 或 `IServiceProvider`。
- 不强制多 `SailClient` 实例并存（默认同进程单例；多租户可后续扩展 `named client`）。

---

## 2. 概念对照（ASP.NET → Sailflow）

| ASP.NET Core | Sailflow |
|--------------|----------|
| `WebApplication` / App | `SailClient` |
| `WebApplicationBuilder` | `SailClientBuilder` |
| `IServiceCollection` | `ISailCollection` |
| `services.AddXxx()` / `app.UseXxx()` | `builder.Services.UseXxx()`（统一用 Use 前缀，表示“启用某能力包”） |
| `builder.Build()` | `builder.Build()` → `SailClient` |
| `Program.cs` / Startup | 业务宿主（ASP.NET Host、控制台、WinService 等） |
| 懒加载 Host | `SailClientHost.GetOrCreate(...)` / `SailClientAccess.Ensure(...)` |

说明：ASP.NET 里 `Add*` 偏 DI 注册、`Use*` 偏中间件。Sailflow 外层管道更接近“能力包启用”，**统一命名为 `UseXxx`**，避免与 MS DI 的 `Add*` 混淆；若未来桥接 `IServiceCollection`，扩展内部再 `services.AddSingleton<...>()`。

---

## 3. 总体结构

```
┌─────────────────────────────────────────────────────────┐
│  Host (ASP.NET / Console / Test)                        │
│                                                         │
│   var builder = SailClient.CreateBuilder(args/config);  │
│   builder.Services                                      │
│       .UseDatabase(db)                                  │
│       .UseBizAI()                                       │
│       .UseForm()                                        │
│       .UseSchema(o => { o.IncludeAi = true; });         │
│   var client = builder.Build();   // or lazy Ensure()   │
│   SailClientAccess.Use(client);                         │
└──────────────────────────┬──────────────────────────────┘
                           │ Build
                           ▼
┌─────────────────────────────────────────────────────────┐
│  SailClient (应用根实例)                                 │
│   · DBFactory / Dialect / Config（已有插槽）             │
│   · Features / Locators（AI、Form、Resource…）           │
└─────────────────────────────────────────────────────────┘
         ▲ 注册回调来自各包的 UseXxx
         │
┌────────┴────────┐  ┌──────────────┐  ┌────────────────┐
│ sailflow.biz.AI │  │ sailflow.form│  │ sailflow.biz   │
│ UseBizAI()      │  │ UseForm()    │  │ UseSchema()    │
└─────────────────┘  └──────────────┘  └────────────────┘
```

### 3.1 程序集归属

| 类型 | 程序集 | 说明 |
|------|--------|------|
| `ISailCollection` / `SailCollection` | `sailflow.engine`（`saiflow.engine.Client.Hosting`） | 管道契约与默认实现 |
| `SailClientBuilder` | 同上 | Fluent 构建器 |
| `SailFeatureDescriptor` / 启动钩子 | 同上 | 描述“注册了什么、Build 时做什么” |
| `SailClient` / `SailClientAccess` | `sailflow.engine`（已有） | 运行根与进程入口 |
| `UseBizAI` | `sailflow.biz.AI` | AI 执行器 + AI 表 |
| `UseBizDocs` | `sailflow.biz.docs` | PDF LocalService 等 |
| `UseForm` | `sailflow.form` | `IFormService` 注册 |
| `UseResource` | `sailflow.resource` | `IResourceService` 注册 |
| `UseSchema` / `InitDb` | `sailflow.biz`（host） | 核心表 + 可选 AI 表 + 种子 |
| 宿主 `Program.cs` | 业务应用 | 调用管道 |

---

## 4. 核心 API 设计

### 4.1 `ISailCollection`

类比 `IServiceCollection`：可枚举的注册表，供扩展方法追加“功能描述符”。

```csharp
namespace saiflow.engine.Client.Hosting;

/// <summary>
/// Sail 能力注册集合（类比 IServiceCollection）。
/// 扩展方法应对本接口编程，勿依赖具体 SailCollection。
/// </summary>
public interface ISailCollection : IList<SailFeatureDescriptor>
{
    /// <summary>构建期共享属性袋（配置片段、开关、自定义对象）。</summary>
    IDictionary<object, object?> Properties { get; }

    /// <summary>可选：桥接到宿主 MS DI；未设置时走 Sail 内置解析。</summary>
    IServiceCollection? ServiceCollection { get; set; }

    /// <summary>便捷：追加一个已命名的功能描述符。</summary>
    ISailCollection Add(SailFeatureDescriptor descriptor);
}
```

### 4.2 `SailFeatureDescriptor`

```csharp
namespace saiflow.engine.Client.Hosting;

public enum SailFeatureLifetime
{
    /// <summary>注册服务/Locator（Build 前写入集合）。</summary>
    ConfigureServices = 0,
    /// <summary>在 SailClient 骨架就绪后执行（可访问 client.UseRunSQL）。</summary>
    ConfigureClient = 1,
    /// <summary>Build 末尾（建表、种子、热身）。</summary>
    PostBuild = 2
}

/// <summary>
/// 一次 UseXxx 贡献的注册项。可多次调用同一 phase。
/// </summary>
public sealed class SailFeatureDescriptor
{
    public required string Name { get; init; }
    public SailFeatureLifetime Phase { get; init; }
    public int Order { get; init; }

    /// <summary>ConfigureServices：仅操作 collection / MS DI。</summary>
    public Action<ISailCollection>? ConfigureServices { get; init; }

    /// <summary>ConfigureClient：骨架已 Assemble，可挂服务到 client。</summary>
    public Action<SailClient, ISailCollection>? ConfigureClient { get; init; }

    /// <summary>PostBuild：例如 EnsureTables / Seed。</summary>
    public Action<SailClient, ISailCollection>? PostBuild { get; init; }
}
```

**Order 约定（建议）：**

| Order | 用途 |
|------|------|
| 0–99 | 基础设施（Database、Config、Event） |
| 100–199 | 核心扩展（Form、Resource、Localize） |
| 200–299 | 业务 AI / Docs |
| 900–999 | Schema / Seed / 诊断 |

### 4.3 `SailCollection`（默认实现）

```csharp
namespace saiflow.engine.Client.Hosting;

public sealed class SailCollection : List<SailFeatureDescriptor>, ISailCollection
{
    public IDictionary<object, object?> Properties { get; } =
        new Dictionary<object, object?>();

    public IServiceCollection? ServiceCollection { get; set; }

    public ISailCollection Add(SailFeatureDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        Add(descriptor); // List.Add — 注意命名冲突，实现里用 base.Add / ((List<>)this).Add
        return this;
    }
}
```

实现时注意：`Add(descriptor)` 与 `List.Add` 同名，建议方法名为 `AddFeature` 或显式接口实现。定稿 API：

```csharp
public ISailCollection AddFeature(SailFeatureDescriptor descriptor)
{
    ArgumentNullException.ThrowIfNull(descriptor);
    base.Add(descriptor);
    return this;
}
```

`ISailCollection` 对应改为 `AddFeature`。

### 4.4 `SailClientBuilder`

```csharp
namespace saiflow.engine.Client.Hosting;

public sealed class SailClientBuilder
{
    public ISailCollection Services { get; } = new SailCollection();
    public IConfiguration? Configuration { get; private set; }

    /// <summary>尚未 Build 的数据库工厂；也可由 UseDatabase 扩展写入 Properties。</summary>
    public SailDBFactory? DbFactory { get; set; }

    public SailClientBuilder UseConfiguration(IConfiguration configuration)
    {
        Configuration = configuration;
        return this;
    }

    public SailClientBuilder ConfigureServices(Action<ISailCollection> configure)
    {
        configure(Services);
        return this;
    }

    /// <summary>积极构建：立即得到 SailClient，并可选绑定到 SailClientAccess。</summary>
    public SailClient Build(bool bindAccess = true)
    {
        // 1) ConfigureServices phase
        foreach (var d in Services.OrderBy(x => x.Order)
                                  .Where(x => x.Phase == SailFeatureLifetime.ConfigureServices))
            d.ConfigureServices?.Invoke(Services);

        // 2) 解析 DbFactory（扩展 UseDatabase 应已设置）
        var dbFactory = DbFactory
            ?? Services.Properties.TryGetValue(SailBuilderKeys.DbFactory, out var box)
                ? box as SailDBFactory
                : null;
        if (dbFactory == null)
            throw new InvalidOperationException(
                "SailDBFactory is required. Call builder.Services.UseDatabase(...) before Build().");

        // 3) 组装骨架（复用现有 Assembler 逻辑）
        var client = SailClientAssembler.Assemble(dbFactory, Configuration);

        // 4) ConfigureClient phase
        foreach (var d in Services.OrderBy(x => x.Order)
                                  .Where(x => x.Phase == SailFeatureLifetime.ConfigureClient))
            d.ConfigureClient?.Invoke(client, Services);

        // 5) PostBuild phase
        foreach (var d in Services.OrderBy(x => x.Order)
                                  .Where(x => x.Phase == SailFeatureLifetime.PostBuild))
            d.PostBuild?.Invoke(client, Services);

        if (bindAccess)
            SailClientAccess.Use(client);

        return client;
    }
}

public static class SailBuilderKeys
{
    public static readonly object DbFactory = new();
    public static readonly object FormService = new();
    public static readonly object ResourceService = new();
}
```

### 4.5 `SailClient` 入口工厂

在现有 `SailClient` 上增加静态工厂（或并列静态类，避免膨胀）：

```csharp
// SailClient.Hosting.cs （partial）
public partial class SailClient
{
    public static SailClientBuilder CreateBuilder() => new();

    public static SailClientBuilder CreateBuilder(IConfiguration configuration)
        => new SailClientBuilder().UseConfiguration(configuration);
}
```

### 4.6 懒构建宿主 `SailClientHost`

```csharp
namespace saiflow.engine.Client.Hosting;

/// <summary>
/// 懒构建入口：首次 Get() 时执行 builder 工厂。
/// </summary>
public static class SailClientHost
{
    private static readonly object Gate = new();
    private static Func<SailClientBuilder>? _builderFactory;
    private static SailClient? _client;

    /// <summary>注册构建配方（不立即 Build）。</summary>
    public static void Configure(Func<SailClientBuilder> builderFactory)
    {
        ArgumentNullException.ThrowIfNull(builderFactory);
        lock (Gate)
        {
            if (_client != null)
                throw new InvalidOperationException("SailClient already built.");
            _builderFactory = builderFactory;
        }
    }

    /// <summary>积极或懒：若未构建则 Build。</summary>
    public static SailClient GetOrCreate()
    {
        if (_client != null) return _client;
        lock (Gate)
        {
            if (_client != null) return _client;
            if (_builderFactory == null)
                throw new InvalidOperationException(
                    "Call SailClientHost.Configure(() => { var b = SailClient.CreateBuilder(); ...; return b; }) first.");
            _client = _builderFactory().Build(bindAccess: true);
            return _client;
        }
    }

    public static void Reset() // 测试用
    {
        lock (Gate)
        {
            _client = null;
            _builderFactory = null;
            SailClientAccess.Clear();
        }
    }
}
```

`SailClientAccess` 可增加：

```csharp
public static SailClient Ensure()
    => IsConfigured ? Required : SailClientHost.GetOrCreate();
```

---

## 5. 扩展方法约定（UseXxx）

### 5.1 命名与签名

```csharp
public static class XxxSailBuilderExtensions
{
    public static ISailCollection UseXxx(
        this ISailCollection services,
        Action<XxxOptions>? configure = null)
    {
        var options = new XxxOptions();
        configure?.Invoke(options);

        return services.AddFeature(new SailFeatureDescriptor
        {
            Name = "Xxx",
            Phase = SailFeatureLifetime.ConfigureClient, // 或拆成多项
            Order = 2xx,
            ConfigureClient = (client, coll) => { /* 注册 */ },
            PostBuild = (client, coll) => { /* 可选建表 */ }
        });
    }
}
```

规则：

1. 第一个参数必须是 `this ISailCollection`。
2. 返回 `ISailCollection` 以支持链式调用。
3. **幂等**：同一 `Name` 重复 `UseXxx` 时，后写覆盖或忽略（建议：同名替换，并 Debug 日志）。
4. 不在扩展方法里直接 `Build()`。
5. 需要配置时用 `Action<TOptions>`，默认 options 安全。

### 5.2 推荐扩展一览

| 扩展 | 程序集 | Phase | 职责 |
|------|--------|-------|------|
| `UseDatabase(SailDBFactory)` / `UseDatabase(DBInstance)` | engine | ConfigureServices | 写入 DbFactory |
| `UseConfiguration(IConfiguration)` | engine（Builder 方法已有） | — | 配置 |
| `UseEventHub(ISailEventHub)` | engine | ConfigureClient | 替换默认 Event stub |
| `UseForm()` | form | ConfigureClient | 注册 `IFormService` → Locator |
| `UseResource()` | resource | ConfigureClient | 注册 `IResourceService` |
| `UseBizAI()` | biz.AI | ConfigureClient + PostBuild | `AiNodeExecutorLocator` + `AiRuntime.EnsureTables` |
| `UseBizDocs()` | biz.docs | ConfigureClient | 文档 LocalService 相关（若需） |
| `UseSchema(...)` | biz host | PostBuild | 核心表 + 种子；可组合 AI 表 |

### 5.3 具体实现示例：`UseDatabase`

```csharp
namespace saiflow.engine.Client.Hosting;

public static class SailDatabaseExtensions
{
    public static ISailCollection UseDatabase(this ISailCollection services, SailDBFactory dbFactory)
    {
        ArgumentNullException.ThrowIfNull(dbFactory);
        services.Properties[SailBuilderKeys.DbFactory] = dbFactory;
        return services;
    }

    public static ISailCollection UseDatabase(this ISailCollection services, mooSQL.data.DBInstance db, int mainPosition = 0)
        => services.UseDatabase(new DelegateSailDBFactory(db, mainPosition));
}
```

Builder 侧也可提供语法糖：

```csharp
public SailClientBuilder UseDatabase(SailDBFactory factory)
{
    DbFactory = factory;
    Services.UseDatabase(factory);
    return this;
}
```

### 5.4 具体实现示例：`UseBizAI`

```csharp
namespace saiflow.AI.Hosting; // 位于 sailflow.biz.AI

public static class BizAiSailExtensions
{
    public static ISailCollection UseBizAI(
        this ISailCollection services,
        Action<BizAiOptions>? configure = null)
    {
        var opt = new BizAiOptions();
        configure?.Invoke(opt);

        services.AddFeature(new SailFeatureDescriptor
        {
            Name = "BizAI",
            Phase = SailFeaturePhase.ConfigureClient,
            Order = 200,
            ConfigureClient = (_, _) =>
            {
                if (opt.RegisterExecutor)
                    AiRuntime.UseDefaultExecutor(); // 现有入口，后续可改为基于 client 的非静态注册
            }
        });

        if (opt.EnsureTables)
        {
            services.AddFeature(new SailFeatureDescriptor
            {
                Name = "BizAI.Schema",
                Phase = SailFeaturePhase.PostBuild,
                Order = 920,
                PostBuild = (client, _) => AiRuntime.EnsureTables(client.UseRunSQL())
            });
        }

        return services;
    }
}

public sealed class BizAiOptions
{
    public bool RegisterExecutor { get; set; } = true;
    public bool EnsureTables { get; set; } = true;
}
```

### 5.5 具体实现示例：`UseForm`（为后续解耦做准备）

```csharp
namespace saiflow.hub.Form.Hosting; // 位于 sailflow.form

public static class FormSailExtensions
{
    public static ISailCollection UseForm(this ISailCollection services)
    {
        return services.AddFeature(new SailFeatureDescriptor
        {
            Name = "Form",
            Phase = SailFeaturePhase.ConfigureClient,
            Order = 120,
            ConfigureClient = (_, coll) =>
            {
                // 过渡：写入 Properties；WorkflowService 改为 FormServiceLocator.Resolve()
                coll.Properties[SailBuilderKeys.FormService] = FormServiceFactory.Create();
                FormServiceLocator.Register(
                    (IFormService)coll.Properties[SailBuilderKeys.FormService]!);
            }
        });
    }
}
```

引擎侧最终改为：

```csharp
FormService = FormServiceLocator.Resolve(); // 不再 FormServiceFactory.Create()
```

从而 **engine 不再 ProjectReference form**（与 AI 解耦同构）。

### 5.6 具体实现示例：`UseSchema`（biz host）

```csharp
namespace saiflow.biz.Hosting;

public static class SchemaSailExtensions
{
    public static ISailCollection UseSchema(
        this ISailCollection services,
        Action<SchemaOptions>? configure = null)
    {
        var opt = new SchemaOptions();
        configure?.Invoke(opt);

        return services.AddFeature(new SailFeatureDescriptor
        {
            Name = "Schema",
            Phase = SailFeaturePhase.PostBuild,
            Order = 900,
            PostBuild = (client, _) =>
            {
                SailSchemaInitializer.EnsureTables(client, includeAi: opt.IncludeAi);
                if (opt.Seed)
                    SailSchemaInitializer.EnsureMinimalSeed(client);
            }
        });
    }
}

public sealed class SchemaOptions
{
    public bool IncludeAi { get; set; } = true;
    public bool Seed { get; set; } = false;
}
```

> 注意：若同时 `UseBizAI(o => o.EnsureTables = true)` 与 `UseSchema(o => o.IncludeAi = true)`，AI 表可能建两次——`EnsureTables` 必须幂等（现有 `useDBInit().autoMode()` 已满足）。

---

## 6. 宿主用法（定稿示例）

### 6.1 积极构建（Startup）

```csharp
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;
using saiflow.AI.Hosting;
using saiflow.biz.Hosting;
using saiflow.hub.Form.Hosting;

// Program.cs / Startup
var builder = SailClient.CreateBuilder(configuration);
builder.Services
    .UseDatabase(dbInstance)          // 或 UseDatabase(dbFactory)
    .UseForm()
    .UseResource()                    // 规划中
    .UseBizAI()
    .UseBizDocs()                     // 规划中
    .UseSchema(o =>
    {
        o.IncludeAi = true;           // 若 UseBizAI 已 EnsureTables，可关其一
        o.Seed = env.IsDevelopment();
    });

var sail = builder.Build();           // 内部 SailClientAccess.Use(sail)
```

### 6.2 懒构建

```csharp
SailClientHost.Configure(() =>
{
    var b = SailClient.CreateBuilder(configuration);
    b.Services
        .UseDatabase(dbInstance)
        .UseBizAI()
        .UseSchema(o => o.Seed = false);
    return b;
});

// 任意首次用到流程时：
var client = SailClientHost.GetOrCreate();
// 或 Manager 路径：SailClientAccess.Ensure().UseRunSQL();
```

### 6.3 与 ASP.NET Core 宿主组合

```csharp
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var db = sp.GetRequiredService<DBInstance>(); // 宿主自己的注册

    var sailBuilder = SailClient.CreateBuilder(config);
    sailBuilder.Services
        .UseDatabase(db)
        .UseBizAI()
        .UseForm()
        .UseSchema();

    // 可选：把同一 IServiceCollection 挂上，供高级扩展使用
    sailBuilder.Services.ServiceCollection = builder.Services;

    return sailBuilder.Build(bindAccess: true);
});
```

---

## 7. 与现状代码的映射与迁移

### 7.1 现状 → 新管道

| 现状 | 新管道 |
|------|--------|
| `SailClientAssembler.Assemble(db, config)` | `CreateBuilder().UseDatabase(db).UseConfiguration(config).Build()` |
| `SailClientAccess.Use(client)` | `Build(bindAccess: true)` 默认完成 |
| `AiRuntime.UseDefaultExecutor()` | `UseBizAI()` |
| `AiRuntime.EnsureTables(kit)` | `UseBizAI` PostBuild 或 `UseSchema(IncludeAi)` |
| `SailSchemaInitializer.InitDb(client)` | `UseSchema` + 可选 Seed；或保留方法供脚本调用 |
| `FormServiceFactory.Create()` in engine | `UseForm()` + `FormServiceLocator` |
| 静态 Locator 散落 | 逐步收敛到 Collection Properties / 统一 `SailServiceLocator` |

### 7.2 分阶段落地（建议）

**Phase A — 管道骨架（engine）**

1. 新增 `Client/Hosting/`：`ISailCollection`、`SailCollection`、`SailFeatureDescriptor`、`SailClientBuilder`、`SailClientHost`、`UseDatabase`。
2. `SailClient.CreateBuilder()`。
3. `Build()` 内部调用现有 `SailClientAssembler`。
4. 文档与单测：无扩展时仅 Database + Config 可 Build。

**Phase B — biz 接入**

1. `UseBizAI` / `UseSchema` 扩展。
2. 示例宿主改为管道写法；`SailSchemaInitializer.InitDb` 标记为过时包装。

**Phase C — Form / Resource 解耦**

1. `FormServiceLocator` / `ResourceServiceLocator`（或统一 Locator）。
2. `UseForm` / `UseResource`。
3. 去掉 engine → form / resource 的实现型 ProjectReference（仅保留 baseHub 契约）。

**Phase D — 可选 MS DI 桥**

1. `ISailCollection.ServiceCollection` 深度集成。
2. `Build()` 可产出 `IServiceProvider` 子集（非必须）。

### 7.3 兼容策略

- `SailClientAssembler` **保留为 public**，标注：推荐改用 `SailClient.CreateBuilder()`。
- 旧 `InitDb` 可实现为：

```csharp
public static SailInitDbResult InitDb(SailClient client, ...)
{
    // 直接跑表，不走 Builder；供迁移期脚本使用
}
```

---

## 8. 设计细节与约束

### 8.1 线程安全

- `Build()` / `GetOrCreate()` 必须对进程内单例加锁（见 `SailClientHost`）。
- `ISailCollection` 仅在 Build 前单线程写入；Build 后视为不可变。

### 8.2 错误信息

缺少 `UseDatabase`、缺少 `UseBizAI` 却执行 AI 节点等，错误信息必须指明**应调用的扩展方法**（延续 `AiNodeExecutorLocator` 风格）。

### 8.3 测试

- 单元测试可用 `SailClientHost.Reset()`。
- 集成测试：内存库 / SQLite + `UseDatabase` + `UseSchema`。

### 8.4 与 BeeFlow / 文档 08 的关系

现有注释指向「业务不持有 DBInstance、经 Client 取 SQLBuilder」。本管道**强化**该约定：Database 只在 Builder 期注入一次，运行期一律 `client.UseRunSQL()` / `SailClientAccess.UseRunSQL()`。

---

## 9. 目录规划（落地时）

```
src/core/pure/engine/src/Client/
  SailClient.cs                    # 已有；增加 CreateBuilder partial
  SailClientAssembler.cs           # 已有；保留
  SailClientAccess.cs              # 已有；增加 Ensure()
  Hosting/
    ISailCollection.cs
    SailCollection.cs
    SailFeatureDescriptor.cs
    SailClientBuilder.cs
    SailClientHost.cs
    SailBuilderKeys.cs
    SailDatabaseExtensions.cs

src/biz/AI/src/Hosting/
  BizAiSailExtensions.cs
  BizAiOptions.cs

src/biz/host/src/Hosting/
  SchemaSailExtensions.cs
  SchemaOptions.cs

src/core/ext/form/src/Hosting/
  FormSailExtensions.cs
  FormServiceLocator.cs            # Phase C
```

---

## 10. 最小可运行伪代码（验收标准）

以下代码在 Phase A+B 完成后应可编译运行：

```csharp
var client = SailClient.CreateBuilder(config)
    .ConfigureServices(s => s
        .UseDatabase(db)
        .UseBizAI()
        .UseSchema(o => o.Seed = true))
    .Build();

Debug.Assert(SailClientAccess.IsConfigured);
Debug.Assert(AiNodeExecutorLocator.IsRegistered);
_ = client.UseRunSQL(); // 可取库
```

懒构建验收：

```csharp
SailClientHost.Configure(() =>
    SailClient.CreateBuilder(config).ConfigureServices(s => s.UseDatabase(db)));

var a = SailClientHost.GetOrCreate();
var b = SailClientHost.GetOrCreate();
Debug.Assert(ReferenceEquals(a, b));
```

---

## 11. 决策摘要

1. **外层管道**采用 `SailClientBuilder` + `ISailCollection` + `UseXxx` 扩展，对齐 .NET 6+ 宿主体验。  
2. **SailClient** 仍是唯一运行根；Builder 只负责装配。  
3. **积极 / 懒构建**均支持；业务自选。  
4. **引擎提供管道**；**业务包提供 UseXxx**；宿主只编排扩展调用。  
5. 与 AI/Form 解耦路线一致：实现注册进管道，引擎只依赖契约/Locator。  

本文档作为后续实现与 Code Review 的基准；实现 PR 应按第 7.2 节分阶段提交。
