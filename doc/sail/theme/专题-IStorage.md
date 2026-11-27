# 专题 · IStorage（数据库连接器）

> 状态：**P0–P2 已收口**（含 Preselect / Picked；2026-11-27）  
> 已收口：运行时六件套 + 定义四件 + Log/Archive + Preselect/Picked；`UseDefaultStorages` 十二件套；XPDL 独立。  
> 余债：宿主策略 Job/Hook / Query **散落读**（§8.2 审计清单；**不默认全迁**）。  
> 用途：流程定义 / 实例套表的**纯库交互层**——无状态、`kit` 首参、热点专属 API、唯一注册可覆盖；为实施提供可执行契约。  
> 讨论溯源：[讨论-办理人操作模式与人员范围正交-分析归档.md](../done/讨论-办理人操作模式与人员范围正交-分析归档.md)（§6、§8 已定寿命）  
> 关联：[专题-对象生命周期与复用边界.md](./专题-对象生命周期与复用边界.md) · [架构设计-项目依赖与分层.md](../架构设计-项目依赖与分层.md) · [业务管道特性设计.md](../业务管道特性设计.md) · [专题-事务一致性.md](./专题-事务一致性.md) · [专题-对象解析器.md](./专题-对象解析器.md)

---

## 0. 结论先行

| 项 | 口径 |
|----|------|
| **一句话** | `IStorage` = 纯数据库交互；CRUD + 查询 DTO；**不含业务逻辑** |
| **事务** | **`SQLBuilder kit` 为第一参数**；连接器不持 kit/连接字段 |
| **状态 / 寿命** | **无状态**；**同寿于一个 `SailClient` 注册槽**（已拍；非跨 Client 进程上帝单例） |
| **热点** | 状态更新等建**专属方法**；禁止只靠厚仓储泛化 |
| **默认实现** | **`sailflow.data`**（`Default*Storage`）；可继承；注册替换 |
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

## 4. 与现行差距（2026-11-27）

| 现行 | 目标 / 备注 |
|------|-------------|
| PI / Task / AI / Transition / HookOutbox / ProcessVariable Manager | **已收口**：仅 `Storages.GetRequired`；无 `useRepo` |
| `ProcessManager` / `VariableManager` / `RuleSetManager` / `JobInfoManager` | **已收口** → `IProcessDefinitionStorage` / `IVariableDefinitionStorage` / `IRuleSetStorage` / `IJobInfoStorage` |
| `IPreselectAssigneeStorage` | **已挂**（宿主 Storage + Mode Store） |
| `IPickedAssigneeStorage` | **已挂**（宿主 `sf_picked_assignee`；`persistPick=true` 时读写） |
| `DefaultManagerFactory` 懒持 Manager（A） | Storage 同为 A；已挂默认六件套；余 Manager 按需注入 |

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

## 7. 实施切片

```text
S0  ✅ 注册槽约定 + kit 首参 + 覆盖日志（SailStorageRegistry / useStorage / UseDefaultStorages）
S1  ✅ ProcessInstanceManager：GetById + Complete/Suspend/Resume/Reverse/RecallSubProcess → Storage.UpdateState
S2  ✅ Task/ActivityInstance Storage + Manager 热点（Set/End/Cancel/Children）
S3  ✅ Preselect / Picked：宿主 Storage + Mode Store 均已挂（`persistPick` 门控）
S4  ✅ 聚合根评估决议 + Transition / HookOutbox / ProcessVariable 下沉
S4+ ✅ 写/查收口：PI/Task/AI/Transition/Outbox/ProcessVariable（Router 回落留 Manager）
S5  ✅ Definition/XPDL 评估：维持 `IXPDLStorage` 独立（文件/XML，非 kit 首参族）
```

**注意：** `ProcessVariableManager`（实例变量，已收口）≠ `VariableManager`（定义表 `sf_variable`，P1 已收口）。

**S0–S5 锚点：**

| 材料 | 路径 / 现状 |
|------|-------------|
| 注册槽 | `engine/src/Storage/SailStorageRegistry.cs` · `SailClient.Storages` |
| 运行时六件套接口 | `IProcessInstanceStorage` / `ITaskStorage` / `IActivityInstanceStorage` / `ITransitionInstanceStorage` / `IHookOutboxStorage` / `IProcessVariableStorage` |
| 默认实现（`sailflow.data`） | `Default*Storage` · `UseDefaultStorages`（NS：`sailflow.data.Storage`） |
| 项集投影 | `AssigneeItemRow` |
| Preselect Storage | `IPreselectAssigneeStorage` · 宿主 `SleveenPreselectAssigneeStorage` → `sf_preselect_assignee`（**已挂**） |
| Picked Storage | `IPickedAssigneeStorage` · 宿主 `SleveenPickedAssigneeStorage` → `sf_picked_assignee`（**已挂**） |
| Mode Store | `SleveenPreselectAssigneeStore` / `SleveenPickedAssigneeStore` → `Storages.GetRequired` |
| 挂载 | **`UseSailData` / `UseDefaultStorages`**；宿主 `UseSleveenPreselectAssignees` + `UseSleveenPickedAssignees` |

**办理人项集表（与对象解析器对齐，已定）：** Preselect 仓与 Picked 仓**不同表**；覆盖写入为**物理删后插**（项集语义，非软删）。数据量大时 Storage 实现须**考虑分片**（如按 `ProcessInstanceId` 哈希）；首版单表 + `(process_instance_id, activity_id)` 索引。

---

## 8. S4 决议（已拍）

| # | 议题 | 决议 |
|---|------|------|
| 1 | 试点清单 / Definition | **运行时套表**：PI / Task / AI / Transition / HookOutbox / **Variable（已下沉）**。**Definition / XPDL 不并入**本 kit 族（见 S5）：`IXPDLStorage` 为文件/XML 读写，无 `SQLBuilder kit` 首参，经 `IProcessModelFactory` 创建，与 `SailStorages` 分离。 |
| 2 | 聚合根切分 | **Task ≠ ActivityInstance ≠ ProcessInstance**：分接口保留（热点列与查询面不同）。Transition、HookOutbox **各自独立接口**，不并入 Task/AI。 |
| 3 | 乐观并发 | **首版不引入版本列**；状态竞争用 **条件更新 / CAS**（`where Status=…`、`CasClaimPending` 等）。若日后加 `RowVersion`，仅落在对应 Storage 热点 API，不改 Manager 业务语义。 |
| 4 | `useRepo` 口径 | **仅允许在 Storage 实现内**使用 `useRepo` / `useClip`；Manager / Lane / Mode Store **对外不得泄漏** repo。默认实现在 **`sailflow.data`**；Manager **只**经 `Storages.GetRequired` 取实现（**禁止** runtime → data；未挂 `UseSailData`/`UseDefaultStorages` 则硬失败）。 |
| 5 | Outbox 业务边界 | Attempt 递增、退避、Skip 原因截断留 **Manager**；Storage 提供 `Insert` / `Update` / `GetById` / `Cas*` / `DeleteById` **及纯查询**（`ListPendingDue` / `ListDead` / `ListStaleProcessing` 等），不含状态机策略。 |
| 6 | 热分表 | Router 解析物理表名留 **Manager**；Storage `Insert(..., physicalTable)` 只负责写指定表。 |

### 8.1 P0–P2（**2026-11-27 已收口**）

#### P0 — Preselect / Picked Storage

| 项 | 现状 |
|----|------|
| 引擎接口 | `IPreselectAssigneeStorage` / `IPickedAssigneeStorage` 已有 |
| 宿主 Preselect | **已挂**：`SleveenPreselectAssigneeStorage` + Store；`UseSleveenPreselectAssignees` |
| 宿主 Picked | **已挂**：`SleveenPickedAssigneeStorage` + Store；表 `sf_picked_assignee` + `IX_sf_picked_pi_act`；`UseSleveenPickedAssignees`（管道仅 `persistPick=true` 读写；未挂则仍 Noop） |

#### P1 — 定义侧 Manager

| Manager | Storage | 备注 |
|---------|---------|------|
| `ProcessManager` | `IProcessDefinitionStorage` | `ProcessEntity` SQL 下沉；XML/XPDL 编排仍在 Manager |
| `VariableManager` | `IVariableDefinitionStorage` | ≠ `IProcessVariableStorage`（实例变量） |
| `RuleSetManager` | `IRuleSetStorage` | |
| `JobInfoManager` | `IJobInfoStorage` | |

#### P2 — Log / Archive

| 组件 | Storage | 备注 |
|------|---------|------|
| `TableEngineLogSink` | `IEngineLogStorage` | 分表物理名仍由 Sink 解析 |
| `ProcessArchiveService` / `ProcessArchiveQuery` | `IProcessArchiveStorage` | 冷表候选/读写；热表删读写复用运行时 Storage |
| `ArchiveRowCopier` | 兼容门面 → `DefaultProcessArchiveStorage` | 新代码优先 `IProcessArchiveStorage` |

**明确不做 / 已决议：** Definition **文件/XML** 不并入 kit 族（`IXPDLStorage`）；热分表 Router 留 Manager；首版无 RowVersion。

### 8.2 宿主散落 SQL 审计（sleveen-admin · 2026-11-27）

> 范围：`backend/sailflow/src` 内 `useRepo` / `useClip` / `UseRunSQL`。  
> 原则：**产品表 / 报表 / 种子 / Schema 可留宿主**；**引擎热路径套表**已走 Storage；策略 Hook/Job 的套表读写为**可选下沉**，不默认全迁。

| 档 | 含义 |
|----|------|
| **A · 已合规** | 经 IStorage / Mode Store，或纯 Schema/种子 |
| **B · 产品域 · 保留** | 宿主产品表 / 门户 / 报表 / API 编排；**不迁**引擎 Storage |
| **C · 可选下沉** | 直接打 PI/Task/AI 等套表；有复用价值时可改走 `Storages.GetRequired`，非阻塞 |

#### A · 已合规

| 位置 | 说明 |
|------|------|
| `Assignee/Sleveen*Preselect*Storage|Store` | → `IPreselectAssigneeStorage` |
| `Assignee/Sleveen*Picked*Storage|Store` | → `IPickedAssigneeStorage` |
| `Hosting/SailHostSchemaInitializer` | 建表/索引（含 `sf_picked_assignee`） |
| `Hosting/SailProductSeed` | 种子写定义/表单/分类 |

#### B · 产品域 · 保留（摘录）

| 位置 | 表/职责 |
|------|---------|
| `Service/SailCategoryService` | `SailProcessCategoryEntity` |
| `Service/SailProcessMetaService` | `SailProcessMetaEntity` |
| `Service/SailLeaveService` / `SailApprovalPhraseService` / `SailSurrogateService` | 请假 / 常用语 / 代理产品表 |
| `Service/SailProcessExtraService` + `Hooks/SailProcessExtra*` | Extra JSON / HTTP / 去重 / 创建触发 |
| `Service/SailReleaseService` | 发布编排 |
| `Service/SailReportService` · `Integration/.../SailTodoPortalRecordService` | 报表 / 门户读模型 |
| `UnitScope/SailProcessAuthStore` | 流程授权 |
| `Controller/WfFormController` · `WfOutboxController` · `Wf2XmlController*` | API / 设计器查询 |
| `Assignee/SurrogateAssigneeFilter` | 代理过滤读 |

#### C · 可选下沉（策略 / 超时打戳）

| 位置 | 触达套表 | 建议 |
|------|----------|------|
| `Timeout/SailNodePolicyTimeoutStampHook` | Task / AI / PI `useRepo`·`useClip` | 可改 `ITaskStorage` / `IActivityInstanceStorage` / `IProcessInstanceStorage` |
| `Timeout/SailNodePolicyJobWorker` | 同上 + 到期扫描 | 扫描可留 Job；写状态优先 Storage |
| `Assignee/SailNodePolicyAutoApproveHook` | Task / AI / PI | 读 PI 取 starter；写任务可走 Storage |
| `Hooks/SailProcessExtraStartedHook` | PI GetById | 可 `IProcessInstanceStorage.GetById` |
| `Hooks/SailEndTextLocalizeHook` | 局部读 | 低优先级 |
| `Controller/Wf2XmlController.Runtime` | 运行时 clip 查询 | 查询面可留 Controller；热点写勿新增散落 |

**决议：** C 档**不默认全迁**；新代码写套表须走 Storage；改旧 Hook 时顺手下沉即可。

---

## 9. 代码锚点（现状）

| 材料 | 路径 |
|------|------|
| 运行时套表 Manager | PI/Task/AI/Transition/HookOutbox/**ProcessVariable**：**无** `useClip`/`useRepo` |
| 定义侧 Manager | Process / Variable 定义 / RuleSet / JobInfo：**已**经 Storage（**P1**） |
| Log / Archive | `IEngineLogStorage` / `IProcessArchiveStorage`（**P2**） |
| Definition 文件 | `IXPDLStorage` / `XPDLLocalStorage`（非 `SailStorages`） |
| 寿命 A/B | [专题-对象生命周期与复用边界.md](./专题-对象生命周期与复用边界.md) |
| UseXxx / ISailCollection | [业务管道特性设计.md](../业务管道特性设计.md) |
| Preselect / Picked | 宿主 `Sleveen*AssigneeStorage|Store` + `UseSleveen*Assignees`（**P0**） |
| 默认十二件套 | 接口 `engine/Storage`；实现 `ext/data/Storage`（`sailflow.data`） |
| 宿主散落审计 | 本专题 §8.2 |

---

## 修订记录

| 日期 | 说明 |
|------|------|
| 2026-11-27 | 盘点回写：纠正 S3 过标；展开 P0–P2；状态改为「运行时已收口 · 余债实施中」 |
| 2026-11-27 | **P0 Preselect**：宿主 Storage + Mode Store 适配落地；Picked 标明接口预留 |
| 2026-11-27 | **P1**：Process/Variable 定义/RuleSet/Job → Storage；`UseDefaultStorages` 扩至十件套 |
| 2026-11-27 | **P2**：`IEngineLogStorage` / `IProcessArchiveStorage`；热删复用运行时 Storage；十二件套 |
| 2026-11-27 | **P0 Picked**：`sf_picked_assignee` + Storage/Store/`UseSleveenPickedAssignees`；§8.2 宿主散落 SQL 审计 |
