# 开发手册 · OA 命令执行链路（类 / 方法级）

> 状态：**现行实现对齐说明**（2026-11-06；**D3 + D4-a/b**）  
> 用途：从宿主调度入口追到 DB 读写，固化主要办理动词的**类·方法**链路，方便排障与改命令。  
> **读法**：装配经 `SailClient` 四槽（`RuntimeFactory` / `MediatorFactory` / `ProcessModelFactory` / `ManagerFactory`）；Runtime/Mediator 内部为 `RuntimeInstanceCreator` / `MediatorInstanceCreator`；Manager 经 `ManagerFactory`；Service **无状态**（不扣 Runner）。寿命立法见 [专题-对象生命周期与复用边界.md](../theme/专题-对象生命周期与复用边界.md)。  
> 关联：[特性说明-中国式OA审批要素.md](../特性说明-中国式OA审批要素.md) · [特性说明-引擎能力基线.md](../特性说明-引擎能力基线.md) · [特性设计-回调与事件.md](../特性设计-回调与事件.md) · [专题-流转动作骨架.md](../theme/专题-流转动作骨架.md)（已落地） · [草案-命令执行管道.md](../done/草案-命令执行管道.md)（已归档吸收） · [专题-对象生命周期与复用边界.md](../theme/专题-对象生命周期与复用边界.md) · [架构设计-项目依赖与分层.md](../架构设计-项目依赖与分层.md)  
> D3 前残旧样例（`_wfAppRunner` / `new *Manager` / `NodeSendBackFactory`）已迁 [../done/残旧口径清出-OA手册与基线-2026-11-06.md](../done/残旧口径清出-OA手册与基线-2026-11-06.md)。

---

## 1. 一句话

```text
Host API / ButtonsOperation
  → IWorkflowService.*Process[Async]
    → UseRunSQL + beginTransaction + InstanceExecutionLock
      → Sail.RuntimeFactory.Create* / Runtimes.Create*
        → FlowCapabilityPipeline.TryGate
          → IWfRuntime.Execute → ExecuteInstanceImp
            → INodeMediator* / INodeSendBack* / TaskManager.*
              → *Manager（mooSQL useRepo / useClip）
```

多数推进类命令共用这一外壳；**Transfer / Terminate / Discard** 等旁路命令略短，见各节。

---

## 2. 分层与关键类型

| 层 | 落点 | 职责 |
|----|------|------|
| **Host 调度** | `sleveen-admin/backend/__KEEP_sailflow__` | HTTP、码表按钮 → Handler、鉴权绑 Runner |
| **命令入口** | `WorkflowService`（`Service/WorkflowService.ProcessExecution.cs`） | 事务、实例锁、Gate/Hook、调 Factory |
| **运行时装配** | `DefaultRuntimeFactory` → `RuntimeInstanceCreator` | 查当前节点/任务/模型，构造具体 `WfRuntimeManager`* |
| **运行时执行** | `WfRuntimeManager`* | `Execute` → `ExecuteInstanceImp` |
| **节点调度** | `DefaultMediatorFactory` → `MediatorInstanceCreator` / `NodeMediator`* | 办结当前项、继续下一跳、写 AI/Task/Transition |
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

Manager 经 `Sail.ManagerFactory` / `SailFactoryAccess.Managers`；模型经 `Sail.ProcessModelFactory.CreateBy*`。

---

## 3. Host 外层调度入口

仓：`sleveen-admin/backend/__KEEP_sailflow__`

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
| `Reject` | `RejectOperationHandler` | 注入 `RejectMode` 后 **只** `RejectProcessAsync`（不再转调 SendBack） |
| `SendBack` | `SendBackOperationHandler` | `SendBackProcessAsync` |
| `Withdraw` | `WithdrawOperationHandler` | `WithdrawProcessAsync` |
| `Resend` | `ResendOperationHandler` | `ResendProcessAsync` |
| `SignForward` | `SignForwardOperationHandler` | `SignForwardProcessAsync` |
| `Transfer` | `TransferOperationHandler` | `TransferProcessAsync` |
| `Jump` | `JumpOperationHandler` | `JumpProcessAsync` |
| `Close` | `CloseOperationHandler` | `CloseProcessAsync` |
| `Revise` | `ReviseOperationHandler` | `ReviseProcessAsync` |
| `Reverse` | `ReverseOperationHandler` | `ReverseProcessAsync` |
| `Terminate` | `TerminateOperationHandler` | `TerminateProcess`（Lifecycle / `WfExecutedResult`） |
| `Discard` | `DiscardOperationHandler` | `DiscardProcess`（Lifecycle / `WfExecutedResult`） |

发起不在办理条：`StartProcess` / `StartProcessByMessage`（直调或 FormBiz）。

---

## 4. 共用推进深链（Run / Start 典型）

人工任务推进（`Approve` → `RunProcess`）在 Mediator 内的通用深链：

```text
WfRuntimeManagerRun.ExecuteInstanceImp
  → ActivityForwardContext.CreateRunningContextByTask|ByActivity
  → Sail.MediatorFactory.CreateNodeMediator(...)
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

1. `CreateActivityInstanceObject` / `GenerateActivitySignInfo`（**Assignee 管道**）
2. `ActivityInstanceManager.Insert`
3. `CreateNewTask` → `TaskManager.Insert`
4. `InsertTransitionInstance` → `TransitionInstanceManager` 写转移
5. `FlowCapabilityPipeline.AfterSuccess(OnActivityCreated)`（可选）

实例办结时：`ProcessInstanceManager.Complete`；`WfRuntimeManagerRun.FireProcessCompletedHooks` 再发 `OnProcessCompleted`。

---

## 5. 各 OA 命令链路

下列路径省略 Host Handler（见 §3）。引擎类均在 `src/core/pure/engine`。

### 5.1 StartProcess（发起）

Transition 族 `StartAction`（A3 续）。

| 步骤 | 类·方法 |
|------|---------|
| API | `StartProcess` → `RunAction` → `StartAction.Execute`；`Start(kit,…)` 同 |
| 族群时序 | Load Runtime → Gate（补 Start 活动码）→ Execute → `OnProcessStarted`（PI 取 `ProcessInstanceIdStarted`） |
| Factory | `CreateRuntimeInstanceStartup` |
| Factory 读库 | `ProcessInstanceManager.GetProcessInstanceCurrent`（查重 Running）· `client.ProcessModelFactory.CreateByProcess` |
| Gate | `FlowOperation.Start` |
| Runtime | `WfRuntimeManagerStartup.ExecuteInstanceImp` |
| 建实例对象 | `ProcessInstanceManager.CreateNewProcessInstanceObject`（内存实体，尚未 Insert） |
| Mediator | `ActivityForwardContext.CreateStartupContext` → `Sail.MediatorFactory.CreateNodeMediator` → 通常 `NodeMediatorStart.ExecuteWorkItem` |
| Start 写库 | `ProcessInstanceManager.Insert` · `ActivityInstanceManager.Insert` + `Complete`（开始节点自动完）· `ContinueForwardCurrentNode` → 首任务 AI/Task/Transition |
| Hook | `OnProcessStarted` |

`StartProcessByMessage`：`BindMessageStartup`（`GetByMessage` 填定义 + 系统用户）→ 同一 `StartAction`。

### 5.2 RunProcess（Approve / 通过）

Transition 族 `RunAction`（A3 续）。

| 步骤 | 类·方法 |
|------|---------|
| API | `RunProcess` → `RunAction` → `RunAction.Execute`；`RunProcessAsync` → Factory 预载 + `ExecuteWithRuntime` |
| 族群时序 | Load Runtime → Gate → `Runtime.Execute` → `OnProcessContinued` |
| Factory | `CreateRuntimeInstanceAppRunning`（同步）/ `CreateRuntimeInstanceAppRunningAsync`（异步） |
| Factory 读库 | `ActivityInstanceManager.GetRunningNode` · `TaskManager.CheckTaskStateInRunningState` · `ProcessInstanceManager.GetById` · `client.ProcessModelFactory.CreateByTask` · `ProcessVariableResolver.CheckVariableRequired` |
| Gate | `FlowOperation.Run` |
| Runtime | `WfRuntimeManagerRun.ExecuteInstanceImp` → §4 深链；含代码说明见 **§6** |
| Hook | `OnProcessContinued`；若实例 Completed 另 `OnProcessCompleted` |

`RunProcessAuto`：Transition 族 `RunAutoAction`（`SkipGate=true`；仍 `FlowOperation.Run` + `OnProcessContinued`）；Async 走 Factory 预载 + `ExecuteWithRuntime`。

### 5.3 SendBackProcess（退回）

A3 起走 Transition 动作骨架（`SendBackAction`）。

| 步骤 | 类·方法 |
|------|---------|
| API | `SendBackProcess` → `RunAction` → `SendBackAction.Execute`（`SendBack(kit,…)` 同） |
| 族群时序 | `TransitionActionBase`：Load Runtime → Gate → `Runtime.Execute` → `OnProcessSendBacked` |
| Factory | `CreateRuntimeInstanceSendBack`（在 `SendBackAction.TryLoadContext`） |
| Factory 读库 | `ActivityInstanceManager.GetRunningNode` · `IsMineTask` · `ProcessInstanceManager.GetById` · `client.ProcessModelFactory.CreateByProcessInstance` · `PreviousStepChecker.GetPreviousActivityList` |
| Gate | `FlowOperation.SendBack` |
| Runtime | `WfRuntimeManagerSendBack.ExecuteInstanceImp` |
| 退回执行 | `DistributeEachPerformerTask` → `CreateNodeSendBack` → `NodeSendBack*.Execute` |
| 收尾写库 | `SetRunningNodeSendBack` → `TaskManager.SendBack` · `ActivityInstanceManager.SendBack` |
| Hook | `OnProcessSendBacked`（族群 `OnFireFlowHook`；路径内 TaskNotifier 不在此重复） |
| 设计 | [专题-流转动作骨架.md](../theme/专题-流转动作骨架.md) · [progress/2026-11-15-动作骨架-A3-SendBack.md](../progress/2026-11-15-动作骨架-A3-SendBack.md) · [OA/专题-退回.md](../OA/专题-退回.md) |

### 5.4 WithdrawProcess（撤回）

与 SendBack 同属 Transition 族（`WithdrawAction`）。

| 步骤 | 类·方法 |
|------|---------|
| API | `WithdrawProcess` → `RunAction` → `WithdrawAction.Execute` |
| 族群时序 | 同 SendBack：Load → Gate → Execute → `OnProcessWithdrawn` |
| Factory | `CreateRuntimeInstanceWithdraw`（**产出仍是** `WfRuntimeManagerSendBack`） |
| Factory 读库 | `TaskManager.GetTaskView`（已办）· `IsMine` · `TransitionInstanceManager.GetTargetActivityInstanceList` · 会签批次 · `PreviousStepChecker.GetPreviousActivityList` |
| 语义 | `SendBackOperation.BackwardType = Withdrawed`，`IsCancellingBrothersNode = true` |
| Gate | `FlowOperation.Withdraw` |
| Runtime / 写库 | 同 SendBack：`WfRuntimeManagerSendBack.ExecuteInstanceImp` |
| Hook | `OnProcessWithdrawn`（族群 `OnFireFlowHook`） |

### 5.5 RejectProcess（驳回）

Transition 族 `RejectAction`：按控制袋 `RejectMode` 分发（默认 `Previous`）。

| 步骤 | 类·方法 |
|------|---------|
| API | `RejectProcess` → `RunAction` → `RejectAction.Execute` |
| 控制袋 | `ControlParameterSheet.RejectMode` · `RejectReturnActivityId`（仅 Specified） |
| 解析 | `RejectTargetResolver` → 唯一 `activityId` + 历史 `PerformerList`，**覆盖**客户端 `NextActivityPerformers` |
| Terminate | 委托 `TerminateAction`；成功事件 **OnProcessTerminated**；`ReasonCode=RejectModeTerminate`，Message 带 `RejectMode=Terminate` |
| 回跳三态 | Previous / Starter / Specified → Factory `CreateRuntimeInstanceReject` |
| Factory 读库 | `GetRunningNode` · `GetActivityInstanceLatest`（目标）· `ProcessInstanceManager.GetById` · `PreviousStepChecker`（Previous）· 首活动/指定活动办结人 |
| Gate | `FlowOperation.Reject`（Terminate 路径与 `TerminateAction` Gate 约定对齐，避免双套横切漏挂） |
| Runtime | `WfRuntimeManagerReject.ExecuteInstanceImp`（回跳） |
| 写库 | `TaskManager.Complete` · 兄弟 `Cancel` · `NodeMediatorBackward` · `ActivityInstanceManager.SendBack`（回跳写库复用，**无** BackSrc/Resend 语义） |
| Hook | 回跳：`OnProcessRejected`；Terminate 模式：Terminate 成功事件 |
| ReasonCode | `RejectModeInvalid` · `RejectTargetMissing` · `RejectModeTerminate` |

Host：`RejectOperationHandler` 读 NodePolicy → `SailRejectModeMapper.ApplyToRunner` → **只**调 `RejectProcessAsync`（Specified 亦不再转调 `SendBack`）。

| Host `rejectHandlerType` | `RejectMode` |
|--------------------------|--------------|
| `1` | `Terminate`（旧文案「结束流程」纠偏；旧实现曾 `Fill(Startup)` 回发起，属 breaking） |
| `2` + `returnNodeId` | `Specified` |
| `3`（默认） | `Previous` |
| `4` | `Starter` |

**红线：** 驳回 ≠ 退回两维；不开放 History 自选；ApprovalOrSplit 拒绝边仍是网关分流，不是本枚举。

### 5.6 ResendProcess（返送退回人）

Transition 族 `ResendAction`。

| 步骤 | 类·方法 |
|------|---------|
| API | `ResendProcess` → `RunAction` → `ResendAction.Execute` |
| Factory | `CreateRuntimeInstanceResend` |
| Factory 读库 | `TaskManager.GetTaskView` · `ActivityInstanceManager.GetById`（当前 + `BackSrcActivityInstanceId`）· `client.ProcessModelFactory.CreateByTask` |
| Gate | `FlowOperation.Resend` |
| Runtime | `WfRuntimeManagerResend.ExecuteInstanceImp` |
| Mediator | `CreateRunningContextByTask(..., isNotParsedByTransition: true)` → `MediatorInstanceCreator` → `ExecuteWorkItem`（强制指向退回源活动） |
| Hook | `OnProcessResent` |

### 5.7 ReviseProcess（修订）

Transition 族 `ReviseAction`（Factory 已 `Success` 则直接返回，不跑 Gate/Execute）。

| 步骤 | 类·方法 |
|------|---------|
| API | `ReviseProcess` → `RunAction` → `ReviseAction.Execute` |
| Factory | `CreateRuntimeInstanceRevise`（TaskView + Running AI + NextPerformers） |
| Runtime | `WfRuntimeManagerRevise.ExecuteInstanceImp` |
| Mediator | **固定** `new NodeMediatorRevise(...).ExecuteWorkItem`（不经 Factory 选型） |
| Hook | `OnProcessRevised` |

### 5.8 ReverseProcess（反转 / 已完成返签）

Transition 族 `ReverseAction`。

| 步骤 | 类·方法 |
|------|---------|
| API | `ReverseProcess` → `RunAction` → `ReverseAction.Execute` |
| Factory | `CreateRuntimeInstanceReverse` |
| Factory 读库 | `ProcessInstanceManager.GetProcessInstanceLatest`（须 Completed）· `TransitionInstanceManager.GetEndTransition` · `ActivityInstanceManager.GetById` · `PreviousStepChecker.GetPreviousActivityInstanceList` |
| Gate | `FlowOperation.Reverse` |
| Runtime | `WfRuntimeManagerReverse.ExecuteInstanceImp` |
| 写库 | `ProcessInstanceManager.Reverse` · `NodeMediatorBackward.CreateBackwardActivityTaskTransitionInstance`（`BackwardType.Reversed`） |
| Hook | `OnProcessReversed` |

### 5.9 JumpProcess（跳转）

Transition 族 `JumpAction`（`TransitionNextPerformers.Fill(jumpOption)`）。

| 步骤 | 类·方法 |
|------|---------|
| API | `JumpProcess` → `RunAction` → `JumpAction.Execute` |
| Factory | `CreateRuntimeInstanceJump`（`NextActivityPerformers` 只能一步） |
| Factory 读库 | `GetRunningNode` · `client.ProcessModelFactory.CreateByTask` |
| Gate | `FlowOperation.Jump` |
| Runtime | `WfRuntimeManagerJump.ExecuteInstanceImp` |
| 写库 / Mediator | `ActivityInstanceManager.Complete`（当前）· `CreateJumpforwardContext` / `CreateRunningContextByTask` → `MediatorInstanceCreator` → 若 `NodeMediatorEnd` 先 `CreateActivityTaskTransitionInstance` → `ExecuteWorkItem` |
| Hook | `OnProcessJumped` |

### 5.10 CloseProcess（关闭）

Transition 族 `CloseAction`（`Fill(End)`）。

| 步骤 | 类·方法 |
|------|---------|
| API | `CloseProcess` → `RunAction` → `CloseAction.Execute` |
| Factory | `CreateRuntimeInstanceClose`（目标多为 End 活动） |
| Runtime | `WfRuntimeManagerClose.ExecuteInstanceImp` |
| 写库 / Mediator | 同 Jump：Complete 当前 → Jumpforward 上下文 → Mediator（常到 End）→ `ExecuteWorkItem` |
| Hook | `OnProcessClosed` |

### 5.11 SignForwardProcess（加签）

Transition 族 `SignForwardAction`。产品细表：[OA/专题-改签.md](../OA/专题-改签.md)。  
席位级子堆栈（**引擎已改**）：[theme/专题-节点实例临时子规则.md](../theme/专题-节点实例临时子规则.md)。

| 步骤 | 类·方法 |
|------|---------|
| API | `SignForwardProcess` → `RunAction` → `SignForwardAction.Execute` |
| Factory | `CreateRuntimeInstanceSignForward` |
| Factory 读库 | `GetRunningNode` + `TaskManager.IsMine(taskView)`（办理权以 Task 为准）· `client.ProcessModelFactory.CreateByTask` |
| Gate | `FlowOperation.SignForward` |
| Runtime | `WfRuntimeManagerSignForward.ExecuteInstanceImp` |
| Mediator（压栈） | `NodeMediatorSignForward`：发起人 AI 抬席/升级为容器（直属 Task + 加签人子 AI）；**不改**节点 Leave；子规则写容器 |
| Mediator（办结） | `NodeMediatorMultiSignForward`：同级 seats `TryCanLeave`；Passed 折合一票上浮；仅节点 Passed 才 ContinueForward；Race 可淹没子树 |
| Hook | `OnProcessSignForwarded` |

### 5.12 TransferProcess（转办，不推进）

**不经** `WfRuntimeManager` / `NodeMediator`。A1 起走动作骨架（Assignee 族）。产品细表：[OA/专题-转办.md](../OA/专题-转办.md)。

| 步骤 | 类·方法 |
|------|---------|
| API | `TransferProcess` → `RunAction`（事务+锁）→ `TransferAction.Execute` |
| 族群时序 | `AssigneeActionBase`：Load → `TryGate` → `ExecuteCore` → `OnProcessTransferred` |
| 读库 | `TaskManager.GetTaskView` · `ActivityInstanceManager.GetById`（在 `TransferAction.TryLoadContext`） |
| Gate | `FlowOperation.Transfer` |
| 写库 | `TaskManager.Transfer`：原待办 `Closed` + `Update`；目标 `Insert` 同 Activity 新待办；`RefreshActivitySignFromOpenTasks`；`CapabilityDispatcher.FireTaskLifecycle(Entrusted)`（仍在 Manager 内；族内不重复） |
| Hook | `OnProcessTransferred`（族群 `OnFireFlowHook`） |
| 设计 | [专题-流转动作骨架.md](../theme/专题-流转动作骨架.md) · [progress/2026-11-15-动作骨架-A1-Transfer.md](../progress/2026-11-15-动作骨架-A1-Transfer.md) |

### 5.12b RemoveAssigneeProcess（减签，不推进）

Assignee 族 `RemoveAssigneeAction`（A4；**Host 办理条已接线**：码表 + Handler + RemovableAssignees）。产品细表：[OA/专题-改签.md](../OA/专题-改签.md)。

| 步骤 | 类·方法 |
|------|---------|
| API | `RemoveAssigneeProcess` → `RunAction` → `RemoveAssigneeAction.Execute` |
| 定位 | `TaskId` → 活动；`NextActivityPerformers` = 待移除人 |
| Gate | `FlowOperation.RemoveAssignee` |
| 写库 | `TaskManager.RemoveAssignees`：匹配未办待办 → `Closed` + `Cancelled`；刷新 SignIds/Names；禁减至 0 |
| Hook | `OnAssigneeRemoved` |
| 设计 | [progress/2026-11-15-动作骨架-A4-RemoveAssignee.md](../progress/2026-11-15-动作骨架-A4-RemoveAssignee.md) |

### 5.12c ChangeAssigneeProcess（改派，不推进）

Assignee 族 `ChangeAssigneeAction`（A4 续）。

| 步骤 | 类·方法 |
|------|---------|
| API | `ChangeAssigneeProcess` → `RunAction` → `ChangeAssigneeAction.Execute` |
| 定位 | `TaskId` → 活动；`NextActivityPerformers` = **新办理人全集** |
| Gate | `FlowOperation.ChangeAssignee` |
| 写库 | `TaskManager.ChangeAssignees`：不在新集合 → Closed+Cancelled；缺开待办 → Insert+Created；刷新 SignIds/Names |
| Hook | `OnAssigneeChanged` |

### 5.12d UrgeProcess（催办，不推进）

Assignee 族 `UrgeAction`（A4 续）。**不改** TaskState。

| 步骤 | 类·方法 |
|------|---------|
| API | `UrgeProcess` → `RunAction` → `UrgeAction.Execute` |
| 定位 | `TaskId` → 活动；可选 `NextActivityPerformers` 限定对象（空=全部打开待办） |
| Gate | `FlowOperation.Urge` |
| 通知 | `TaskManager.UrgeOpenTasks` → 逐条 `FireTaskLifecycle(Urged)` → `OnTaskUrged` + Notifier |
| Hook | `OnProcessUrged` |
| Host | 需注册 `TaskKinds` 含 `Urged` 的 Notifier 通道后才有实际消息 |

### 5.12e DelegateProcess（委派，办完自动回转）

Assignee 族 `DelegateAction`。**拍板：** 受托人办结必须回转原人；否则用 Transfer。**Host 已接线**（码表默认关 + `DelegateOperationHandler` + 办理条）。

| 步骤 | 类·方法 |
|------|---------|
| API | `DelegateProcess` → `RunAction` → `DelegateAction.Execute` |
| Host | `ButtonsOperation` → `DelegateOperationHandler`；直调 `POST …/DelegateProcess` |
| 发起 | `TaskManager.Delegate`：原人 Closed + `Entrusted`；受托人新建（`EntrustedTaskId`=原 TaskId） |
| 办结 | 受托人 `Approve/Run` → `TryCompleteAsDelegationReturn`：受托 Completed + 原人重建 Waiting；**不**完结 AI、**不** ContinueForward |
| Hook | `OnProcessDelegated`（发起时） |
| Notifier | Portal/Debug `TaskKinds` 含 `Entrusted` |

### 5.12f Consult / Acknowledge / RecallConsult（沟通等待协议）

Assignee 族三动词。字段：`tk_assignee_role`（0/null 常规，1 参与者）、`tk_consult_src_task_id`。产品细表：[OA/专题-沟通.md](../OA/专题-沟通.md)。

| 步骤 | 类·方法 |
|------|---------|
| 沟通 API | `ConsultProcess` → `ConsultAction` → `TaskManager.Consult` |
| 阅办 API | `AcknowledgeProcess` → `AcknowledgeAction` → `TaskManager.Acknowledge` |
| 收回 API | `RecallConsultProcess` → `RecallConsultAction` → `TaskManager.RecallConsult` |
| 硬锁 | `ConsultPendingGuard`：源 Task 有打开参与者时拦截 Run/Reject/SendBack/SignForward/Transfer/Delegate/… |
| SignIds/Names | `RefreshActivitySignFromOpenTasks` **排除**参与者 |
| 节点离开 | `CompleteActivityInstance` → `CancelOpenParticipantsOnActivity` |
| Hook | `OnProcessConsulted` / `OnTaskAcknowledged` / `OnConsultRecalled` |
| Host | 码表 `Consult`/`Acknowledge`/`RecallConsult`；Workbench 下发 `AssigneeRole`/`ConsultPending` |

### 5.13 Suspend / Resume（Lifecycle 族，A2）

**不经** Runtime/Mediator。走动作骨架。

| 步骤 | 类·方法 |
|------|---------|
| API | `SuspendProcess(runner)` / `ResumeProcess(runner)` → `RunAction` → `SuspendAction` / `ResumeAction` |
| 定位 | Suspend：`TaskId`；Resume：`ProcessInstanceId` |
| Gate | `FlowOperation.Suspend` / `Resume` |
| 写库 | `ProcessInstanceManager.Suspend/Resume(kit, …)` |
| Hook | `OnProcessSuspended` / `OnProcessResumed` |
| 兼容 | 原 `bool SuspendProcess(taskId,…)` / `bool ResumeProcess(processInstanceId,…)` 仍可用，内部转 Action |

### 5.14 Terminate / Discard / Cancel（Lifecycle 族，A2 续）

**不经** Runtime/Mediator。走动作骨架；产品码表默认仍关。

| 步骤 | 类·方法 |
|------|---------|
| API | `*Process` → `RunAction` → `TerminateAction` / `DiscardAction` / `CancelAction` |
| 定位 | Cancel/Discard：`BizPK+ProcessId(+Version)`；Terminate：`ProcessInstanceId` 或同上 |
| Gate | `FlowOperation.Terminate` / `Discard` / `Cancel` |
| 写库 | `ProcessInstanceManager.*(kit, …)`；Terminate 级联收口活动/待办 + 逐条 `FireTaskLifecycle(Cancelled)` |
| Hook | `OnProcessTerminated` / `OnProcessDiscarded` / `OnProcessCanceled` |

### 5.15 Notifier / Hook 对账（防双响）

命令级 AfterSuccess **只**在动作骨架各族 `OnFireFlowHook`；路径内 `FireTaskLifecycle`（Created/Completed/Cancelled/Entrusted）与节点级 Hook **保留**。全表见 [progress/2026-11-15-动作骨架-A3-SendBack.md](../progress/2026-11-15-动作骨架-A3-SendBack.md)「Notifier / Hook 对账表」。产品细表：[OA/专题-待办推送.md](../OA/专题-待办推送.md)。

### 5.16 抄送专用钩子（≠ 待办 Notifier）

| 项 | 说明 |
|----|------|
| 写入 | `SailCopytoService.CreateMany` → 每行后 `FireCopytoLifecycle` |
| FlowHook | `EventFireType.OnCopytoCreated`（可 `AddHook` + Events） |
| 旁路 | `AddCopytoNotifier`；须配 `CopytoKinds`（如 `Created`）；Outbox Kind=`CopytoNotify` |
| 禁忌 | 禁止用 `AddNotifier` / `TaskNotifyKind` 冒充抄送 |

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
// WorkflowService.ProcessExecution.cs（H · 无状态；不扣 runner 字段）
public async Task<WfExecutedResult> RunProcessAsync(FlowCommandContext runner, CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();
    // CommitOnSuccessAsync：UseRunSQL + beginTransaction + AcquireAsync → 成功才 commit
    return await CommitOnSuccessAsync(kit => RunAsync(kit, runner, cancellationToken));
}
```

`FinishRunCore` 是推进类命令的标准「Gate → Execute → Hook」模板：

```csharp
WfExecutedResult FinishRunCore(SQLBuilder kit, FlowCommandContext runner,
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
// RuntimeInstanceCreator.CreateRuntimeInstanceAppRunning（经 ManagerFactory / ProcessInstances）
var aim = _sail.ManagerFactory.Activities;
var runningNode = aim.GetRunningNode(SailSqlAccess.Sibling(kit), runner, out taskView);
// 办理权：TaskManager.IsMine(taskView, runner.UserId)；再 CheckTaskStateInRunningState
var processInstance = _sail.ProcessInstances.GetById(kitForPi, runningNode.ProcessInstanceId.Value);
var processModel = _sail.ProcessModelFactory.CreateByTask(SailSqlAccess.Sibling(kit), taskView);
// ProcessVariableResolver.CheckVariableRequired …
rmins.TaskView = taskView;
rmins.RunningActivityInstance = runningNode;
```

`GetRunningNode` 优先用 `runner.TaskId`；否则按 Running AI 列表用 **本人打开待办** 选型（勿用活动 `SignIds`）：

```csharp
// ActivityInstanceManager.GetRunningNode
if (!string.IsNullOrWhiteSpace(taskId))
{
    taskView = tm.GetTaskView(kit, taskId);
    runningNode = GetById(kit, taskView.ActivityInstanceId);
    return runningNode;
}
// else: GetRunningActivityInstanceList
//   单 AI → GetTaskOfMineByActivityInstance
//   多 AI → 逐个 GetTaskOfMineByActivityInstance，命中则选用该 AI（禁止 SignIds == userId）
```

### 6.5 Runtime → Mediator：办结当前项

```csharp
// WfRuntimeManager.Execute → ExecuteInstanceImp
// WfRuntimeManagerRun.ExecuteInstanceImp
var runningExecutionContext = ActivityForwardContext.CreateRunningContextByTask(
    TaskView, ProcessModel, ActivityResource, false, kit, Sail);
INodeMediator mediator = Sail.MediatorFactory.CreateNodeMediator(runningExecutionContext, kit);
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
toActivityInstance = GenerateActivitySignInfo(toActivityInstance, activityResource); // Assignee 管道
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
| SendBack | `CreateRuntimeInstanceSendBack` | `SendBackAction` / `WfRuntimeManagerSendBack` | `NodeSendBack*` |
| Withdraw | `CreateRuntimeInstanceWithdraw` | `WithdrawAction` / **同** `WfRuntimeManagerSendBack` | `NodeSendBack`*（Withdrawed） |
| Reject | `CreateRuntimeInstanceReject` | `RejectAction` / `WfRuntimeManagerReject` | `NodeMediatorBackward` |
| Resend | `CreateRuntimeInstanceResend` | `WfRuntimeManagerResend` | `NodeMediator*`（指向 BackSrc） |
| Revise | `CreateRuntimeInstanceRevise` | `WfRuntimeManagerRevise` | `NodeMediatorRevise` |
| Reverse | `CreateRuntimeInstanceReverse` | `WfRuntimeManagerReverse` | `NodeMediatorBackward` |
| Jump | `CreateRuntimeInstanceJump` | `JumpAction` / `WfRuntimeManagerJump` | `NodeMediator*`（跳转目标） |
| Close | `CreateRuntimeInstanceClose` | `CloseAction` / `WfRuntimeManagerClose` | 常 `NodeMediatorEnd` |
| SignForward | `CreateRuntimeInstanceSignForward` | `WfRuntimeManagerSignForward` | 加签 Mediator |
| Transfer | （无 Runtime） | `TransferAction` / `AssigneeActionBase` | `TaskManager.Transfer` |

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
5. **Transfer 未流转**：预期行为；只改 Task + SignIds/Names，无 `ContinueForward`。
6. **Async**：`*Async` 可取消等锁；Mediator `ExecuteWorkItem` 仍同步（见异步化草案）。

---

## 10. 源码索引（相对 `src/core/pure/engine`）

| 区域 | 路径 |
|------|------|
| 命令入口 | `src/Service/WorkflowService.ProcessExecution.cs` · `IWorkflowService.cs` |
| Factory | `src/Core/Runtime/RuntimeInstanceCreator.cs` |
| Runtime 基类 / 各动词 | `src/Core/Runtime/WfRuntimeManager*.cs` |
| Mediator | `src/Core/Pattern/NodeMediator*.cs` · `MediatorInstanceCreator` |
| Manager | `src/Business/Manager/*.cs` |
| Gate/Hook | `src/Hooks/FlowCapabilityPipeline` / `FlowCapabilityDispatcher` |
| Host | `sleveen-admin/backend/__KEEP_sailflow__/src/Controller/Wf2XmlController.Runtime.cs` · `Operations/` |

---

## 修订记录

| 日期 | 说明 |
|------|------|
| 2026-11-06 | 初稿：外层调度 → WorkflowService → Factory → Runtime → Mediator/Manager → DB；覆盖主要 OA 动词 |
| 2026-11-06 | 增 §6：Approve→RunProcess 典型链（Host→Gate→Mediator→DB 关键代码） |
| 2026-11-06 | **D3+D4 对齐**：去 `_wfAppRunner` / `new *Manager` / `NodeSendBackFactory` 样例；残旧迁 done |
| 2026-11-06 | §5.1 / §6.5 Mediator 调用改 `SailFactoryAccess.Mediator`；模型入口改为 `Sail.ProcessModelFactory` |
