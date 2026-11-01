# 待办：产品缺口（NodePolicy + 按钮 + 表单编辑性 + 转办/退回返送）

> 状态：**Open**  
> 录入：2026-11-01（空人/同人 → 超时 → 按钮 → 表单编辑性；本文为合并稿）  
> 续录：2026-11-01（**F 转办**、**G 退回后再走：正常发送 vs 返送退回人**）  
> 关联：[特性说明-待办人管道.md](./特性说明-待办人管道.md) **R4**；[特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md)；宿主 `sailflow-buttons-capability.md`、节点 `sf:NodePolicy` / `sf:ButtonsSetting` / 表单接入

---

## A. NodePolicy：空人 / 同人 / 超时接线

### A1. 缺口摘要

设计器已提供「审批人为空时 / 与发起人相同 / 审批人超时未处理」等产品策略，并落盘 `sf:NodePolicy.json`，但**引擎与 Host 均未按该策略执行**。

| 配置面 | 字段 | UI 选项 | 运行时 |
|--------|------|---------|--------|
| 审批人为空 | `assignEmptyHandlerType` + `assignEmptyUserIds` | 1 自动通过 · 2 自动拒绝 · 3 指定人员 · 4 转交流程管理员 | **未消费** |
| 与发起人相同 | `assignStartUserHandlerType` | 1 自己审 · 2 自动跳过 · 3 转部门负责人 | **未消费** |
| 审批超时 | `timeoutHandlerEnable` + `timeoutHandlerType` + `timeDuration` / `timeUnit` + `maxRemindCount` | 启用后：1 自动提醒 · 2 **自动同意** · 3 自动拒绝 | **未消费**（无调度作业） |

类型注释（宿主）已写明：`引擎不解释，Host/办理台消费`；sleveen-admin 后端亦无 `assignEmpty*` / `timeoutHandler*` 消费点。

**空人现状：** 挂 Assignee 管道后，算人为空且无显式 Resolve `Fallback` → **拒绝推进**（R4），不会自动跳过、转管理员或指定人。

**超时现状：** 无「到点自动同意/拒绝/提醒」闭环；与 `sf:Boundaries` / BPMN Timer 不是同一条产品语义。

### A2. 与现有能力的边界

| 已有 | 能做什么 | 不能替代本缺口 |
|------|----------|----------------|
| Resolve `Fallback`（`primary` → `fallback`） | 主策略空后再算人（如落到 `FixedUsers`） | 不是「跳过节点」；也不是 NodePolicy 语义 |
| 设计器 Fallback 表单的 `onEmpty=Error\|Suspend\|Resolve` | 仅 UI 草稿感 | 与内置 `FallbackAssigneeResolver` 的 `primary/fallback` **未对齐** |
| `SkipDetail` | 建模期显式跳转 | 不读 NodePolicy 空人/同人 |
| `sf:Boundaries` + ISO8601（如 `PT10M`） | 建活动实例时写入 `OverdueDateTime` | **只打逾期时刻**；无作业扫描 → 自动同意/拒绝 |
| BPMN 中间 Timer（`timeDate` / `timeDuration` / `timeCycle`） | 流程等待后继续 | 是定时事件节点，**不是**待办逾期策略 |
| `JobTimer*` / `SetActivityJobTimerCompleted` | Timer/Conditional 等事件态 | 未驱动 NodePolicy 超时动作 |
| R4 拒绝 | 防静默乱派 | 产品要的「显式策略」需另接 |

**原则（保持 R4）：** 禁止暗默认当发起人/管理员；跳过、转管理员、指定人、超时自动办必须来自**已落盘且可审计**的显式配置（NodePolicy 或等价 ConfigPair）。

### A3. 拍板

**空人 / 同人**

- [ ] 空人策略执行归属：**引擎解释 NodePolicy** vs **Host Gate/Hook 消费** vs **导入期转写为 Resolve `Fallback` / Skip**
- [ ] 「自动通过」语义：跳过本节点继续流转，还是生成系统代办并自动同意
- [ ] 「转交流程管理员」：管理员身份键来源（配置项 / 租户角色 / 流程定义字段）
- [ ] 「自动拒绝」：结束流程 vs 走现有 Reject 路径；与 `rejectHandlerType` 关系
- [ ] 同人「自动跳过 / 转部门负责人」与空人策略是否同一执行管线
- [ ] 与 R4 / 发布校验：未配策略时空结果仍拒绝；配了策略才允许跳过/换人

**超时**

- [ ] NodePolicy 超时与 `sf:Boundaries`：并存（双写时长）、互斥，还是发布时把 NodePolicy 转写为 Boundaries
- [ ] 谁调度：Host 后台作业扫 `OverdueDateTime` / 专用队列表，还是引擎内置 drain（对齐 HookOutbox 心智）
- [ ] `timeoutHandlerType=2` 自动同意：系统身份 Runner 调 `Run`，意见文案与审计字段约定
- [ ] `=3` 自动拒绝：走 Reject 还是终结（`Terminated`）；与 `rejectHandlerType` 关系
- [ ] `=1` 自动提醒：复用 Task Notifier，还是独立通道；`maxRemindCount` 与提醒间隔
- [ ] 会签/串行子待办超时：按子任务分别计时，还是按主节点统一计时

### A4. 实现

**公共**

- [ ] 解析 `sf:NodePolicy`（引擎 XPDL 或 Host 侧读 BPMN 扩展）
- [ ] ReasonCode / 前端提示：策略执行成功与仍失败（如管理员未配置、超时作业失败）可区分

**空人 / 同人**

- [ ] 在 **Resolve 之后、建待办之前** 挂空人处理（与管道单轨一致，禁止回落 Definition）
- [ ] `assignEmptyHandlerType=1` 自动通过 / 跳过
- [ ] `assignEmptyHandlerType=2` 自动拒绝
- [ ] `assignEmptyHandlerType=3` 指定 `assignEmptyUserIds`（Account 编码，对齐 R3）
- [ ] `assignEmptyHandlerType=4` 转流程管理员
- [ ] `assignStartUserHandlerType` 同人三策略
- [ ] 收敛设计器 Fallback UI：`onEmpty` 与真实 `FallbackAssigneeResolver` Params 对齐，或标注废弃
- [ ] 单测 + 宿主联调（设计器勾选 → 发起算人为空 → 行为符合选项）

**超时**

- [ ] 启用超时后写入可扫描截止时间（复用/对齐 `OverdueDateTime`，或 NodePolicy → Boundaries 转写）
- [ ] 到期扫描作业（幂等、可恢复、租约/防重）
- [ ] `timeoutHandlerType=1` 自动提醒（含次数上限）
- [ ] `timeoutHandlerType=2` 自动同意
- [ ] `timeoutHandlerType=3` 自动拒绝
- [ ] 与 Boundaries / Timer 事件文档与设计器文案对齐，避免两套超时互相踩
- [ ] 单测 + 宿主联调（短时长 → 到期 → 同意/拒绝/提醒符合选项）

### A5. 建议落地路径（供拍板，非定论）

**空人 / 同人：** 优先 Host 消费 NodePolicy + 引擎只认显式结果（避免 pure 依赖管理员主数据）。Resolve 空 → 读策略 → 指定人/管理员改写 `Performer[]`，或 Skip / Reject。备选：发布期转写 `Fallback`（仅覆盖换人）。

**超时：** 优先建待办落 `OverdueDateTime` + Host 作业扫描（与 Boundaries 时长对齐）。按 `timeoutHandlerType` 调 Run/Reject/Notifier；成功标记已处理。勿用 BPMN 中间 Timer 冒充用户任务逾期。

### A6. 验收口径

1. 空人=指定人员 → 待办落指定 Account。  
2. 空人=转管理员可解析 → 落管理员；未配置 → **拒绝** + 明确 ReasonCode。  
3. 空人=自动通过 → 越过该人工节点（或等价语义），可追溯。  
4. 未配空人策略且算人为空 → 仍 **拒绝推进**。  
5. 同人=自动跳过 有对应用例。  
6. 超时=自动同意 + 短时长 → 系统身份办结，审计可辨。  
7. 超时=自动拒绝 → 符合拍板语义。  
8. 超时=自动提醒 → 次数上限内提醒。  
9. 未启用超时 → 仅有 `OverdueDateTime` 也不自动办结。

---

## B. 按钮：按待办 / 其他分组 + 终止 / 作废产品化

### B1. 缺口摘要

现行 `sf:ButtonsSetting` 是**单表扁平列表**（enable + displayName），显隐再乘运行态 `handle | done | admin`。设计器面板名「办理按钮设置」，**没有**按「待办办理 / 查看·已办·实例操作」分组的产品面。

| 问题 | 现状 |
|------|------|
| 分组 | 码表有 `contexts`，设计器**不分组展示**；无独立「查看页按钮」配置 |
| 撤回 / 关闭 | 已在码表（`done` / `admin`），可被节点 enable 关掉，但与待办按钮混在一张表 |
| 终止 / 作废 | 引擎有 API，**未进按钮码表**，Host 亦无对等办理条入口 |
| 取消 / 挂起 | 引擎有 `CancelProcess` / `SuspendProcess`，同样未产品化 |

### B2. 引擎已有、码表未对齐

| 引擎 API | 语义（摘要） | 按钮码表 | Host Runtime API |
|----------|--------------|----------|------------------|
| `WithdrawProcess` | 撤回 | 有 `Withdraw`（`done`） | 有 |
| `CloseProcess` | 关闭 | 有 `Close`（`admin`） | 有 |
| `TerminateProcess` | **终止** | **无** | **无**（需补） |
| `DiscardProcess` | **作废** | **无** | **无**（需补） |
| `CancelProcess` | 取消 | 无 | 无 |
| `SuspendProcess` | 挂起 | 无 | 无 |

原则：进码表必须有引擎命令 + Host 可调 API + 办理条接线（**禁止空壳按钮**）。

### B3. 建议上下文分组（供拍板）

| 分组（产品） | 建议 context | 典型按钮 | 出现场景 |
|--------------|--------------|----------|----------|
| **待办办理** | `handle` | 通过 / 驳回 / 退回 / 加签 / 抄送 / 修订… | 当前用户待办 |
| **其他 / 实例操作** | `done` + `view`/`instance` + `admin`（命名待定） | 撤回、重发、**终止**、**作废**、关闭、跳转、反转… | 已办、只读查看、管理 |

- [ ] 「其他」是否再拆：`done` vs `view` vs `admin`
- [ ] 终止 / 作废挂组与默认 enable（建议默认关）
- [ ] Close vs Terminate vs Discard 文案与互斥
- [ ] 落盘仍单条 `sf:ButtonsSetting.json` 仅 UI 分组，或拆扩展

### B4. 拍板 / 实现

**拍板**

- [ ] 设计器 UI：待办一组 / 其他一组（或多组）
- [ ] 是否新增 context：`view`
- [ ] 首批：`Terminate` + `Discard`；Cancel/Suspend 同轮或暂缓
- [ ] 权限：谁可见终止/作废
- [ ] 勿把生命周期动作塞进 `sf:Actions`

**码表与设计器**

- [ ] 码表增加 `Terminate`、`Discard`（及拍板项）
- [ ] 设计器按 **待办 / 其他** 分组渲染
- [ ] 终止/作废 `defaultEnable=false`；旧图 merge 不崩

**Host / 办理条**

- [ ] 暴露 `TerminateProcess` / `DiscardProcess` API
- [ ] Workbench / 办理条：查看·已办·管理态展示「其他」组并调通
- [ ] 显隐：`enable` ∧ context ∧ 权限

**验收**

- [ ] 关掉撤回 → 已办无撤回  
- [ ] 开启终止/作废 → 约定上下文可见且调用成功  
- [ ] 待办态默认不出现终止/作废  
- [ ] 旧流程不出现新码（defaultEnable=false）

---

## C. 节点可配置表单编辑性参数（string）

### C1. 目标

节点上为表单字段配置**编辑性参数**，取值用 **string 枚举**（勿再拆成多套布尔语义打架）。Workbench / 动态表单 / **业务表单**接入时：引擎·Host **只下发参数**，**由表单侧自行解释并渲染**（脱敏规则、业务组件适配均在表单内完成）。

### C2. 四种模式（必含）

| 参数值（建议 string） | 含义 | 现状 |
|----------------------|------|------|
| `editable` / `WRITE` | 可编辑 | 运行时等价于非只读且非隐藏 |
| `readonly` / `READ` | 只读 | Form 权限有 `IsReadOnly`；NodePolicy UI 有 `READ` |
| `hidden` / `NONE` | 隐藏 | Form 权限有 `IsNotVisible`；NodePolicy UI 有 `NONE` |
| `masked`（或 `DESENSITIZE`） | **脱敏** | **无**；需新增码 |

拍板后固定**一套** string 常量（推荐小写：`editable` | `readonly` | `hidden` | `masked`），设计器、落盘、Workbench、表单契约共用；旧 `READ`/`WRITE`/`NONE` / `IsReadOnly`/`IsNotVisible` 做映射兼容。

### C3. 现状双轨（须收口）

| 轨 | 落点 | 问题 |
|----|------|------|
| A | `sf:NodePolicy.fieldsPermission`（`field`/`title`/`permission`） | 设计器可配；**办理不读** |
| B | `FormService` 字段活动权限表（`IsReadOnly`/`IsNotVisible`） | Workbench 在用；**无脱敏**；与轨 A 未打通 |

目标态：**节点配置为权威来源（或发布时转写到 Form 权限表）**；运行时统一成「字段 → string 模式」再传给表单。

### C4. 接入约定（定型）

1. Host / Workbench 组装表单上下文时附带：  
   `fieldModes: Record<fieldName, string>` 或 `Array<{ field: string; mode: string }>`（命名拍板）。  
2. **内置动态表单**（form-create 等）：按 mode 自行 `disabled` / 移除节点 / 脱敏展示。  
3. **业务表单**：同一参数结构注入（props / slot / 事件）；业务自行适配，引擎不解析业务字段语义。  
4. 引擎 **不**实现脱敏算法；最多校验 mode ∈ 允许集合。

### C5. 拍板 / 实现

- [ ] 四码最终 string 字面量与大小写
- [ ] 权威落盘：只留 NodePolicy / 只留 Form 权限表 / 双写（发布同步）
- [ ] 设计器：字段选择器 + 四模式（含脱敏）；废弃或映射旧 READ/WRITE/NONE
- [ ] Workbench：`FieldPermissions` 改为（或并行）下发 string `mode`
- [ ] 内置表单：`applyFieldPermissions` 支持四模式（含 `masked` 展示策略）
- [ ] 业务表单接入文档：参数形状 + 示例（自行适配）
- [ ] 兼容：旧 `IsReadOnly`/`IsNotVisible` → string 映射
- [ ] 验收：节点配脱敏 → 办理页该字段脱敏；业务表单能读到同一参数

### C6. 验收口径

1. 节点字段 = `readonly` → 内置表单不可改。  
2. = `hidden` → 不展示。  
3. = `editable` → 可改。  
4. = `masked` → 展示脱敏，提交策略符合表单侧约定（是否回传原文由业务定）。  
5. 业务表单 mock：能收到相同 `field → mode` 映射并自行处理。

---

## D. 文档同步

- [ ] [特性说明-待办人管道.md](./特性说明-待办人管道.md)（空人显式策略与 R4）
- [ ] [特性说明-引擎能力基线.md](./特性说明-引擎能力基线.md)（空人/超时验收 + 办理命令若列表）
- [ ] 宿主 `sailflow-assignee-candidate-design.md` / `sailflow-buttons-capability.md` / `sailflow-designer-sidebar-gap.md`
- [ ] 宿主表单/办理文档：节点字段 mode string 契约（含业务表单自行适配）

---

## E. 参考路径

| 位置 | 说明 |
|------|------|
| `sleveen-admin/web/.../nodeConfig/types.ts` | `SailNodePolicy`（含 `fieldsPermission`） |
| `sleveen-admin/web/.../SailNodeConfigPanel.vue` | 空人 / 同人 / 超时 / 字段权限 UI |
| `sleveen-admin/web/.../provider/sfBoundaryProvider.ts` | `sf:Boundaries` |
| `sleveen-admin/doc/sailflow-designer-sidebar-gap.md` | 两套超时 |
| `sailflow/.../FallbackAssigneeResolver.cs` | `primary`/`fallback` |
| `sailflow/.../ActivityInstanceManager.CalculateActivityOverdueDateTime` | Boundaries → `OverdueDateTime` |
| `sleveen-admin/doc/sailflow-buttons-capability.md` | 按钮能力说明 |
| `sleveen-admin/web/.../bpmn/buttonsCatalog.ts` | 前端码表 |
| `sleveen-admin/backend/saiflow/Utility/SailButtonsCatalog.cs` | 后端码表 |
| `sleveen-admin/web/.../SailOperationBar.vue` | 办理条 |
| `sailflow/.../IWorkflowService.cs` | Terminate / Discard / Cancel / Suspend；**无** Transfer |
| `sleveen-admin/.../Wf2XmlController.Runtime.cs` | Workbench；已有 Withdraw/Close/Resend，缺 Terminate/Discard/Transfer |
| `sailflow/.../FormService.cs` | `GetFieldActivityEditByForm`（IsReadOnly / IsNotVisible） |
| `sleveen-admin/web/.../utils/formCreate.ts` | `applyFieldPermissions`（仅隐藏/只读） |
| `sleveen-admin/backend/saiflow/.../WfFormController.cs` | Get/Save FieldPermissions |
| `sailflow/.../WfRuntimeManagerResend.cs` | 引擎「返送」；依赖 `BackSrcActivityInstanceId` |
| `sailflow/.../WfRuntimeManagerFactory.CreateRuntimeInstanceResend` | 默认办理人：退回源节点 + 当前活动 `CreatedUser*` |
| [特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md) | 动词编码与 Disagree 候选策略 |

---

## F. 转办（常用 · 须有）：个人办结 + 加签，不发生节点流转

### F1. 产品定型（2026-11-01 拍板意向）

| 项 | 约定 |
|----|------|
| 编码 | 码表 Id：`Transfer`（办理条 `handle`） |
| 语义 | **转办 = 当前人待办办结（结束本人任务）+ 在本节点加签式交给他人** |
| 关键 | **不发生节点流转**（不走到下一 Activity；图位置仍在当前 UserTask） |
| 与加签 | 复用/对齐 `SignForward` 的「本节点加人」能力，但转办侧必须**关掉自己的待办**（加签通常保留自己） |
| 与委派 | **不做**「办完回到原人」；转办是责任转移。委派 `Delegate` 另项，本缺口不绑 |
| 优先级 | **高**——中国式 OA 高频；原「暂缓/禁止空壳」改为 **必须产品化** |

### F2. 现状

| 层 | 现状 |
|----|------|
| 引擎 | 无独立 `TransferProcess`；有 `SignForwardProcess`、任务结束 API |
| 码表 / 办理条 | **未入** `Transfer`（宿主文档刻意未纳入） |
| 审计 | 无转办流水（可对照 AntFlow `BpmFlowrunEntrust` 是否要薄审计表——拍板） |

### F3. 建议实现路径（供拍板）

1. **引擎：** 新增 `TransferProcess`（或 Host 事务内组合，但须单一命令语义可测）：校验当前待办 → 结束本人任务（非 `Run` 出节点）→ 本节点为指定人创建待办（对齐 SignForward 建人，**禁止**推进网关/下一节点）。  
2. **Leave：** 转办不触发「离开节点」；会签计数/抢办规则需写清（转出人是否仍计入 Collaborate——建议 **转出后不再计入未办**）。  
3. **码表：** `Transfer` 进 `SailButtonsCatalog`，默认 enable 待拍（建议 **true** 或按项目）；`Engine=TransferProcess`。  
4. **办理条：** 选人控件 → 调 Transfer API；成功后原人待办消失、新人待办出现、活动仍为当前节点。  
5. **禁止：** 用 `Jump`/`Run`/`Approve` 冒充转办；禁止无引擎命令的空壳按钮。

### F4. 验收

- [ ] A 转办给 B → A 待办结束，B 待办在**同一** `ActivityId`，实例未进入下一节点  
- [ ] 会签节点转办后，完成条件按「转出人不再占用名额」可测  
- [ ] 设计器可启停/改名「转办」  
- [ ] 审计可区分转办 vs 加签 vs 通过  

---

## G. 退回后再走：正常发送 vs 返送退回人（节点+人）

### G1. 问题（产品两分）

退回（`SendBack`）之后，被退回方再次送出时，中国式 OA 常见两种：

| 模式 | 含义 | Sail 引擎锚点 |
|------|------|----------------|
| **正常发送** | 按图/下一跳继续走（不强制回到打回人） | 通常 `RunProcess`（或选支路后 Run） |
| **返送退回人** | 回到**退回发生的那个节点**，并尽量落到**当时退回的那个人** | **`ResendProcess`（引擎已有，类注释即「返送」）** |

### G2. 引擎现状（已有能力 · 产品未收口）

| 能力 | 说明 |
|------|------|
| `SendBackProcess` | 退到可选历史节点；活动上可记 `BackSrcActivityInstanceId`（退回源） |
| `ResendProcess` | **要求**存在 `BackSrcActivityInstanceId`；目标活动 = 退回源的 `ActivityId`；默认办理人取当前活动 `CreatedUserId/Name`（典型即退回操作创建侧/返送对象）；可选 `RecreatedMultipleInstanceWhenResending` 改用 Runner 指定人 |
| 码表 | 已有 `SendBack`（handle）、`Resend`（done） |
| **缺口** | ① 未产品化「退回时/节点上选择：改完后默认正常发送还是必须返送」；② 办理台未把两种出口讲清（何时出 Resend、何时出 Approve/Run）；③ Disagree 策略枚举未落 `sf:*`；④ `CreatedUser*` 是否**恒等于**退回人需联调验收，必要时改为显式存「退回人 UserId」 |

### G3. 建议产品化

- [ ] 节点或流程配置 `sf:DisagreePolicy`（名待定）：`ForwardNormal` \| `ReturnToSender`（先覆盖两分；AntFlow 五型可后扩）  
- [ ] **返送**：Workbench 在「被退回且存在 BackSrc」时突出 `Resend`；文案「返送退回人」  
- [ ] **正常发送**：同场景可走 `Approve`/`Run`（或单独「提交」），**不**读 BackSrc  
- [ ] 互斥/引导：若策略=`ReturnToSender`，默认只露出返送；若=`ForwardNormal`，默认正常发送（仍可管理态例外）  
- [ ] 验收：退回人 U1 从节点 N2 退到 N1 → N1 办理人返送 → 待办回到 **N2 + U1**；另一路径 Run 则按出边到下一跳而非强制 U1  

### G4. 与 OA 要素文档

详见 [特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md) §4.1；本文 G 为落地待办，要素文档为语义对照。
