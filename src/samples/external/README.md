# sailflow.external（样例，非产品 ext）

Supabase 客户 / 会话 / 订单等 **演示用业务实体与服务**，以及引擎扩展文档样例（默认不注册）。

- 契约骨架：`IExternalService` / `ExternalServiceBase` 仍在 **pure/engine**
- 下一跳：`src/NextStep/`（`SampleAmountPrepare` / `SamplePermissionHideBranchFilter`）
- Gate/Hook：`src/Hooks/`（`FinancePreSendGate` / `FinanceAggregateHook`）
- Notifier：`src/Notify/SampleHttpTaskNotifier.cs`（依赖 `sailflow.msg`）
- Schema：`src/Schema/SampleSchemaInitializer.cs`（宿主建表 Demo；产品包不再提供 UseSchema 建表）
- 本包不进入产品默认引用面；宿主演示时再 ProjectReference

目录原属 `core/ext/external`，已按「业务实体下沉 / 样例离产品 ext」迁至 `samples/`。
