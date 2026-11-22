# 专题 · Bug 治理：Sync 钩子同事务自锁

> 状态：**案例库**（与治理决策配套；总纲见 [专题-事务一致性.md](./专题-事务一致性.md)；决策见 [专题-钩子治理与同事务可观测.md](./专题-钩子治理与同事务可观测.md)）  
> 用途：收录「主事务未提交 + 另开连接读写引擎库」导致的自锁 / 闷锅案例，供排障对照与代码评审检查。  
> 触发：2026-11-21 发起流程卡死（`SailProcessExtraStartedHook`）。  
> 接口警示：见 `IFlowHook` / `IProcessInstanceCreateHook` / `FlowHookContext.Kit` / `HookMode.Sync` XML 注释。

---

## 0. 一句话

**Sync 钩子与发起/办理共用同一数据库事务时，再 `Sail.UseRunSQL()`（或无 kit 的 Service/Manager）去读刚写入、尚未提交的行 → 第二连接等锁，主事务等钩子 → 自锁闷锅。**

表现常为：调试停在钩子某一行无响应；强制停止后下次 `beginTransaction` 报 SQLite Error 5/6，待办查询也挂。

---

## 1. 故障指纹

| 信号 | 含义 |
|------|------|
| `beginTransaction` 已成功，之后无返回 | 卡在 Sync 钩子或后续同事务逻辑 |
| 堆栈停在宿主 `IFlowHook.Handle` / `OnCreating` | 高概率钩子内同步 IO/DB |
| SQLite `Error 5 database is locked` / `Error 6 table is locked` | 多连接争用；Shared Cache 下只读亦可能被写锁毒化 |
| SQL Server 命令超时、无 1205 | 更像锁等待，不一定是死锁图 |
| 强制终止调试后「所有查询出不来」 | 写事务未释放；重启进程 / 检查残留连接 |

**与「死锁 1205」的区别**：多为**同请求内两连接互相等待**，不一定出现引擎检测到的 deadlock victim。

---

## 2. 案例登记

### BUG-2026-1121-A · ExtraStarted 无 kit 写变量

| 项 | 内容 |
|----|------|
| **症状** | 发起可开事务，卡在 `SailProcessExtraStartedHook`；无响应 |
| **根因** | Sync + `ctx.Kit` 存在，却调用 `WorkflowService.GetProcessVariable*` / `SaveProcessVariable`（内部 `UseRunSQL`） |
| **修复** | 变量 API 增加 kit 重载；钩子改走 `ctx.Kit` |
| **仓库** | sleveen-admin 宿主钩子 + sailflow `ProcessVariableManager` / `WorkflowService` |
| **状态** | 已修 |

### BUG-2026-1121-B · 选人 / 相对人读未提交实例

| 项 | 内容 |
|----|------|
| **症状** | 发起超时或锁等待（尤其 ProcessRelative=Starter、空 Version） |
| **根因** | `BpmnAssigneeConfigReader` / `ProcessRelativeAssigneeResolver` 等 `UseRunSQL` 读刚插入 PI；`SurrogateAssigneeFilter` / `SameAsStarterFilter` 同类 |
| **修复** | 传入 `AssigneeInvokeRequest.Kit`；宿主 Filter 复用 kit |
| **状态** | 已修 |

### BUG-2026-1121-C · 子流程 Execute 换连接

| 项 | 内容 |
|----|------|
| **症状** | 含子流程节点的发起/推进锁等待 |
| **根因** | 父事务已插入，`subKit = Sail.UseRunSQL()` 再 `Execute` |
| **修复** | `subKit = kit.useSQL()` |
| **状态** | 已修 |

---

## 3. 根因模型

```text
Conn1 (流转 kit)          Conn2 (UseRunSQL / 无 kit API)
  beginTransaction
  INSERT PI / Task / Var …
  RunSyncHooks / Assignee
       │                        SELECT/UPDATE 未提交行
       │◄──── 等待 Conn1 提交 ────┤
       │ 等待钩子/解析返回
       ▼
     自锁 / busy_timeout / 命令超时
```

调度侧加重因素（见治理专题）：有 `kit` 时 `SyncHookTimeoutMs` **被旁路**，且无 BEGIN/END 进度日志时极难定位「卡在哪个钩子」。

---

## 4. 排障步骤（现场）

1. **看日志**：若已落地进度日志，找最后一条 `[sail.sync-hook] BEGIN` 且无对应 `END` → 即嫌疑钩子。  
2. **未落地时**：调试 Call Stack 停在哪个 `IFlowHook` / `OnCreating`；对该方法全文搜 `UseRunSQL`、`new WorkflowService`、无 kit 的 Get/Save。  
3. **确认是否同库第二连接**：主数据跨库一般安全；引擎库同库必危。  
4. **强制终止后**：重启 API 进程再测；勿在未释放锁时连点发起。  
5. **修复验证**：同场景发起应秒级返回；不再卡在原钩子。

---

## 5. 代码评审检查单（新 Sync 钩子）

- [ ] 注册为 `HookMode.Sync` 是否必要？可延后 → `AsyncOutbox`。  
- [ ] 凡读/写 `sf_*` 流程表：是否使用 `ctx.Kit`（或 Create 钩子的 `ctx.Kit`）？  
- [ ] 是否调用无 kit 的 `WorkflowService` / Manager 重载？有则改为 kit 重载或直接用 kit。  
- [ ] 是否存在空 `catch { }`？至少打日志。  
- [ ] Sync 内是否同步 HTTP/长等？应改为 Outbox。  
- [ ] 读过 `IFlowHook` / `FlowHookContext.Kit` 上的死锁警示注释。

---

## 6. 修复模式（推荐）

```csharp
// ❌ Sync 钩子内
var wf = new WorkflowService(sail);
wf.SaveProcessVariable(entity);           // 内部 UseRunSQL
sail.UseRunSQL().findRowById<ProcessInstanceEntity>(piId);

// ✅
var kit = ctx.Kit ?? sail.UseRunSQL();    // 无 kit 时才独立连接（如 Outbox 回放）
var db = kit.useSQL();
wf.SaveProcessVariable(db, entity);       // kit 重载
db.findRowById<ProcessInstanceEntity>(piId);
```

引擎侧持续补齐 kit 重载；宿主侧以检查单约束，**不做**运行时强制禁连（见治理专题 D3）。

---

## 7. 关联文档与代码

| 类型 | 链接 |
|------|------|
| 同事务总纲 | [专题-事务一致性.md](./专题-事务一致性.md) |
| 治理决策（进度日志 / 软超时） | [专题-钩子治理与同事务可观测.md](./专题-钩子治理与同事务可观测.md) |
| 事件体系教程 | [教程-事件体系Gate-Hook-Notifier-Outbox.md](../../helps/教程-事件体系Gate-Hook-Notifier-Outbox.md) |
| 接口警示 | `IFlowHook`、`IProcessInstanceCreateHook`、`FlowHookContext.Kit`、`HookMode.Sync`、`AssigneeInvokeRequest.Kit` |

---

## 8. 变更记录

| 日期 | 说明 |
|------|------|
| 2026-11-21 | 初建：收录 1121 三案 + 排障/评审单；配合接口 XML 强烈警示 |
