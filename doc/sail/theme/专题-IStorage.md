# 专题 · IStorage（数据库连接器）

> 状态：**实施中**（运行时套表含 Variable 已收口；Definition/XPDL 维持独立路径，不并入 kit 族）  




> 用途：流程定义 / 实例套表的**纯库交互层**——无状态、`kit` 首参、热点专属 API、唯一注册可覆盖；为实施提供可执行契约。  
> 讨论溯源：[讨论-办理人操作模式与人员范围正交.md](../talk/讨论-办理人操作模式与人员范围正交.md)（§6、§8 已定寿命）  
> 关联：[专题-对象生命周期与复用边界.md](./专题-对象生命周期与复用边界.md) · [架构设计-项目依赖与分层.md](../架构设计-项目依赖与分层.md) · [业务管道特性设计.md](../业务管道特性设计.md) · [专题-事务一致性.md](./专题-事务一致性.md) · [专题-对象解析器.md](./专题-对象解析器.md)

---

## 0. 结论先行

| 项 | 口径 |
|----|------|
| **一句话** | `IStorage` = 纯数据库交互；CRUD + 查询 DTO；**不含业务逻辑** |
| **事务** | **`SQLBuilder kit` 为第一参数**；连接器不持 kit/连接字段 |
| **状态 / 寿命** | **无状态**；**同寿于一个 `SailClient` 注册槽**（已拍；非跨 Client 进程上帝单例） |
| **热点** | 状态更新等建**专属方法**；禁止只靠厚仓储泛化 |
| **默认实现** | **ext**；可继承；注册替换 |
| **注册** | **唯一槽**；后注册覆盖（建议打日志，类比 R9） |
| **DI** | 引擎**仿 DI**（`ISailCollection` / `UseXxx`）；宿主**真 DI** 桥接注入 |
| **与办理人落库** | [对象解析器](./专题-对象解析器.md) 的 pre/`LoadPicked` 默认实现应**调用**本层，不另写 SQL 风格 |

---

## 1. 一句话与边界

```text
Lane / 规则 → *Manager（业务） → IStorage / 热点 API（纯库） → mooSQL kit
```

| 做 | 不做 |
|----|------|
| 按表或聚合暴露 Storage 接口 | **不**在 Storage 内写 Gate/Hook/ReasonCode 业务分支 |
| kit 首参传事务 | **不**把 kit 存成 Storage 字段 |
| 热点列更新专属 API | **不**热路径只 `Update(整实体)` |
| ext 默认 + 唯一覆盖 | **不**同键多实现隐式聚合（≠ Filter 链） |
| Manager 依赖接口 | **不**让 Lane 直接 `useRepo` 散落 |
| 试点表渐进下沉 | **不**大爆炸改写全部 Manager |

---

## 2. 契约示意（非 API 定稿名）

```csharp
// 无状态；kit 首参
public interface IStorage<TEntity>
{
    TEntity? GetById(SQLBuilder kit, string id);
    string Insert(SQLBuilder kit, TEntity row);
    void Update(SQLBuilder kit, TEntity row);
    // 列表 / 查询 DTO …
}

// 热点专属：勿只靠通用 Update(entity)
public interface IProcessInstanceStorage // 可继承或独立于 IStorage<>
{
    int UpdateState(SQLBuilder kit, string processInstanceId, int newState /*, 乐观版本? */);
}
```

```text
注册（仿 DI）
  UseStorage<IProcessInstanceStorage, DefaultProcessInstanceStorage>()
  // 或 client.useStorage(...)
  // 同键后注册 → 覆盖 + 日志

宿主
  真 DI 解析 → Build 时写入 Sail 唯一槽
```

**接口粒度：** 口述「每表一接口」可落为「每表有主入口」；实现允许按**聚合根**收敛（Process / Activity / Task / Transition / Definition），避免类型爆炸。

---

## 3. 寿命与 DI（已拍）

| 说法 | 口径 |
|------|------|
| 无状态、可共享 | ✅ 不持 Runner/kit；同一 `SailClient` 内单例持有 |
| 跨多个 Client 的进程上帝单例 | ❌ 默认禁止（对齐 [对象生命周期](./专题-对象生命周期与复用边界.md) A 档） |

`kit` 永远是调用入参（**B**）。

| 规则 | 说明 |
|------|------|
| 引擎不强制 MS.DI | 保持 `ISailCollection` / Builder / `useXxx` |
| 宿主真 DI | `AddSingleton` 等解析后 `useStorage` 注入 |
| 唯一注册 | 一键一槽；后写覆盖；建议覆盖日志 |

---

## 4. 与现行差距

| 现行 | 目标 |
|------|------|
| `TaskManager` / `ProcessManager` / … 内 `kit.useRepo` + 业务同文件 | SQL 下沉 Storage；规则留 Manager/Lane |
| `IPreselectAssigneeStore` 等零散 Store | 统一 kit 首参 + 无状态 + 可覆盖；可归为 Storage 族 |
| `DefaultManagerFactory` 懒持 Manager（A） | Storage 同为 A；Manager **注入** Storage 接口 |

---

## 5. 专属方法 vs 厚仓储

**鼓励：** `UpdateProcessState`、`MarkTaskCompleted`、`BatchInsertTasks`；查询返回投影 DTO。  

**避免：** 热路径整行 Update；Storage 内发 Hook；空壳泛型仓储零专属方法。  

**软删：** 若表有软删语义，删除 API 用 Update 标记还是物理删须**按表写清**，禁止 silently 混用。

---

## 6. 与对象解析器落库

| | 对象解析器 Mode Store | 本专题 IStorage |
|--|----------------------|-----------------|
| 对象 | 预选 / 自选**办理人项集** | **流程套表**行 |
| 接口 | pre / `LoadPicked`·`StorePicked` | `IStorage<T>` / 专属更新 |
| 关系 | Mode 默认实现 **内部调用** 对应表 Storage | 提供表访问 |

---

## 7. 实施切片（建议）

```text
S0  ✅ 注册槽约定 + kit 首参 + 覆盖日志（SailStorageRegistry / useStorage / UseDefaultStorages）
S1  ✅ ProcessInstanceManager：GetById + Complete/Suspend/Resume/Reverse/RecallSubProcess → Storage.UpdateState
S2  ✅ Task/ActivityInstance Storage + Manager 热点（Set/End/Cancel/Children）
S3  ✅ 预选 / Picked 表 Storage（分表）+ Mode Store 适配；宿主建表/挂载
S4  ✅ 聚合根评估决议 + Transition / HookOutbox / Variable(删) 下沉
S4+ ✅ 写/查收口：PI/Task/AI/Transition/Outbox/Variable（Router 回落留 Manager）
S5  ✅ Definition/XPDL 评估：维持 `IXPDLStorage` 独立（文件/XML，非 kit 首参族）
```

**S0–S5 锚点：**

| 材料 | 路径 |
|------|------|
| 注册槽 | `engine/src/Storage/SailStorageRegistry.cs` · `SailClient.Storages` |
| PI / Task / AI | `I*Storage` · `Default*Storage` · 写 + 列表/视图查询全下沉 |
| Transition | `ITransitionInstanceStorage` · Insert/Delete/查询 + 网关分支 Count/Join；Router 回落留 Manager |
| HookOutbox | `IHookOutboxStorage` · CAS + Pending/Dead/Purge/Bind 查询全下沉 |
| Variable | `IProcessVariableStorage` · Get/List/Insert/Update/Delete（分表名入参）；Router 回落留 Manager |
| 项集投影 | `AssigneeItemRow` |
| Preselect Storage | `IPreselectAssigneeStorage` · 宿主 `SleveenPreselectAssigneeStorage` → `sf_preselect_assignee` |
| Picked Storage | `IPickedAssigneeStorage` · 宿主 `SleveenPickedAssigneeStorage` → `sf_picked_assignee` |
| Mode 适配 | `SleveenPreselectAssigneeStore` / `SleveenPickedAssigneeStore`（无 kit 对外；内部 kit 首参） |
| 挂载 | `UseDefaultStorages`；宿主 `UseSleveenPreselectAssignees` + `UseSleveenPickedAssignees` |

**办理人项集表（与对象解析器对齐，已定）：** Preselect 仓与 Picked 仓**不同表**；覆盖写入为**物理删后插**（项集语义，非软删）。数据量大时 Storage 实现须**考虑分片**（如按 `ProcessInstanceId` 哈希）；首版单表 + `(process_instance_id, activity_id)` 索引。

---

## 8. S4 决议（已拍）

| # | 议题 | 决议 |
|---|------|------|
| 1 | 试点清单 / Definition | **运行时套表**：PI / Task / AI / Transition / HookOutbox / **Variable（已下沉）**。**Definition / XPDL 不并入**本 kit 族（见 S5）：`IXPDLStorage` 为文件/XML 读写，无 `SQLBuilder kit` 首参，经 `IProcessModelFactory` 创建，与 `SailStorages` 分离。 |
| 2 | 聚合根切分 | **Task ≠ ActivityInstance ≠ ProcessInstance**：分接口保留（热点列与查询面不同）。Transition、HookOutbox **各自独立接口**，不并入 Task/AI。 |
| 3 | 乐观并发 | **首版不引入版本列**；状态竞争用 **条件更新 / CAS**（`where Status=…`、`CasClaimPending` 等）。若日后加 `RowVersion`，仅落在对应 Storage 热点 API，不改 Manager 业务语义。 |
| 4 | `useRepo` 口径 | **仅允许在 Storage 实现内**使用 `useRepo` / `useClip`；Manager / Lane / Mode Store **对外不得泄漏** repo。Manager 未注册槽时允许回落 `new Default*Storage()`。 |
| 5 | Outbox 业务边界 | Attempt 递增、退避、Skip 原因截断留 **Manager**；Storage 提供 `Insert` / `Update` / `GetById` / `Cas*` / `DeleteById` **及纯查询**（`ListPendingDue` / `ListDead` / `ListStaleProcessing` 等），不含状态机策略。 |
| 6 | 热分表 | Router 解析物理表名留 **Manager**；Storage `Insert(..., physicalTable)` 只负责写指定表。 |

**下一批（可选，非阻塞）：** 其它运行时 Manager（如 Process 定义元数据表若有 DB 行）按需下沉；宿主/Lane 散落 SQL 审计。

---

## 9. 代码锚点（现状）

| 材料 | 路径 |
|------|------|
| 运行时套表 Manager | PI/Task/AI/Transition/HookOutbox/Variable：**无** `useClip`/`useRepo`（经 Storage） |
| Definition | `IXPDLStorage` / `XPDLLocalStorage`（文件侧；非 `SailStorages`） |
| 寿命 A/B | [专题-对象生命周期与复用边界.md](./专题-对象生命周期与复用边界.md) |
| UseXxx / ISailCollection | [业务管道特性设计.md](../业务管道特性设计.md) |
| 预选 Store | 宿主 Preselect/Picked Storage + Mode Store |
| **S0–S5 Storage** | PI/Task/AI/Transition/HookOutbox/Variable + Preselect/Picked · `UseDefaultStorages` / 宿主挂载 |
