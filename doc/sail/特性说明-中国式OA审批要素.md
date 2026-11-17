# 中国式 OA 审批要素（办理动词）

> 状态：**现行对照说明**（2026-11-01）  
> 用途：用「场景 → Sail 编码 → 执行含义」固化中国式办公审批里常见办理动词；指导按钮码表、引擎命令与产品文案，避免和 AntFlow/芋道名词一一硬套。  
> 权威码表（已入办理条）：宿主 `SailButtonsCatalog` / `buttonsCatalog.ts` · [宿主 sailflow-buttons-capability.md](../../../sleveen-admin/doc/sailflow-buttons-capability.md)  
> 关联：[特性说明-引擎能力基线.md](./特性说明-引擎能力基线.md) · [特性说明-待办人管道.md](./特性说明-待办人管道.md) · [存疑参考功能点.md](./talk/存疑参考功能点.md) · [todo-产品缺口.md](./dev/todo-产品缺口.md) · [专题-流转动作骨架.md](./theme/专题-流转动作骨架.md)（动词扩展配方，已落地）

**编码约定**

| 标记 | 含义 |
|------|------|
| **码表 Id** | 已进 `sf:ButtonsSetting` / 办理条（PascalCase） |
| **候选 Id** | 中国式 OA 常见、Sail **尚未**入码表；本文给出建议编码，**禁止空壳上线** |
| **引擎 API** | `IWorkflowService` / Host 真实入口；无 API 不得冒充按钮 |
| **模式码** | 非 UX 按钮：Assignee Leave Type（`sf:leaveJudge`） |

---

## 1. 一句话

中国式 OA 审批 = **发起之后，围绕「当前待办 / 已办 / 管理」的一组办理动词**，再叠 **多人如何离开节点（办理模式）** 与 **退回后再怎么走（Disagree 策略）**。  
承办、转办、加批等是典型「动词要素」；会签/或签/抢办是「模式要素」——二者正交，勿用一个万能按钮吞掉 Leave。

---

## 2. 已入 Sail 码表的办理动词

| 场景 / 常用名 | Sail 编码 | 引擎 / Host | 执行含义（产品语义） |
|---------------|-----------|-------------|----------------------|
| 通过 / 同意 / 审批通过 | `Approve` | `RunProcess` | 当前待办人办结本任务并按图推进；受 Leave（抢办/会签等）约束 |
| 驳回 / 拒绝 | `Reject` | `RejectProcess` | **极简**：办理人不选目标/不选人；设计期 `RejectMode`=Terminate\|Previous（默认）\|Starter\|Specified；人取历史办结人。**≠** 作废（`Discard`）；**≠** 退回两维；ApprovalOrSplit 拒绝边仍是网关分流 |
| 退回 / 回退 / 打回 | `SendBack` | `SendBackProcess` | 运行时可选目标（Prev/Starter/History）× 再发送（`sendBackTarget` × `resubmitMode`）；流程多继续可再提交 |
| 加签 | `SignForward` | `SignForwardProcess` | 在会签/加签语义下增加审批人（与 AntFlow「加批生新节点」需区分，见 §4）；细表见 [OA/专题-改签.md](./OA/专题-改签.md) |
| 沟通 | `Consult` | `ConsultProcess` | 建参与者待办（`AssigneeRole=1`）；锁定发起人审批至全员阅办或收回；阅办=`Acknowledge`，收回=`RecallConsult` |
| 阅办 | `Acknowledge` | `AcknowledgeProcess` | 参与者关闭自己的沟通待办（无审批推进权） |
| 收回沟通 | `RecallConsult` | `RecallConsultProcess` | 发起人取消未反馈的参与者待办并解锁 |
| 抄送 | `Copyto` | `WfCopyto/Create` + `FireCopytoLifecycle` | 知会他人，**不产生**推进权待办；**专用** `ICopytoNotifier` / `OnCopytoCreated`（≠ 待办 TaskNotifier） |
| 撤回 | `Withdraw` | `WithdrawProcess` | 已办人在允许条件下收回上一步办理（常见：下一手尚未办） |
| 重发 | `Resend` | `ResendProcess` | Sail 特色：按重发语义再次送出（常与退回后修正配套；≠ 简单再点同意） |
| 跳转 | `Jump` | `JumpProcess` | 管理态跳到指定活动并指定办理人；强管控，慎对终端用户默认开 |
| 关闭 | `Close` | `CloseProcess` | 结束/关闭实例（管理向）；与终止/作废文案需产品区分 |
| 修订 | `Revise` | `ReviseProcess` | 修订类路径；码表默认 **enable=false** |
| 反转 | `Reverse` | `ReverseProcess` | 反转/回转类路径，**≠** `SendBack`；默认关，接入前核对场景 |

**发起**（Submit / Start）属发起页，**不是**任务办理条按钮；引擎对应 `StartProcess` / `StartProcessByMessage`。

---

## 3. 办理模式要素（非按钮，约束「通过」如何离开）

多人同节点时，**先定 Leave，再谈按钮**。编码在 Assignee 管道，不进 `ButtonsSetting`。细表见 [OA/专题-多人办理模式.md](./OA/专题-多人办理模式.md)。

| 场景 / 常用名 | Sail 编码（Leave Type） | 执行含义 |
|---------------|-------------------------|----------|
| 抢办 / 或签办完即走 | `Race` | 任一人完成即可离开；他人待办应失效（实现细节见管道） |
| 会签 / 协办全员 | `Collaborate` | 需满足组内完成条件才离开 |
| 组间抢办 | `GroupRace` | 组维度抢办 |
| 组间协办 | `GroupCollaborate` | 组维度协办（可叠加 Count/Rate，见管道 R7） |
| 队长关卡 | `Captain` | 关卡与组员待办并存；关卡办完才算组完成（R8） |

**与「承办」关系：** AntFlow 的承办 = 或签场景下**先认领再审批**；Sail 默认用 `Race` 表达「谁办完谁独占」，**不**默认增加 `Undertake` 按钮。详见 [存疑参考功能点.md](./talk/存疑参考功能点.md) §1。

---

## 4. 中国式 OA 常见、尚未入码表（候选编码）

下列为调研常见要素（AntFlow / 公文习惯）。**候选 Id 仅文档建议**；落地须先有引擎命令 + Host API + 审计，再进码表。

| 场景 / 常用名 | 建议 Sail 编码 | 建议引擎落点 | 执行含义 | 现状 |
|---------------|----------------|--------------|----------|------|
| 转办 | `Transfer` | `TransferProcess` | **个人办结 + 本节点交人，不发生节点流转**（原人待办 Closed，新人同 Activity 待办；刷新 SignIds/Names；≠ 委派回转） | **已接线**（码表 + Host `ButtonsOperation`）；见 [todo-产品缺口.md](./dev/todo-产品缺口.md) **§F** · [OA/专题-转办.md](./OA/专题-转办.md) |
| 委派 / 委托 | `Delegate` | `DelegateProcess` | 请他人代办；**受托人办结后自动回转**原责任人待办（不推进）。若办完不回转 → 用 `Transfer`。审计走审批轨迹，不默认单独流水表 | **已接线**（码表默认关 + Handler + 办理条；≠ Surrogate 出差代批） |
| 变更处理人 | `ChangeAssignee` | `ChangeAssigneeProcess` | 管理员/有权人改当前节点办理人集合（差量） | **已接线**（码表默认关 + Handler + Workbench `CurrentAssignees` + 办理条） |
| 承办 / 认领 | `Claim`（勿用 Undertake 当万能名） | 待定认领命令；**仅** `Leave=Race` | 或签多人时先独占待办，**不推进**；再 `Approve`/`Reject` | 存疑，默认不跟 AntFlow 按钮 |
| 加批 | `SignAddNode`（候选） | 与 `SignForward` 拆语义后定 | **生成新审批节点/段**再审（AntFlow JP） | 现揉在加签，未拆 |
| 当前节点加签 | `AddAssignee` | 可落 `SignForward` 子选项 | 不改图结构，仅当前节点增加办理人 | 语义待从 `SignForward` 拆清 |
| 减签 | `RemoveAssignee` | `RemoveAssigneeProcess` | 从当前多人节点移除尚未办完的办理人 | **已接线**（码表 + Host Handler + Workbench 候选人 + 办理条弹窗）；与加签/改派见 [OA/专题-改签.md](./OA/专题-改签.md) |
| 未来节点加签 | `AddFutureAssignee` | 待定 | 改尚未到达节点的预定办理人（加人） | 无；主场景用预选表，见 [OA/专题-超前选人.md](./OA/专题-超前选人.md) |
| 未来节点减签 | `RemoveFutureAssignee` | 待定 | 同上，减人 | 无；改/清预选表 |
| 未来节点换人 | `ChangeFutureAssignee` | 待定 | 同上，替换 | 无；`WfPreselect/Replace` 覆盖 |
| 打回修改 | （多用 `SendBack`） | `SendBackProcess` + 策略 | 退回修改态；常要求发起人/原节点改完再来 | 有退回；缺「再提交落点」策略 |
| 重新提交 | `Resubmit`（候选） | 常组合 `Resend` / `Run` | 被打回后再次提交；落点由 Disagree 策略决定 | 有 `Resend`；缺策略枚举 |
| 终止 | `Terminate` | `TerminateProcess` | 强行结束运行实例 | 码表默认关；Host Handler 已挂 |
| 作废 | `Discard` | `DiscardProcess` | 作废实例 | 码表默认关；Host Handler 已挂 |
| 取消 | `Cancel` | `CancelProcess` | 取消 | 未产品化 |
| 挂起 / 恢复 | `Suspend` / `Resume` | `SuspendProcess` 等 | 暂停与恢复 | 未产品化 |
| 转发（知会外的转发） | 慎用；多归 `Copyto` | Host 抄送或独立 | AntFlow Forward：偏知会/转发；易与抄送混淆 | 以 `Copyto` 为主通道 |

### 4.1 退回策略：两维（配置要素，非单独按钮）

**已拍板：** 退回 = **两个正交维度**，不要扁成单一五选一主键。

| 维度 | 问什么 | 候选码 | Sail 锚点 |
|------|--------|--------|-----------|
| **① 退回目标** | 可以退到哪个节点 | `Prev` / `Starter` / `History` | `SendBackProcess` + 目标约束 |
| **② 再发送方式** | 被退回人改完后再送：正常还是返送 | `ForwardNormal` / `ReturnToSender` | `Run`/`Approve` vs **`ResendProcess`** |

```text
维度①（SendBack 时）     维度②（被退回人再发送时）
Prev | Starter | History  ×  ForwardNormal | ReturnToSender
```

| 维② | 含义 | 现状 |
|-----|------|------|
| `ForwardNormal` | 按图/下一跳继续，不强制回打回人 | 有 `RunProcess`；办理台有 BackSrc 时文案「正常发送」；策略落盘见 todo §J |
| `ReturnToSender` | 回**退回节点** + 尽量**原退回人** | 有 `ResendProcess`；Workbench 下发 `HasBackSrc` 时露出「返送退回人」；策略互斥见 §J |

AntFlow 五型 = 上表笛卡尔积中的常见组合，**仅对照别名**（`BackPrevThenNext` 等），见 [todo-产品缺口.md](./dev/todo-产品缺口.md) **§J**。对外落盘用两维字段。

落地：并入 `sf:NodePolicy.json`（`sendBackTarget` + `resubmitMode`，默认 Starter + ReturnToSender）+ `SendBack` / `Resend` / `Run`；办理台见 todo **§G/§J**；细表见 [OA/专题-退回.md](./OA/专题-退回.md)。

### 4.2 驳回落点（配置要素，非运行时自选）

| 项 | 说明 |
|----|------|
| 产品口径 | 办理 UX **不选**目标、**不选**办理人；落点仅设计期配置 |
| 引擎控制袋 | `ControlParameterSheet.RejectMode` · `RejectReturnActivityId`（仅 Specified） |
| 码 | `Terminate` / `Previous`（默认）/ `Starter` / `Specified` |
| Host `rejectHandlerType` | `1`→Terminate · `2`→Specified（+`returnNodeId`）· `3`→Previous · `4`→Starter；旧「1=结束」纠偏为 Terminate（breaking：旧实现实际曾回 Startup） |
| 红线 | 不写 `BackSrc`、不启用 `Resend`；Specified **禁止**转调 `SendBackProcess` |
| ReasonCode | `RejectModeInvalid` · `RejectTargetMissing` · Terminate 成功时 `RejectModeTerminate`（事件以 Terminate 为准） |

---

## 5. 场景速查（谁在什么时候点）

| 办理上下文 | 典型动词（码表） | 说明 |
|------------|------------------|------|
| 发起页 | （无码表 Id）`StartProcess` | 选定义、填表、选人后启动 |
| 待办 `handle` | `Approve` `Reject` `SendBack` `SignForward` `Transfer` `Consult` `Acknowledge` `RecallConsult` `Copyto` `Revise` | 当前用户有待办；参与者仅 `Acknowledge` |
| 已办 `done` | `Withdraw` `Resend` | 本人已办且流程仍运行 |
| 管理 `admin` | `Jump` `Close` `Reverse` | 运维/有权跳转关闭 |
| 候选扩展 | `Transfer` `Delegate` `Claim` `Terminate` `Discard` … | 见 §4 |

显隐 = **上下文** ∧ **节点 `ButtonsSetting.enable`**（宿主约定）。

---

## 6. 易混词对照（防抄错）

| 说法 | 别当成 | Sail 正确锚点 |
|------|--------|----------------|
| 驳回 | 整单作废 | `Reject` + `RejectMode`；作废用 `Discard` |
| 驳回 | 退回 | `Reject` 配置落点、无 Resend；`SendBack` 可选目标 × 再发送 |
| 退回 | 反转 | `SendBack` ≠ `Reverse` |
| 加签 | 加批（生新节点） | 现 `SignForward`；加批待拆；办理细表 [OA/专题-改签.md](./OA/专题-改签.md) |
| 转办 | 委派 | `Transfer`＝交人不再回；`Delegate`＝代办完**自动回转**原人；细表 [OA/专题-转办.md](./OA/专题-转办.md)；配置型「委托代批」见 [解说-OA委托动词.md](../helps/解说-OA委托动词.md) · [OA/专题-委托代办.md](./OA/专题-委托代办.md) |
| 承办 | 通过 / 越过会签 | 仅或签认领；见存疑文档；Leave=`Race` |
| 抄送 | 算人 / 待办 | Host `Copyto`；禁止塞进 Resolve Type |
| 沟通 | 抄送 / 前加签 | `Consult` 建参与者 `sf_task` 并锁审批；抄送不锁、不加签无审批权 |
| 关闭 | 终止 / 作废 | `Close` vs 候选 `Terminate`/`Discard` |

---

## 7. 演进红线

1. **禁止空壳**：无引擎命令 + Host API，不得进办理条码表。  
2. **Leave 正交**：认领/转办不得默认可绕过 `Collaborate` / 会签。  
3. **抄送独立通道**：不与 Assignee ConfigPair、Task Notifier 混用；用 `AddCopytoNotifier` + `CopytoNotifyKind`。待办推送见 [OA/专题-待办推送.md](./OA/专题-待办推送.md)。  
4. **存疑先记文档**：怪异 UX（如 AntFlow 承办）进 [talk/存疑参考功能点.md](./talk/存疑参考功能点.md)，不默许进基线。  
5. **文案可本地化**：码表 `DisplayName` 可改「同意/通过」；**Id 与 Engine 稳定**。

---

## 修订记录

| 日期 | 说明 |
|------|------|
| 2026-11-01 | 初版：已入码表动词 + Leave 模式 + 中国式候选动词/Disagree 策略 + 易混对照 |
| 2026-11-01 | 转办定为「个人办结+本节点交人、不流转」须有；退回后再走两分对齐 Resend/Run + todo F/G |
| 2026-11-01 | 退回改为两维定型（目标 × 再发送）；AntFlow 五码降为对照别名 |
| 2026-11-01 | 转办 `TransferProcess` 接线：码表 + Host ButtonsOperation + 办理条 |
| 2026-11-15 | **拍板**：委派办完自动回转；不回转则用转办；`DelegateProcess` 引擎落地 |
| 2026-11-15 | 沟通入码表：`Consult`/`Acknowledge`/`RecallConsult`；参与者 `AssigneeRole=1` + 等待协议 |
| 2026-11-15 | 驳回定型：极简 + `RejectMode` 四态；与退回两维划界；Host 不再转调 SendBack |
| 2026-11-16 | 委托一族专项解说：[解说-OA委托动词.md](../helps/解说-OA委托动词.md)（转办/委派现行 + 委托代批候选） |
