# 待办：产品缺口（NodePolicy + 按钮 + 表单 + 转办/退回 + 表单生命周期 + 按钮分发 + DisagreeType）

> 状态：**Open**  
> 录入：2026-11-01（空人/同人 → 超时 → 按钮 → 表单编辑性；本文为合并稿）  
> 续录：2026-11-01（**F 转办**、**G 退回两分**、**H 表单生命周期**、**I 按钮分发**、**J 退回策略 DisagreeType 枚举化**）  
> 拍板：2026-11-01 — **空人默认=拒绝**（禁止默认自动通过；`=2` 细义=**R4 拒绝推进**）；退回 = **目标维 × 再发送维** 正交（见 §G / §J）  
> 实施：2026-11-01 — **§F/I/G/J**：转办 + ButtonsOperation + 办理台两分 + **NodePolicy 两维落盘**（`sendBackTarget`/`resubmitMode`）；联调验收待做  

> 关联：[特性说明-待办人管道.md](./特性说明-待办人管道.md) **R4**；[特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md)；[特性设计-回调与事件.md](./特性设计-回调与事件.md)；宿主 `sailflow-buttons-capability.md`、节点 `sf:NodePolicy` / `sf:ButtonsSetting` / 表单接入

### 优先级（实施顺序建议）

| 序 | 块 | 优先级 | 依赖 / 说明 |
|----|----|--------|-------------|
| 1 | **I** 按钮分发 | P0 | 后续新按钮（转办等）不堆 if |
| 2 | **F** 转办 | P0 | 中国式高频；依赖 I 或与 I 同轮 |
| 3 | **G** 退回两维办理台 | P0 | 目标维 + 再发送维 UX；引擎命令已有 |
| 4 | **J** 两维落盘/设计器 | P1 | 紧随 G |
| 5 | **A** 空人/同人（含默认拒绝） | P1 | 与 R4 对齐；超时可后置为 P2 |
| 6 | **C** 字段 mode string | P1 | 可与 H 并行 |
| 7 | **B** 终止/作废 | P2 | 引擎已有，偏管理面 |
| 8 | **H** 表单生命周期 | P2 | 与 I 骨架可同轮，请假样例作验收 |
| 9 | **D** 文档同步 | 随各块收尾 | 宿主转办口径已先改 |

---

## A. NodePolicy：空人 / 同人 / 超时接线

### A1. 缺口摘要

设计器已提供「审批人为空时 / 与发起人相同 / 审批人超时未处理」等产品策略，并落盘 `sf:NodePolicy.json`，但**引擎与 Host 均未按该策略执行**。

| 配置面 | 字段 | UI 选项 | 运行时 |
|--------|------|---------|--------|
| 审批人为空 | `assignEmptyHandlerType` + `assignEmptyUserIds` | 1 自动通过 · **2 自动拒绝（产品默认）** · 3 指定人员 · 4 转交流程管理员 | **未消费** |
| 与发起人相同 | `assignStartUserHandlerType` | 1 自己审 · 2 自动跳过 · 3 转部门负责人 | **未消费** |
| 审批超时 | `timeoutHandlerEnable` + `timeoutHandlerType` + `timeDuration` / `timeUnit` + `maxRemindCount` | 启用后：1 自动提醒 · 2 **自动同意** · 3 自动拒绝 | **未消费**（无调度作业） |

类型注释（宿主）已写明：`引擎不解释，Host/办理台消费`；sleveen-admin 后端亦无 `assignEmpty*` / `timeoutHandler*` 消费点。

**空人现状：** 挂 Assignee 管道后，算人为空且无显式 Resolve `Fallback` → **拒绝推进**（R4），不会自动跳过、转管理员或指定人。

**空人默认（已拍板 2026-11-01）：** 新建节点 / merge 缺省 → `assignEmptyHandlerType=2`（**拒绝**）。**禁止**默认 `=1` 自动通过（与 R4「禁止暗默认跳过」一致）。`=1/3/4` 必须建模者显式改配才生效。

**`=2` 细义（已拍板）：** 对齐 **R4 拒绝推进**——不落待办、流程不静默继续，`WfExecutedResult` 带可识别 `ReasonCode`（如 `AssigneeEmpty`）。**不等于**默认调用 `RejectProcess` 结束整单；若产品要「空人则整单驳回结束」，须另开显式选项或复用 `rejectHandlerType`，不得与默认 `=2` 混名。

**同属 NodePolicy、本文未单独立项：** `approveType` / `rejectHandlerType`（设计器已有、同样未消费）——与空人拒绝路径相关时在 A3 捎带拍板，完整产品化可另开或并入本块尾声。

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

- [x] **默认空人 = 拒绝**（`assignEmptyHandlerType` 缺省/新建 = `2`；禁止默认自动通过）— 2026-11-01 拍板
- [x] **`=2` = R4 拒绝推进**（不落待办 + ReasonCode；**不是**默认 `RejectProcess` 整单结束）— 2026-11-01 拍板
- [ ] 空人策略执行归属：**引擎解释 NodePolicy** vs **Host Gate/Hook 消费** vs **导入期转写为 Resolve `Fallback` / Skip**
- [ ] 「自动通过」语义：跳过本节点继续流转，还是生成系统代办并自动同意（仅显式选 1 时）
- [ ] 「转交流程管理员」：管理员身份键来源（配置项 / 租户角色 / 流程定义字段）
- [ ] 是否另增「空人则 Reject 结束整单」选项（与默认 `=2` 分离）；若有，与 `rejectHandlerType` 关系
- [ ] 同人「自动跳过 / 转部门负责人」与空人策略是否同一执行管线
- [ ] 与 R4 / 发布校验：默认拒绝；仅显式 `=1/3/4` 才允许跳过/换人；旧图若曾落盘 `=1` 按显式配置尊重
- [ ] `approveType` / `rejectHandlerType`：本轮是否一并接线，或只文档记债

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
- [ ] 设计器 / merge：`assignEmptyHandlerType` **默认 2**；文案突出「默认拒绝」
- [ ] `assignEmptyHandlerType=2` 拒绝（默认路径；细义见 A3）
- [ ] `assignEmptyHandlerType=1` 自动通过 / 跳过（显式才开）
- [ ] `assignEmptyHandlerType=3` 指定 `assignEmptyUserIds`（Account 编码，对齐 R3）
- [ ] `assignEmptyHandlerType=4` 转流程管理员
- [ ] `assignStartUserHandlerType` 同人三策略
- [ ] 收敛设计器 Fallback UI：`onEmpty` 与真实 `FallbackAssigneeResolver` Params 对齐，或标注废弃
- [ ] 单测 + 宿主联调（默认空人拒绝；显式勾选通过/指定人/管理员 → 行为符合）

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

1. **新建节点未改空人策略** + 算人为空 → **拒绝推进**（默认 `=2` = R4 + ReasonCode），**不得**自动通过，也**不得**默认整单 `RejectProcess`。  
2. 空人=指定人员 → 待办落指定 Account。  
3. 空人=转管理员可解析 → 落管理员；管理员未配置 → **拒绝** + 明确 ReasonCode。  
4. 空人=自动通过（显式 `=1`）→ 越过该人工节点（或等价语义），可追溯。  
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

- [x] 宿主 `sailflow-buttons-capability.md`：转办改为须有；Terminate/Discard 记缺口；委派仍暂缓（2026-11-01）
- [x] 宿主 `sailflow-designer-sidebar-gap.md`：转办/委派口径拆开（2026-11-01）
- [x] [存疑参考功能点.md](./存疑参考功能点.md)：承办对照表不再写「转办刻意不进码表」（2026-11-01）
- [x] [特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md) §4.1：退回两维（2026-11-01）
- [x] [特性说明-待办人管道.md](./特性说明-待办人管道.md)：R4 / §7.3 对齐空人默认拒绝与 NodePolicy（2026-11-01）
- [ ] [特性说明-引擎能力基线.md](./特性说明-引擎能力基线.md)（空人/超时验收 + 办理命令若列表 + Transfer）
- [ ] 宿主 `sailflow-assignee-candidate-design.md`（若仍写空人/转办旧口径）
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
| `sailflow/.../IWorkflowService.cs` | 含 `TransferProcess`；Terminate / Discard / Cancel / Suspend |
| `sleveen-admin/.../Wf2XmlController.Runtime.cs` | 含 Transfer / ButtonsOperation；缺 Terminate/Discard 产品入口 |
| `sleveen-admin/.../Operations/*` | Host 办理分发：`SailOperationDispatcher` + Handlers |
| `sailflow/.../FormService.cs` | `GetFieldActivityEditByForm`（IsReadOnly / IsNotVisible） |
| `sleveen-admin/web/.../utils/formCreate.ts` | `applyFieldPermissions`（仅隐藏/只读） |
| `sleveen-admin/backend/saiflow/.../WfFormController.cs` | Get/Save FieldPermissions |
| `sailflow/.../WfRuntimeManagerResend.cs` | 引擎「返送」；依赖 `BackSrcActivityInstanceId` |
| `sailflow/.../WfRuntimeManagerFactory.CreateRuntimeInstanceResend` | 默认办理人：退回源节点 + 当前活动 `CreatedUser*` |
| [特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md) | 动词编码与 Disagree 候选策略 |
| AntFlow `ProcessDisagreeTypeEnum` | 对照别名：目标维 × 再发送维（非 Sail 主契约） |
| AntFlow `IFormOperationAdaptor` / `ThirdPartyAccountApplyService` | 对照：一 formCode 一类生命周期 |
| AntFlow `IProcessOperationAdaptor` / `ButtonOperationService` / `FormOperationTagParser` | 对照：operationType → 一动词一类 |
| `sleveen-admin/.../WfLeaveController` + `SailLeaveService` + `SailLeaveStatusHook` | 现状：发起/查询/回写分散，待收口为 Handler |
| `sleveen-admin/.../SailOperationBar.vue` + `Wf2XmlController.Runtime` | 转办已走 `ButtonsOperation`；其余按钮仍可直调旧 API |

---

## F. 转办（常用 · 须有）：个人办结 + 本节点交人，不发生节点流转

> **实施进度（2026-11-01）：** 引擎 `TransferProcess` + 码表 + Host API/`ButtonsOperation` + 办理条已接线。联调验收项仍开放。

### F1. 产品定型（2026-11-01 拍板意向）

| 项 | 约定 |
|----|------|
| 编码 | 码表 Id：`Transfer`（办理条 `handle`） |
| 语义 | **转办 = 当前人待办办结（结束本人任务）+ 在本节点交给他人** |
| 关键 | **不发生节点流转**（不走到下一 Activity；图位置仍在当前 UserTask） |
| 与加签 | 转办关掉自己的待办（`Closed`，不计入会签完成）；加签通常保留自己 |
| 与委派 | **不做**「办完回到原人」；转办是责任转移。委派 `Delegate` 另项，本缺口不绑 |
| 优先级 | **高**——已落地命令；验收联调中 |

### F2. 现状（接线后）

| 层 | 现状 |
|----|------|
| 引擎 | **有** `TransferProcess`：关原待办 → 同活动建新人待办 → 刷新 `AssignedUser*`；Gate/Hook=`Transfer` |
| 码表 / 办理条 | **已入** `Transfer`；办理条经 `ButtonsOperation` |
| 审计 | **不另建** entrust 流水表；任务生命周期 `Entrusted` + 轨迹可辨 |

### F3. 已实现路径

1. **引擎：** `TaskManager.Transfer` + `WorkflowService.TransferProcess`（事务 + Gate/Hook）。  
2. **Leave：** 转出人任务 `Closed`，并从活动 `AssignedUser*` 移除，**不再占用会签未办名额**。  
3. **码表：** 前后端 `Transfer` / `TransferProcess`，默认 enable=true。  
4. **办理条：** 选人 → `ButtonsOperation{ButtonId:Transfer}`。  
5. **禁止：** 用 `Jump`/`Run`/`Approve` 冒充转办。

### F4. 验收

- [x] 引擎/Host/码表/办理条接线（编译通过）  
- [ ] A 转办给 B → A 待办结束，B 待办在**同一** `ActivityId`，实例未进入下一节点（联调）  
- [ ] 会签节点转办后，完成条件按「转出人不再占用名额」可测  
- [ ] 设计器可启停/改名「转办」  
- [ ] 审批轨迹能看出本次为转办（及对象）；**无**独立 entrust 表依赖  


---

## G. 退回：两维模型（目标 × 再发送）

### G1. 定型（已拍板 2026-11-01）

退回**不是**一个五选一扁枚举；是两个**正交维度**：

| 维度 | 问的是什么 | 何时决定 | 引擎锚点 |
|------|------------|----------|----------|
| **① 退回目标** | 可以退到**哪个节点** | 点 `SendBack` 时（可被节点/流程策略约束） | `SendBackProcess` + 目标 Activity |
| **② 再发送方式** | 被退回人改完后再送出：正常走还是返送 | 节点/流程策略；办理台按策略露出出口 | `Run`/`Approve` vs `ResendProcess` |

```text
SendBack 时 ──► 维度① 选定目标节点（上一 / 发起 / 历史自选…）
                 同时记下 BackSrc（谁从哪退下来的）

被退回人再发送 ──► 维度②
                    ├─ 正常发送 → Run / Approve（沿出边/下一跳，不强制回退回人）
                    └─ 返送     → Resend（回退回节点 + 尽量原退回人）
```

**禁止**把「退到哪」和「再怎么走」揉成唯一主键却在文档里又拆开说——对外契约以**两维字段**为准；AntFlow 五码只作对照别名（见 §J）。

### G2. 维度①：退回目标（可退到哪）

| 目标码（候选） | 含义 | 办理 UX |
|----------------|------|---------|
| `Prev` | 上一节点 | 常可自动解析，可不弹选 |
| `Starter` | 发起人节点 | 固定目标 |
| `History` | 自选历史节点 | 退回时弹出历史树/列表 |

策略可限制「本节点只允许 Prev / 只允许 Starter / 允许 History」。引擎侧 `SendBack` 已支持选历史节点；缺的是**产品策略约束 + 办理台按策略收窄可选集**。

### G3. 维度②：再发送（正常 vs 返送）

| 再发送码（候选） | 含义 | Sail 引擎锚点 |
|------------------|------|----------------|
| `ForwardNormal` | 按图/下一跳继续，**不**强制回打回人 | `RunProcess`（或选支路后 Run） |
| `ReturnToSender` | 回到**退回发生的那个节点**，并尽量落到**当时退回的那个人** | **`ResendProcess`**（依赖 `BackSrcActivityInstanceId`） |

### G4. 引擎 / 办理台现状

| 能力 | 说明 |
|------|------|
| `SendBackProcess` | 退到可选历史节点；活动上可记 `BackSrcActivityInstanceId` |
| `ResendProcess` | 要求 `BackSrcActivityInstanceId`；回退回节点 + 尽量原退回人 |
| Workbench | **已下发** `HasBackSrc` / `BackSrcActivityInstanceId` |
| 办理条 | **已分流**：有 BackSrc 时 Approve 文案「正常发送」、Resend「返送退回人」；Resend 可在 handle 露出 |
| **仍缺** | ① 目标维/再发送维**落盘策略**（§J）；② SendBack 按目标维收窄可选集；③ 策略互斥默认只露一个出口；④ `CreatedUser*` vs 显式退回人联调 |

### G5. 产品化清单

- [x] 办理台探测 BackSrc + 两分文案/入口  
- [x] 落盘两字段：`sendBackTarget` + `resubmitMode`（§J / NodePolicy）  
- [x] 办理台 `SendBack`：按目标维收窄 PreviousNodes（Prev/Starter/History）  
- [x] 互斥：读 `resubmitMode` — ReturnToSender 只露返送；ForwardNormal 只露正常发送  
- [ ] 验收联调：U1 从 N2 退到 N1 → 返送 → **N2+U1**；ForwardNormal → Run 沿出边  

### G6. 与 OA / §J

详见 [特性说明-中国式OA审批要素.md](./特性说明-中国式OA审批要素.md) §4.1。G = 两维语义 + 办理台先行；J = 落盘、设计器、与 AntFlow 五码对照表。

---

## H. 表单生命周期 Adaptor（一业务一类）

### H1. 目标

学 AntFlow「**一个业务表单 = 一个 Handler 类**」：业务方几乎不碰 `Start/Run/Reject/…` 细 API，只实现约定生命周期方法；Host 按 **formCode / AppName / 流程定义键** 路由到该类。

**落点：Host / biz（禁止塞进 pure 调度内核）。** 内部仍调 Sail 命令 + Gate/Hook；对外给 CRUD 业务「一个类管一张单」的 DX。

### H2. 对照语义（AntFlow → Sail）

| 生命周期方法（AntFlow） | 含义 | Sail 侧建议落点（Handler 方法内） |
|--------------------------|------|----------------------------------|
| `OnInitData` | 打开发起页默认值 | Handler 填 VO；可不调引擎 |
| `OnQueryData` | 打开详情按 BusinessId 回填 | Handler 查产品表 |
| `OnSubmitData` | 首次提交：插业务表、写出 BusinessId/标题 | Handler 落库 → 再 `StartProcess` |
| `PreviewSetCondition` / `LaunchParameters` | 分支/启动条件 | 写入 Runner 变量 / 条件字典 |
| `OnConsentData` | 同意或再提交时更新业务 | Gate 后或 Hook/同意路径调 Handler |
| `OnBackToModifyData` | 打回修改 | `SendBack` 成功后回调 Handler |
| `OnCancellationData` | 取消 | Close/Cancel 路径回调 |
| `OnFinishData` | 流程结束归档 | 完成态 Hook 调 Handler（对齐现请假 Status Hook） |

流转动词（通过/退回/转办）仍走 OA 按钮 → 引擎命令；**Handler 只负责业务单读写与状态**，不实现转办/会签。

### H3. 现状缺口

| 层 | 现状 |
|----|------|
| 请假样例 | `WfLeaveController.Create` 落单+Start；`SailLeaveStatusHook` 单独回写 Status；Query/Get 另入口——**无统一接口、无 formCode 注册表** |
| 动态表单 | form-create + `sf:Forms`；业务回写靠零散 Hook，无「一类」范式 |
| DIY 业务页 | 约定弱；新业务易复制 Controller/Service/Hook 三件套 |
| pure | **正确**：不应出现业务表 Schema；本缺口不改引擎分层 |

### H4. 建议契约（供拍板）

```csharp
// 名待定：ISailFormBizHandler / ISailBizFormLifecycle
interface ISailFormBizHandler
{
    string FormCode { get; }   // 或 AppName / ProcessCode 路由键

    void OnInit(SailFormBizContext ctx);
    void OnQuery(SailFormBizContext ctx);
    void OnSubmit(SailFormBizContext ctx);      // 落库 + 填 BusinessId；Start 可由 Host 模板调用
    void OnConsent(SailFormBizContext ctx);     // 同意/再提交
    void OnBackToModify(SailFormBizContext ctx);
    void OnCancel(SailFormBizContext ctx);
    void OnFinish(SailFormBizContext ctx);      // 完成/驳回结束等，ctx 带终态
}
```

- [ ] 注册：`builder.UseBizFormHandler<LeaveFormHandler>()` 或按 FormCode 字典  
- [ ] Host 模板：`SubmitBizForm(formCode, json)` → 反序列化 → `OnSubmit` → `StartProcess`  
- [ ] 同意/退回/结束：办理 API 或薄 Hook 适配器按实例 FormCode **回调同一 Handler**（避免业务再注册多个零散 Hook）  
- [ ] 低代码：可提供一个 `DynamicFormBizHandler` 默认实现（只存 conf/字段 JSON）  
- [ ] 文档：最佳实践「新 DIY 业务 = 复制一个 Handler 类」；请假样例迁到该范式  

### H5. 红线

1. **不**把 Handler 放进 `sailflow.engine` pure。  
2. **不**让 Handler 直接操作活动图/待办人管道；要推进只通过 `IWorkflowService` / 已有命令。  
3. Gate 校验可与 Handler 并存：Gate 拒单；Handler 做持久化副作用。  
4. 与 §F 转办、§G 返送正交：那些是办理动词；Handler 只在对应成功回调里改业务表。

### H6. 验收

- [ ] 新增样例业务：仅新增一个 Handler + 注册，无第二套散落 Hook 也能完成「发起→同意→结束回写」  
- [ ] 请假样例改为 Handler 后，删除/收缩专用 Status Hook 或令其变为对 Handler 的委托  
- [ ] formCode 未注册 → 明确错误，不静默空转  
- [ ] 动态表单走默认 Handler 仍可发起/办结  

---

## I. 按钮 → OperationHandler 分发（Host 统一办理入口）

### I1. 目标

学 AntFlow「**按钮码 → ProcessOperationAdaptor**」：Host 提供（或内聚）统一办理入口，按码表 Id / `operationType` **路由到唯一 Handler**；新增办理动词 = 新 Handler + 登记支持的按钮 Id，禁止在 `SailOperationBar` / Controller 里无限堆 `if (id===…)`。

**落点：Host（及可选薄 biz 门面）。** 引擎继续保持 `RunProcess` / `SendBackProcess` / 未来 `TransferProcess` 等命令；Handler 只做「按钮语义 → 调哪条命令 + 参数组装 +（可选）调 §H 表单生命周期」。

### I2. 对照（AntFlow → Sail）

| AntFlow | Sail 应对 |
|---------|-----------|
| 单一 `buttonsOperation` + `operationType` 枚举 | Host `ButtonsOperation`（或等价）+ 码表 Id（`Approve`/`Transfer`/…） |
| `IProcessOperationAdaptor.DoProcessButton` | `ISailOperationHandler.Execute(ctx)` |
| `SetSupportBusinessObjects(枚举…)` 自报支持 | Handler 声明 `SupportedButtonIds` 或注册表 `Register("Transfer", handler)` |
| `FormOperationTagParser` 扫描命中 | `SailOperationDispatcher.Resolve(buttonId)` |
| Adaptor 内再调 FormAdaptor | Handler 内再调 §H `ISailFormBizHandler`（发起/同意等） |

```text
办理条点击按钮Id
  → POST Host/ButtonsOperation { buttonId|operationType, taskId, form… }
  → Dispatcher.Resolve(buttonId) → ISailOperationHandler
  → Handler：组 WfAppRunner → IWorkflowService.XxxProcess
  →（按需）FormBizHandler.OnConsent / OnSubmit / …
```

### I3. 现状（接线后）

| 层 | 现状 |
|----|------|
| 码表 | Id ↔ Engine + **Handler 注册**（`AddSailOperationHandlers`） |
| 办理条 | **引擎按钮均走** `ButtonsOperation`（Copy 仍产品表） |
| 引擎 | 未改 AntFlow 枚举；已补 `TransferProcess` |
| 探针 | `GET api/Wf2Xml/ProbeOperationHandlers` |

### I4. 契约（已落地骨架）

```csharp
// DI: AddSailOperationHandlers() → SailOperationDispatcher
// POST api/Wf2Xml/ButtonsOperation { ButtonId, Runner }
```

- [x] Host API：`POST …/ButtonsOperation`  
- [x] Dispatcher：未知 Id → `OperationHandlerMissing`  
- [x] Handlers：Approve/Reject/SendBack/SignForward/**Transfer**/Withdraw/Resend/Close/Jump/Revise/Reverse  
- [x] 办理条引擎按钮全部改走统一入口（Copy 除外）  
- [ ] 与 §H：Approve Handler 内调 FormBizHandler  
- [x] 与 §G：有 BackSrc 时办理台两分文案；策略落盘仍待 §J  
- [x] 探针列出码表 Id × 是否已挂 Handler（Copy 无 Handler 属预期）  

### I5. 红线

1. **不**把 Dispatcher/Handler 放进 pure。  
2. **禁止**空壳按钮（Copy 走产品表，不经本 Dispatcher）。  
3. 一个按钮 Id **唯一** Handler（后注册覆盖打日志）。  
4. 对外以字符串码表 Id 为准。

### I6. 验收

- [x] Dispatcher + 办理条引擎按钮均走 ButtonsOperation  
- [x] Transfer / Resend / Approve… 统一入口  
- [x] 未注册 Id → 业务错误  
- [x] ProbeOperationHandlers 可用  

---

## J. 退回策略落盘（两维 · 对照 AntFlow 五码）

### J1. 目标

把 §G 两维落成**稳定配置**（节点/流程），运行时映射到现有 `SendBack` + `Resend` / `Run`，**禁止**再发明第五套推进内核。

**对外契约（已拍板）：两维字段，不是单一五选一。**

| 字段（候选名） | 维 | 取值 |
|----------------|----|------|
| `sendBackTarget` | ① 退回目标 | `Prev` \| `Starter` \| `History` |
| `resubmitMode` | ② 再发送 | `ForwardNormal` \| `ReturnToSender` |

与 **§G** 关系：G 定语义与办理台；J 定落盘、设计器、Handler 消费与 AntFlow 对照。

### J2. 与 AntFlow 五码对照（仅别名，非主契约）

AntFlow `ProcessDisagreeTypeEnum` = 两维笛卡尔积中的常见组合，**导入/对照用**，不要当成 Sail 唯一主键：

| AntFlow | 对照别名（可选） | `sendBackTarget` | `resubmitMode` |
|---------|------------------|------------------|----------------|
| 1 退回上一节点提交下一节点 | `BackPrevThenNext` | `Prev` | `ForwardNormal` |
| 2 退回发起人提交下一节点 | `BackStarterThenNext` | `Starter` | `ForwardNormal` |
| 3 退回发起人提交回退节点（其默认） | `BackStarterThenReturn` | `Starter` | `ReturnToSender` |
| 4 退回历史节点提交下一节点 | `BackHistoryThenNext` | `History` | `ForwardNormal` |
| 5 退回历史节点提交回退节点 | `BackHistoryThenReturn` | `History` | `ReturnToSender` |

设计器 UI：**两个下拉/单选**（目标 + 再发送），不要只给一个「五型」下拉却在别处又拆维。若需兼容展示，可用上表生成只读别名。

### J3. 现状（接线后）

| 层 | 现状 |
|----|------|
| 落盘 | 并入 `sf:NodePolicy.json`：`sendBackTarget` + `resubmitMode`（默认 Starter + ReturnToSender） |
| 设计器 | 节点弹窗「退回策略（两维）」两单选 |
| Host | `SailNodePolicyCatalog.ParseForActivity`；Workbench 下发并收窄 `PreviousNodes` |
| 办理台 | 按 `resubmitMode` 互斥 Approve/Resend；有 BackSrc 时文案两分 |
| **仍缺** | 联调验收；`BackSrcUserId` 显式字段；流程级默认覆盖；单测矩阵 |

### J4. 落盘与消费

- [x] 并入 `sf:NodePolicy.json`：`sendBackTarget` + `resubmitMode`  
- [x] 设计器两维控件；缺字段 merge 默认 `Starter` + `ReturnToSender`  
- [x] Workbench 按目标维收窄 PreviousNodes；按再发送维互斥出口  
- [ ] SendBack Handler 内再强校验目标（当前依赖 Workbench 已收窄列表）  
- [ ] 单测：两维 × 边界；非法组合 Reason  
- [x] 对外不用 AntFlow 1–5 数字  

### J5. 红线

1. 两维只描述「退哪 + 再怎么走」，**不**替代 Leave / 会签。  
2. 实现 = 组合现有命令；禁止为 Disagree 新开并行 RuntimeManager 家族。  
3. 与 `Reject`（驳回）分离：本策略服务 **SendBack**；驳回整单另语义。  
4. 退回人身份以显式字段为准（`CreatedUser*` 不可靠则补 `BackSrcUserId`），见 §G4④。  
5. **禁止**只落一个扁枚举却在运行时再拆维却无字段——主存储必须是两维（或等价可逆拆分）。

### J6. 验收

- [x] 设计器可配两维并写入 NodePolicy JSON  
- [x] Workbench 返回 `SendBackTarget` / `ResubmitMode`；PreviousNodes 按维①收窄  
- [x] 有 BackSrc 时维②互斥露出返送或正常发送  
- [ ] 联调：`Starter`+`ReturnToSender` 返送落点正确  
- [ ] 联调：`Prev`+`ForwardNormal` 不强制回退回人  
- [ ] 旧图无字段 merge 默认不崩  

