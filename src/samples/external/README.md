# sailflow.external（样例，非产品 ext）

Supabase 客户 / 会话 / 订单等 **演示用业务实体与服务**，以及引擎扩展文档样例（默认不注册）。

- 契约骨架：`IExternalService` / `ExternalServiceBase` 仍在 **pure/engine**
- 下一跳：`src/NextStep/`（`SampleAmountPrepare` / `SamplePermissionHideBranchFilter`）
- Gate/Hook：`src/Hooks/`（`FinancePreSendGate` / `FinanceAggregateHook`）
- **审批业务门面**：`src/Approval/`（`PurchaseApprovalBusiness` → `ISailApprovalBusiness`）
- Notifier：`src/Notify/SampleHttpTaskNotifier.cs`（依赖 `sailflow.msg`）
- Schema：`src/Schema/SampleSchemaInitializer.cs`（宿主建表 Demo；产品包不再提供 UseSchema 建表）
- 本包不进入产品默认引用面；宿主演示时再 ProjectReference

目录原属 `core/ext/external`，已按「业务实体下沉 / 样例离产品 ext」迁至 `samples/`。

## 审批门面样例（采购申请）

对应讨论稿 [仿真-采购申请与审批业务门面](../../../doc/sail/talk/仿真-采购申请与审批业务门面.md)：一业务一类，`FormCode=purchase`。

```csharp
using sailflow.biz.Hosting;
using sailflow.external.Approval;

var store = new PurchaseStore(); // 样例内存仓；产品请用表 + Sync Kit

builder
    .AddApprovalBusiness(_ => new PurchaseApprovalBusiness(store))
    .UseApprovalBusiness(); // Sync Finish/Aborted + Completing Gate；勿再挂第二套终态 Hook

// 列表进度（权限过滤归宿主）：
// query.QueryProgressByBizPKs("purchase", bizPkList);
```

| 回调 | 样例行为 |
|------|----------|
| `OnSubmit` / `OnStarted` | 落内存单、写 `amount` 条件、挂 PI |
| `OnCompleting(Run)` | 金额/终态校验，失败 `HookResult.Reject` |
| `OnBackToModify` | Status=退回修改 |
| `OnFinish` | approved→已通过；rejected→已驳回 |
| `OnAborted` | 关闭/撤回/作废等 → Aborted |
