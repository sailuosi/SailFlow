# 开发手册 · OA 命令执行链路（类 / 方法级）

> 状态：**现行实现对齐说明**（2026-11-06）  
> 用途：从宿主调度入口追到 DB 读写，固化主要办理动词的**类·方法**链路，方便排障与改命令。  
> 关联：[特性说明-中国式OA审批要素.md](../特性说明-中国式OA审批要素.md) · [特性说明-引擎能力基线.md](../特性说明-引擎能力基线.md) · [特性设计-回调与事件.md](../特性设计-回调与事件.md) · [草案-命令执行管道.md](../草案-命令执行管道.md) · [专题-对象生命周期与复用边界.md](./专题-对象生命周期与复用边界.md)（寿命档位讨论草案） · [架构设计-项目依赖与分层.md](../架构设计-项目依赖与分层.md)

---

## 1. 一句话

```text
Host API / ButtonsOperation
  → IWorkflowService.*Process[Async]
    → UseRunSQL + beginTransaction + InstanceExecutionLock
      → WfRuntimeManagerFactory.Create*
        → FlowCapabilityPipeline.TryGate
          → WfRuntimeManager.Execute → ExecuteInstanceImp
            → NodeMediator* / NodeSendBack* / TaskManager.*
              → *Manager（mooSQL useRepo / useClip）
```

多数推进类命令共用这一外壳；**Transfer / Terminate / Discard** 等旁路命令略短，见各节。

---

## 2. 分层与关键类型

| 层 | 落点 | 职责 |
|----|------|------|
| **Host 调度** | `sleveen-admin/backend/saiflow` | HTTP、码表按钮 → Handler、鉴权绑 Runner |
| **命令入口** | `WorkflowService`（`Service/WorkflowService.ProcessExecution.cs`） | 事务、实例锁、Gate/Hook、调 Factory |
| **运行时装配** | `WfRuntimeManagerFactory` | 查当前节点/任务/模型，构造具体 `WfRuntimeManager`* |
| **运行时执行** | `WfRuntimeManager`* | `Execute` → `ExecuteInstanceImp` |
| **节点调度** | `NodeMediatorFactory` / `NodeMediator`* | 办结当前项、继续下一跳、写 AI/Task/Transition |
| **业务 Manager** | `Business/Manager/`* | 实体 CRUD；经 `SQLBuilder` → mooSQL |
| **数据访问** | `SailClient.UseRunSQL()` · `SailSqlAccess.Sibling(kit)` · `useRepo` / `useClip` | 同一事务 kit 贯穿 |

### 2.1 事务 / 锁 / Gate 外壳（几乎所有动词）

同步公开 API 典型形态：

1. `Sail.UseRunSQL()` → `kit.beginTransaction()`
2. `InstanceExecutionLock.Acquire(ResolveInstanceLockKey(runner))`（`AppInstanceId` 优先，否则 `ProcessId`）
3. `ExecuteXxxCore(kit, runner)`：Factory → Gate → `runtime.Execute(kit)` → Hook
4. 仅当 `result.Status == Success` 时 `kit.commit()`

Async 动词多数走 `RunVerbAsync`（开事务 → `AcquireAsync` → 同步 Core → 成功提交）。  
`RunProcessAsync` / `StartProcessAsync` / `RunProcessAutoAsync` 另有专用路径（Factory 预载可 `*Async`）。

Gate / 成功后 Hook：

- `FlowCapabilityPipeline.BuildGateContext` + `TryGate`（Reject → 不 Execute、不 commit）
- 成功后 `BuildHookContext` + `AfterSuccess`（事件类型见各命令）

### 2.2 DB 访问约定

| API | 含义 |
|-----|------|
| `SailClient.UseRunSQL()` | 取得主库 `SQLBuilder`（命令入口事务根） |
| `SailSqlAccess.Sibling(kit)` | 同连接/事务上的兄弟 builder，供 Manager 查询/更新 |
| `kit.useRepo<TEntity, TKey>()` | 主键 CRUD（`GetById` / `InsertReturnId` / `Update`…） |
| `kit.useClip()` | 条件查询 / 批量更新 |

Manager 构造：`new XxxManager(Sail)`；读模型定义还经 `ProcessModelFactory.CreateBy*`（内部读流程定义表）。

---

## 3. Host 外层调度入口

仓：`sleveen-admin/backend/saiflow`

| 入口 | 类·方法 | 说明 |
|------|---------|------|
| 统一办理 | `Wf2XmlController.ButtonsOperation` → `SailOperationDispatcher.ExecuteAsync` → `ISailOperationHandler.ExecuteAsync` | 码表 `ButtonId` → Handler |
| 直调 API | `Wf2XmlController.StartProcess` / `RunProcess` / `SendBackProcess` / … | 直接调 `IWorkflowService.*Async` |
| 表单发起模板 | `SailFormBizHost`（Submit → `StartProcessAsync`） | 产品发起壳 |
| 超时/自动过 Job | `SailNodePolicyJobWorker` → `WorkflowService.RunProcess` | R10 锚点代办等 |

### 3.1 码表 Id → Handler → 引擎命令

| ButtonId | Handler | `IWorkflowService` |
|----------|---------|----------------------|
| `Approve` | `ApproveOperationHandler` | `RunProcessAsync` |
| `Reject` | `RejectOperationHandler` | `RejectProcessAsync` 或（策略）`SendBackProcessAsync` |
| `SendBack` | `SendBackOperationHandler` | `SendBackProcessAsync` |
| `Withdraw` | `WithdrawOperationHandler` | `WithdrawProcessAsync` |
| `Resend` | `ResendOperationHandler` | `ResendProcessAsync` |
| `SignForward` | `SignForwardOperationHandler` | `SignForwardProcessAsync` |
| `Transfer` | `TransferOperationHandler` | `TransferProcessAsync` |
| `Jump` | `JumpOperationHandler` | `JumpProcessAsync` |
| `Close` | `CloseOperationHandler` | `CloseProcessAsync` |
| `Revise` | `ReviseOperationHandler` | `ReviseProcessAsync` |
| `Reverse` | `ReverseOperationHandler` | `ReverseProcessAsync` |
| `Terminate` | `TerminateOperationHandler` | `TerminateProcess`（bool） |
| `Discard` | `DiscardOperationHandler` | `DiscardProcess`（bool） |

发起不在办理条：`StartProcess` / `StartProcessByMessage`（直调或 FormBiz）。

---

## 4. 共用推进深链（Run / Start 典型）

人工任务推进（`Approve` → `RunProcess`）在 Mediator 内的通用深链：

```text
WfRuntimeManagerRun.ExecuteInstanceImp
  → ActivityForwardContext.CreateRunningContextByTask|ByActivity
  → NodeMediatorFactory.CreateNodeMediator(...)
  → NodeMediator.ExecuteWorkItem
       （任务节点 → NodeMediatorTask）
         → OnBeforeExecuteWorkItem
         → CompleteWorkItem
              → TaskManager.Complete(Sibling(kit), taskId, runner)
              → ActivityInstanceManager.Complete(Sibling(kit), fromAiId, runner)
         → OnAfterExecuteWorkItem
         → ContinueForwardCurrentNode
              → ContinueForwardCurrentNodeRecurisivly
                   → 下一节点 CreateActivityTaskTransitionInstance /
                     Gateway / Service / End… 各 NodeMediator*
```

写下一跳待办时（`NodeMediatorTask.CreateActivityTaskTransitionInstance`）：

1. `CreateActivityInstanceObject` / `GenerateActivityAssignedUserInfo`（**Assignee 管道**）
2. `ActivityInstanceManager.Insert`
3. `CreateNewTask` → `TaskManager.Insert`
4. `InsertTransitionInstance` → `TransitionInstanceManager` 写转移
5. `FlowCapabilityPipeline.AfterSuccess(OnActivityCreated)`（可选）

实例办结时：`ProcessInstanceManager.Complete`；`WfRuntimeManagerRun.FireProcessCompletedHooks` 再发 `OnProcessCompleted`。

---

## 5. 各 OA 命令链路

下列路径省略 Host Handler（见 §3）。引擎类均在 `src/core/pure/engine`。

### 5.1 StartProcess（发起）

| 步骤 | 类·方法 |
|------|---------|
| API | `WorkflowService.StartProcess` → `Start()` → `Start(kit)` → `ExecuteStartCore` |
| Factory | `WfRuntimeManagerFactory.CreateRuntimeInstanceStartup(Sail, runner, kit, ref result)` |
| Factory 读库 | `ProcessInstanceManager.GetProcessInstanceCurrent`（查重 Running）· `ProcessModelFactory.CreateByProcess` |
| Gate | `FlowOperation.Start` |
| Runtime | `WfRuntimeManagerStartup.ExecuteInstanceImp` |
| 建实例对象 | `ProcessInstanceManager.CreateNewProcessInstanceObject`（内存实体，尚未 Insert） |
| Mediator | `ActivityForwardContext.CreateStartupContext` → `NodeMediatorFactory.CreateNodeMediator` → 通常 `NodeMediatorStart.ExecuteWorkItem` |
| Start 写库 | `ProcessInstanceManager.Insert` · `ActivityInstanceManager.Insert` + `Complete`（开始节点自动完）· `ContinueForwardCurrentNode` → 首任务 AI/Task/Transition |
| Hook | `OnProcessStarted` |

`StartProcessByMessage`：`StartByMessage` → `ProcessManager.GetByMessage` 填 `ProcessId/Version` → 再走同一 `ExecuteStartCore`。

### 5.2 RunProcess（Approve / 通过）

| 步骤 | 类·方法 |
|------|---------|
| API | `RunProcess` → `Run()` → `Run(kit)` → `ExecuteRunCore` → `FinishRunCore` |
| Factory | `CreateRuntimeInstanceAppRunning` |
| Factory 读库 | `ActivityInstanceManager.GetRunningNode` · `TaskManager.CheckTaskStateInRunningState` · `ProcessInstanceManager.GetById` · `ProcessModelFactory.CreateByTask` · `ProcessVariableResolver.CheckVariableRequired` |
| Gate | `FlowOperation.Run` |
| Runtime | `WfRuntimeManagerRun.ExecuteInstanceImp` → §4 深链；含代码说明见 **§6** |
| Hook | `OnProcessContinued`；若实例 Completed 另 `OnProcessCompleted` |

`RunProcessAuto`：`CreateRuntimeInstanceRunAuto`（按活动实例推进，无人工任务校验）→ 同 `WfRuntimeManagerRun`，**不走 Gate**（`FinishRunAutoCore` 直接 Execute）。

### 5.3 SendBackProcess（退回）

| 步骤 | 类·方法 |
|------|---------|
| API | `SendBackProcess` → `SendBack` → `ExecuteSendBackCore` |
| Factory | `CreateRuntimeInstanceSendBack` |
| Factory 读库 | `ActivityInstanceManager.GetRunningNode` · `IsMineTask` · `ProcessInstanceManager.GetById` · `ProcessModelFactory.CreateByProcessInstance` · `PreviousStepChecker.GetPreviousActivityList` |
| Gate | `FlowOperation.SendBack` |
| Runtime | `WfRuntimeManagerSendBack.ExecuteInstanceImp` |
| 退回执行 | `DistributeEachPerformerTask` → `NodeSendBackFactory.CreateNodeReverter` → `NodeSendBack*.Execute`（按普通/会签组合生成回退 AI+Task+Transition） |
| 收尾写库 | `SetRunningNodeSendBack` → `TaskManager.SendBack` · `ActivityInstanceManager.SendBack` |
| Hook | `OnProcessSendBacked` |

### 5.4 WithdrawProcess（撤回）

| 步骤 | 类·方法 |
|------|---------|
| API | `WithdrawProcess` → `Withdraw` → `ExecuteWithdrawCore` |
| Factory | `CreateRuntimeInstanceWithdraw`（**产出仍是** `WfRuntimeManagerSendBack`） |
| Factory 读库 | `TaskManager.GetTaskView`（已办）· `IsMine` · `TransitionInstanceManager.GetTargetActivityInstanceList`（下一跳运行节点）· 会签时 `GetActivityMulitipleInstanceWithStateBatch` · `GetTaskViewByActivity` · `PreviousStepChecker.GetPreviousActivityList` |
| 语义 | `SendBackOperation.BackwardType = Withdrawed`，`IsCancellingBrothersNode = true` |
| Gate | `FlowOperation.Withdraw` |
| Runtime / 写库 | 同 SendBack：`WfRuntimeManagerSendBack.ExecuteInstanceImp` |
| Hook | `OnProcessWithdrawn` |

### 5.5 RejectProcess（驳回）

| 步骤 | 类·方法 |
|------|---------|
| API | `RejectProcess` → `Reject` → `ExecuteRejectCore` |
| Factory | `CreateRuntimeInstanceReject` |
| Factory 读库 | `GetRunningNode` · `GetActivityInstanceLatest`（目标）· `ProcessInstanceManager.GetById` · `TransitionInstanceManager.GetLastTaskTransition` · `PreviousStepChecker.GetPreviousActivityInstanceList` |
| Gate | `FlowOperation.Reject` |
| Runtime | `WfRuntimeManagerReject.ExecuteInstanceImp` |
| 写库 | `TaskManager.Complete` · `ActivityInstanceManager.GetRunningActivityInstanceList` + `Cancel`（兄弟）· `NodeMediatorBackward.CreateBackwardActivityTaskTransitionInstance` · `ActivityInstanceManager.SendBack` |
| Hook | `OnProcessRejected` |

Host `RejectOperationHandler` 可能按节点策略改走 `SendBackProcessAsync`（驳回到指定 `returnNodeId`），以 Handler 为准。

### 5.6 ResendProcess（返送退回人）

| 步骤 | 类·方法 |
|------|---------|
| API | `ResendProcess` → `Resend` → `ExecuteResendCore` |
| Factory | `CreateRuntimeInstanceResend` |
| Factory 读库 | `TaskManager.GetTaskView` · `ActivityInstanceManager.GetById`（当前 + `BackSrcActivityInstanceId`）· `ProcessModelFactory.CreateByTask` |
| Gate | `FlowOperation.Resend` |
| Runtime | `WfRuntimeManagerResend.ExecuteInstanceImp` |
| Mediator | `CreateRunningContextByTask(..., isNotParsedByTransition: true)` → `NodeMediatorFactory` → `ExecuteWorkItem`（强制指向退回源活动） |
| Hook | `OnProcessResent` |

### 5.7 ReviseProcess（修订）

| 步骤 | 类·方法 |
|------|---------|
| API | `ReviseProcess` → `ExecuteReviseCore` |
| Factory | `CreateRuntimeInstanceRevise`（TaskView + Running AI + NextPerformers） |
| Runtime | `WfRuntimeManagerRevise.ExecuteInstanceImp` |
| Mediator | **固定** `new NodeMediatorRevise(...).ExecuteWorkItem`（不经 Factory 选型） |
| Hook | `OnProcessRevised` |

### 5.8 ReverseProcess（反转 / 已完成返签）

| 步骤 | 类·方法 |
|------|---------|
| API | `ReverseProcess` → `ExecuteReverseCore` |
| Factory | `CreateRuntimeInstanceReverse` |
| Factory 读库 | `ProcessInstanceManager.GetProcessInstanceLatest`（须 Completed）· `TransitionInstanceManager.GetEndTransition` · `ActivityInstanceManager.GetById` · `PreviousStepChecker.GetPreviousActivityInstanceList` |
| Gate | `FlowOperation.Reverse` |
| Runtime | `WfRuntimeManagerReverse.ExecuteInstanceImp` |
| 写库 | `ProcessInstanceManager.Reverse` · `NodeMediatorBackward.CreateBackwardActivityTaskTransitionInstance`（`BackwardType.Reversed`） |
| Hook | `OnProcessReversed` |

### 5.9 JumpProcess（跳转）

| 步骤 | 类·方法 |
|------|---------|
| API | `JumpProcess` → `ExecuteJumpCore` |
| Factory | `CreateRuntimeInstanceJump`（`NextActivityPerformers` 只能一步） |
| Factory 读库 | `GetRunningNode` · `ProcessModelFactory.CreateByTask` |
| Gate | `FlowOperation.Jump` |
| Runtime | `WfRuntimeManagerJump.ExecuteInstanceImp` |
| 写库 / Mediator | `ActivityInstanceManager.Complete`（当前）· `CreateJumpforwardContext` / `CreateRunningContextByTask` → `NodeMediatorFactory` → 若 `NodeMediatorEnd` 先 `CreateActivityTaskTransitionInstance` → `ExecuteWorkItem` |
| Hook | `OnProcessJumped` |

### 5.10 CloseProcess（关闭）

| 步骤 | 类·方法 |
|------|---------|
| API | `CloseProcess` → `ExecuteCloseCore` |
| Factory | `CreateRuntimeInstanceClose`（目标多为 End 活动） |
| Runtime | `WfRuntimeManagerClose.ExecuteInstanceImp` |
| 写库 / Mediator | 同 Jump：Complete 当前 → Jumpforward 上下文 → Mediator（常到 End）→ `ExecuteWorkItem` |
| Hook | `OnProcessClosed` |

### 5.11 SignForwardProcess（加签）

| 步骤 | 类·方法 |
|------|---------|
| API | `SignForwardProcess` → `ExecuteSignForwardCore` |
| Factory | `CreateRuntimeInstanceSignForward` |
| Factory 读库 | `GetRunningNode`（校验 AssignedUserIds）· `ProcessModelFactory.CreateByTask` |
| Gate | `FlowOperation.SignForward` |
| Runtime | `WfRuntimeManagerSignForward.ExecuteInstanceImp` |
| Mediator | `CreateRunningContextByTask` → `NodeMediatorFactory`（加签类型节点 Mediator）→ `ExecuteWorkItem`（生成前/后/并加签 AI+Task） |
| Hook | `OnProcessSignForwarded` |

### 5.12 TransferProcess（转办，不推进）

**不经** `WfRuntimeManager` / `NodeMediator`。

| 步骤 | 类·方法 |
|------|---------|
| API | `TransferProcess` → `Transfer` → `ExecuteTransferCore` |
| 读库 | `TaskManager.GetTaskView` · `ActivityInstanceManager.GetById` |
| Gate | `FlowOperation.Transfer` |
| 写库 | `TaskManager.Transfer`：原待办 `Closed` + `Update`；目标 `Insert` 同 Activity 新待办；`RefreshActivityAssignedUsersFromOpenTasks`；`CapabilityDispatcher.FireTaskLifecycle(Entrusted)` |
| Hook | `OnProcessTransferred` |

### 5.13 Terminate / Discard / Cancel / Suspend / Resume（短路径）

| 命令 | API | 核心 |
|------|-----|------|
| Terminate | `TerminateProcess` | `ProcessInstanceManager.Terminate` |
| Discard | `DiscardProcess` | `ProcessInstanceManager.Discard` |
| Cancel | `CancelProcess` | `ProcessInstanceManager.Cancel` |
| Suspend | `SuspendProcess` | `TaskManager.GetTask` → `ProcessInstanceManager.Suspend` |
| Resume | `ResumeProcess` | `ProcessInstanceManager.Resume` |

无 RuntimeManager / Gate 外壳（bool 返回）；产品码表默认关，Host Handler 包装。

---

## 6. 典型链详解 · Approve → RunProcess（含关键代码）

选 **办理条「通过」** 作为最常见推进链：覆盖事务外壳、Factory 预载、Gate、Mediator 办结、下一跳写库。其余动词（SendBack / Jump…）外壳同构，差别在 Factory 预装与 `ExecuteInstanceImp`。

### 6.1 总览

```text
POST ButtonsOperation (ButtonId=Approve)
  → SailOperationDispatcher.ExecuteAsync
    → ApproveOperationHandler.ExecuteAsync
      → WorkflowService.RunProcessAsync
        → UseRunSQL + beginTransaction + InstanceExecutionLock
          → CreateRuntimeInstanceAppRunning[Async]   ← 读 Task / AI / PI / 定义
          → TryGate(FlowOperation.Run)
          → WfRuntimeManager.Execute
              → WfRuntimeManagerRun.ExecuteInstanceImp
                  → NodeMediatorTask.ExecuteWorkItem
                      → TaskManager.Complete + ActivityInstanceManager.Complete
                      → ContinueForwardCurrentNode → 下一跳 Insert AI/Task/Transition
          → AfterSuccess(OnProcessContinued)
        → Success → kit.commit()
```

### 6.2 Host：码表 → Handler

`Wf2XmlController.ButtonsOperation` 把 `ButtonId` + `Runner` 交给调度器：

```csharp
// sleveen-admin/.../SailOperationDispatcher.cs
public async Task<WfExecutedResult> ExecuteAsync(SailOperationContext ctx, CancellationToken ct = default)
{
    var handler = Resolve(ctx.ButtonId);
    // ...
    var result = await handler.ExecuteAsync(ctx, ct);
    // 成功后可 NotifyAfterOperation（FormBiz）
}
```

```csharp
// ApproveOperationHandler.cs
public Task<WfExecutedResult> ExecuteAsync(SailOperationContext ctx, CancellationToken ct = default)
    => wf.RunProcessAsync(ctx.Runner, ct);
```

### 6.3 WorkflowService：事务 · 锁 · Core

```csharp
// WorkflowService.ProcessExecution.cs
public async Task<WfExecutedResult> RunProcessAsync(WfAppRunner runner, CancellationToken cancellationToken = default)
{
    _wfAppRunner = runner;
    using (var kit = Sail.UseRunSQL())
    {
        kit.beginTransaction();
        var result = await RunAsync(kit, cancellationToken); // 内含 AcquireAsync + ExecuteRunCoreAsync
        if (result.Status == WfExecutedStatus.Success)
            kit.commit();
        return result;
    }
}
```

`FinishRunCore` 是推进类命令的标准「Gate → Execute → Hook」模板：

```csharp
WfExecutedResult FinishRunCore(SQLBuilder kit, WfAppRunner runner,
    WfRuntimeManager runtimeInstance, WfExecutedResult runAppResult)
{
    if (runAppResult.Status == WfExecutedStatus.Exception)
        return runAppResult;

    var gateCtx = FlowCapabilityPipeline.BuildGateContext(
        Sail, kit, runner,
        runtimeInstance.ProcessInstanceId,
        runtimeInstance.RunningActivityInstance,
        runtimeInstance.TaskView?.Id,
        FlowOperation.Run);
    if (!FlowCapabilityPipeline.TryGate(Sail, kit, gateCtx, runAppResult))
        return runAppResult;   // Reject：不 Execute、上层不 commit

    runtimeInstance.Execute(kit);
    runAppResult = runtimeInstance.WfExecutedResult;
    if (runAppResult.Status == WfExecutedStatus.Success)
    {
        FlowCapabilityPipeline.AfterSuccess(
            Sail, kit,
            FlowCapabilityPipeline.BuildHookContext(/* … */, EventFireTypeEnum.OnProcessContinued, FlowOperation.Run),
            EventFireTypeEnum.OnProcessContinued);
    }
    return runAppResult;
}
```

### 6.4 Factory：预载当前任务与运行节点（读库）

```csharp
// WfRuntimeManagerFactory.CreateRuntimeInstanceAppRunning
var aim = new ActivityInstanceManager(client);
var runningNode = aim.GetRunningNode(SailSqlAccess.Sibling(kit), runner, out taskView);
// 校验 AssignedUserIds 含当前用户、Task 处于 Running
var processInstance = (new ProcessInstanceManager(client)).GetById(kitForPi, runningNode.ProcessInstanceId.Value);
var processModel = ProcessModelFactory.CreateByTask(client, SailSqlAccess.Sibling(kit), taskView);
// ProcessVariableResolver.CheckVariableRequired …
rmins.TaskView = taskView;
rmins.RunningActivityInstance = runningNode;
```

`GetRunningNode` 优先用 `runner.TaskId`，否则按 Running AI 列表反查本人待办：

```csharp
// ActivityInstanceManager.GetRunningNode
if (taskId != null && taskId.Value > 0)
{
    taskView = tm.GetTaskView(kit, taskId.Value);          // useRepo / Clip 读任务
    runningNode = GetById(kit, taskView.ActivityInstanceId.Value);
    return runningNode;
}
// else: GetRunningActivityInstanceList → GetTaskOfMineByActivityInstance
```

### 6.5 Runtime → Mediator：办结当前项

```csharp
// WfRuntimeManager.Execute → ExecuteInstanceImp
// WfRuntimeManagerRun.ExecuteInstanceImp
var runningExecutionContext = ActivityForwardContext.CreateRunningContextByTask(
    TaskView, ProcessModel, ActivityResource, false, kit, Sail);
NodeMediator mediator = NodeMediatorFactory.CreateNodeMediator(runningExecutionContext, kit, Sail);
mediator.LinkContext.FromActivityInstance = RunningActivityInstance;
mediator.ExecuteWorkItem(null);
```

人工任务节点落到 `NodeMediatorTask`：

```csharp
// NodeMediatorTask.ExecuteWorkItem
OnBeforeExecuteWorkItem();
bool canContinue = CompleteWorkItem(ActivityForwardContext.TaskId, ActivityForwardContext.ActivityResource, Kit);
OnAfterExecuteWorkItem();
if (canContinue)
    ContinueForwardCurrentNode(ActivityForwardContext.IsNotParsedByTransition, Kit);
```

```csharp
// NodeMediatorTask.CompleteWorkItem —— 本步写库（办结）
base.TaskManager.Complete(SailSqlAccess.Sibling(kit), taskId.Value, activityResource.AppRunner);
base.ActivityInstanceManager.Complete(SailSqlAccess.Sibling(kit), FromActivityInstance.Id.Value, activityResource.AppRunner);
// TaskState=Completed / ActivityState=Completed → useRepo.Update
```

```csharp
// TaskManager.Complete
EndTaskState(kit, taskId, TaskStateEnum.Completed, runner);
FireTaskLifecycle(kit, task, runner, TaskNotifyKind.Completed);  // Notifier 旁路
```

### 6.6 ContinueForward：解析下一跳并落库

```csharp
// NodeMediator.ContinueForwardCurrentNode（按图）
var nextActivityMatchedResult = ProcessModel.GetNextActivityTreeListRuntime(
    FromActivityInstance.ActivityId, /* … */, kit);
ContinueForwardCurrentNodeRecurisivly(/* root = next tree */);
// 对每个后继任务节点 → CreateActivityTaskTransitionInstance
```

```csharp
// NodeMediatorTask.CreateActivityTaskTransitionInstance（普通任务）
var toActivityInstance = CreateActivityInstanceObject(toActivity, processInstance, runner);
toActivityInstance = GenerateActivityAssignedUserInfo(toActivityInstance, activityResource); // Assignee 管道
ActivityInstanceManager.Insert(Sibling(kit), toActivityInstance);
    // → Sibling(kit).useRepo<ActivityInstanceEntity, int>().InsertReturnId(entity)
CreateNewTask(toActivityInstance, activityResource, kit);
    // → TaskManager.Insert(Sibling(kit), …)
InsertTransitionInstance(/* … */);  // TransitionInstanceManager
FlowCapabilityPipeline.AfterSuccess(/* OnActivityCreated */);
```

若整单到达 End 并 Completed：`WfRuntimeManagerRun.FireProcessCompletedHooks` 再发 `OnProcessCompleted`。

### 6.7 读 / 写对照（本链）

| 阶段 | 主要读 | 主要写 |
|------|--------|--------|
| Factory | TaskView、Running AI、ProcessInstance、Process 定义、变量校验 | — |
| CompleteWorkItem | （可选 Notifier 再读 AI） | Task Completed、AI Completed |
| CreateActivityTaskTransitionInstance | Assignee 解析可能查人 | 新 AI Insert、新 Task Insert、Transition Insert |
| Hook / Notifier | — | HookOutbox / 通知（按模式） |

同步 `RunProcess` 路径相同，只是 `Run()` + `Acquire` 替代 `RunProcessAsync` + `AcquireAsync`；Mediator 深链始终同步。

---

## 7. 命令 → Runtime / Mediator 速查

| OA / 引擎命令 | Factory | Runtime 类 | 调度核 |
|---------------|---------|------------|--------|
| Start | `CreateRuntimeInstanceStartup` | `WfRuntimeManagerStartup` | `NodeMediatorStart` + Continue |
| Run / Approve | `CreateRuntimeInstanceAppRunning` | `WfRuntimeManagerRun` | `NodeMediatorTask` 等 |
| RunAuto | `CreateRuntimeInstanceRunAuto` | `WfRuntimeManagerRun` | 同上 |
| SendBack | `CreateRuntimeInstanceSendBack` | `WfRuntimeManagerSendBack` | `NodeSendBack*` |
| Withdraw | `CreateRuntimeInstanceWithdraw` | **同** `WfRuntimeManagerSendBack` | `NodeSendBack`*（Withdrawed） |
| Reject | `CreateRuntimeInstanceReject` | `WfRuntimeManagerReject` | `NodeMediatorBackward` |
| Resend | `CreateRuntimeInstanceResend` | `WfRuntimeManagerResend` | `NodeMediator*`（指向 BackSrc） |
| Revise | `CreateRuntimeInstanceRevise` | `WfRuntimeManagerRevise` | `NodeMediatorRevise` |
| Reverse | `CreateRuntimeInstanceReverse` | `WfRuntimeManagerReverse` | `NodeMediatorBackward` |
| Jump | `CreateRuntimeInstanceJump` | `WfRuntimeManagerJump` | `NodeMediator*`（跳转目标） |
| Close | `CreateRuntimeInstanceClose` | `WfRuntimeManagerClose` | 常 `NodeMediatorEnd` |
| SignForward | `CreateRuntimeInstanceSignForward` | `WfRuntimeManagerSignForward` | 加签 Mediator |
| Transfer | （无） | （无） | `TaskManager.Transfer` |

---

## 8. Manager ↔ 典型表操作

| Manager | 高频方法 | 用途 |
|---------|----------|------|
| `ProcessInstanceManager` | `GetProcessInstanceCurrent` / `GetById` / `Insert` / `Complete` / `Reverse` / `Terminate` / `Discard` / `Cancel` / `Suspend` / `Resume` | 实例生命周期 |
| `ActivityInstanceManager` | `GetRunningNode` / `GetById` / `Insert` / `Complete` / `SendBack` / `Cancel` / `GetRunningActivityInstanceList` | 活动实例 |
| `TaskManager` | `GetTaskView` / `Insert` / `Complete` / `SendBack` / `Transfer` / `CheckTaskStateInRunningState` | 待办/已办 |
| `TransitionInstanceManager` | `Insert`（经 Mediator）/ `GetLastTaskTransition` / `GetTargetActivityInstanceList` / `GetEndTransition` | 转移轨迹 |
| `ProcessManager` | `GetById` / `GetByMessage` | 流程定义 |
| `PreviousStepChecker` | `GetPreviousActivityList` / `GetPreviousActivityInstanceList` | 退回/驳回/反转算上一跳 |
| `HookOutboxManager` | `InsertPending` | Hook/Notifier 旁路入队（同事务或另开） |

实体物理表名以 basis/Entity 映射为准（如流程实例、活动实例、任务、转移）；读写一律经上述 Manager，勿在命令层直接拼 SQL。

---

## 9. 排障抓手

1. **卡在 Factory**：看 `WfExecutedResult.ExceptionType`（如 `RunApp_HasNoTask`、`Sendback_NotMineTask`）——多数是预检读库失败，尚未进 Mediator。
2. **卡在 Gate**：`TryGate` Reject，事务不提交；查宿主注册的 Gate。
3. **卡在 Mediator / Assignee**：`WfAssigneeException` → `ApplyAssigneeException`（`ReasonCode`）；或下一跳 Insert 失败。
4. **Withdraw vs SendBack**：同一 Runtime，差在 Factory 预装的 `BackwardType` / 下一跳定位（已办 TaskId → 目标运行节点）。
5. **Transfer 未流转**：预期行为；只改 Task + AssignedUser*，无 `ContinueForward`。
6. **Async**：`*Async` 可取消等锁；Mediator `ExecuteWorkItem` 仍同步（见异步化草案）。

---

## 10. 源码索引（相对 `src/core/pure/engine`）

| 区域 | 路径 |
|------|------|
| 命令入口 | `src/Service/WorkflowService.ProcessExecution.cs` · `IWorkflowService.cs` |
| Factory | `src/Core/Runtime/WfRuntimeManagerFactory.cs` |
| Runtime 基类 / 各动词 | `src/Core/Runtime/WfRuntimeManager*.cs` |
| Mediator | `src/Core/Pattern/NodeMediator*.cs` · `NodeMediatorFactory` |
| Manager | `src/Business/Manager/*.cs` |
| Gate/Hook | `src/Hooks/FlowCapabilityPipeline` / `FlowCapabilityDispatcher` |
| Host | `sleveen-admin/backend/saiflow/src/Controller/Wf2XmlController.Runtime.cs` · `Operations/` |

---

## 修订记录

| 日期 | 说明 |
|------|------|
| 2026-11-06 | 初稿：外层调度 → WorkflowService → Factory → Runtime → Mediator/Manager → DB；覆盖主要 OA 动词 |
| 2026-11-06 | 增 §6：Approve→RunProcess 典型链（Host→Gate→Mediator→DB 关键代码） |
