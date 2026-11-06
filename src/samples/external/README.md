# sailflow.external（样例，非产品 ext）

Supabase 客户 / 会话 / 订单等 **演示用业务实体与服务**。

- 契约骨架：`IExternalService` / `ExternalServiceBase` 仍在 **pure/engine**
- 本包不进入产品默认引用面；宿主若要对接真实客户域，在业务侧自建实现并反射挂到流程 LocalService

目录原属 `core/ext/external`，已按「业务实体下沉 / 样例离产品 ext」迁至 `samples/`。
