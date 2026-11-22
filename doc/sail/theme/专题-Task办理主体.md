# 专题 · Task 办理主体

> 状态：**契约说明**（字段已落 `sf_task` / `Performer`；精确人路径已落地；模糊主体办理池由业务中心过滤，见 §6）  
> 用途：回答「Task 的办理主体是什么、为何不是简单一个人、多维属性怎么分、何时指精确人 / 何时指模糊对象」。  
> 范围：`sf_task` 办理对象列段 + 正交维度；不含 Leave 完成规则细节、不含组织主数据实现。  
> 关联：  
>
> - 两层模型：[专题-节点实例与任务.md](./专题-节点实例与任务.md)（办理权以 Task 为准）  
> - 计权 / 代批：[专题-办理人权重与委托体系.md](./专题-办理人权重与委托体系.md)  
> - 算人管道：[特性说明-待办人管道.md](../特性说明-待办人管道.md)（R1 Group≠Unit · R3 身份键）  
> - 多人模式：[OA/专题-多人办理模式.md](../OA/专题-多人办理模式.md)  
> - 实体：`TaskEntity` · `Performer` · `SignKindEnum` · `AssigneeRoleEnum`

---

## 0. 一句话

**本引擎的 Task 办理主体是「带类型的引用 + 多维属性袋」，不是「一个 UserId」。**  
可以是精确到人，也可以是部门 / 角色 / 群组 / 自定义对象；业务中心按维度做权限过滤后，有权限的人从办理池认领并流转。

```text
SignKind + SignId     ← 主体身份（是谁 / 哪类对象）
Group* / Unit*        ← 正交维度（分组办结 · 单位展示筛）
Weight / ProxySignId  ← 计权与代批挂载
AssigneeRole          ← 审批席 vs 沟通参与者
Biz* / 时钟 / 状态    ← 挂接与生命周期（不是主体身份）
```

---

## 1. 为何必须多维

### 1.1 「只知道大概谁办」是生产常态

很多业务**来不及、也不需要**在发送瞬间点到具体人：

| 压力 | 典型诉求 | 若强行精确到人 |
|------|----------|----------------|
| **时效** | 7 个节点一个上午必须走完 | 选人 UI 拖死节奏；人不在岗即卡死 |
| **组织模糊** | 只知「财务部 / 值班岗 / 某审批群」 | 名单过时、请假替岗全靠改派 |
| **池式办理** | 谁先看到谁办 | 建 N 条人待办 → 抢办关旁人；名单爆炸 |
| **权限在业务侧** | 能看池 = 有岗位/部门/角色属性 | 引擎扩写组织树 = 越界 |

此时正确姿势是：**任务挂「模糊主体」**（部门、岗位群、角色、自定义码），在业务中心展示办理池；业务侧按当前用户的维度属性过滤可见集，匹配者即可办理。

### 1.2 与「多人精确名单」的边界

| 模式 | 主体形态 | 典型 Leave | 谁决定「我能不能办」 |
|------|----------|------------|----------------------|
| **精确人** | `SignKind=User`，`SignId=账号` | Collaborate / Race / … | 引擎 `IsMine`：`SignId == UserId` |
| **多人精确名单** | N 条 User Task | Race / Collaborate / Group* | 引擎按 Task.SignId；Leave 管「几人算过」 |
| **模糊主体（本专题焦点）** | 1（或少）条 Dept/Role/Group/Custom Task | 常配合 Race / GroupRace | **业务中心维度过滤** + 办理时写入实际办结人 |

三者可并存于同一流程的不同节点：发起精确、中段模糊池、末段再精确。

### 1.3 引擎立场

| 做 | 不做 |
|----|------|
| 持久化 **SignKind + SignId** 与 Group/Unit 等维度 | **不**在 pure 内建组织树 / 岗位编制表 |
| 精确人路径 `IsMine` 仍以 `SignId` 为准 | **不**把「部门任务」默认展开成全员 Task 行 |
| 模糊主体任务可进业务办理池查询 | **不**用 AI.`SignIds` 当权限（见节点实例专题 §1.4） |
| 办结时记录真实 `EndedUserId` | **不**用 GroupId 冒充企业单位（R1） |

---

## 2. 决策表（本专题口径）

| 编号 | 决策 | 口径 |
|------|------|------|
| **S1** | 办理主体 = **类型化引用** | `SignKind` + `SignId`（+ `SignName` 展示）；`TaskId` 是行主键，**不是**办理人 Id |
| **S2** | Kind 枚举对齐现行 | `User=0` · `Dept=1` · `Role=2` · `Group=3` · `Custom=4`；null 同 User |
| **S3** | 维度正交，禁止揉扁 | Group*（Leave）≠ Unit*（单位展示筛）≠ SignKind（主体类型）≠ AssigneeRole（席位角色） |
| **S4** | 精确人鉴权在引擎 | `TaskManager.IsMine`：`SignId == userId`（仅对 User 语义有定义） |
| **S5** | 模糊主体可见性在业务中心 | 列表/池查询按 Dept/Role/Group/Custom + 用户属性过滤；**不**要求引擎展开成员 |
| **S6** | 模糊任务办结须落真人 | `EndedUserId`/`EndedUserName`（及意见）记实际操作者；`Sign*` 仍保留原模糊对象快照 |
| **S7** | 算人输出可直接写模糊主体 | Resolve/Specific 可产出非 User 的 Performer 映射到 `SignKind`/`SignId`；宿主 Adapter 填齐维度 |
| **S8** | 一任务一行主体 | 一个模糊对象 = 一条 Task；不要把「财务部+人事部」塞进同一 `SignId` 逗号串 |

---

## 3. 字段分类（`sf_task`）

按设计职责分成六袋。**主体身份袋**是本专题核心；其余袋与主体正交，勿混作权限键。

### 3.1 主体身份袋（谁来办）

| 属性 | 列 | 含义 |
|------|-----|------|
| **`SignKind`** | `tk_sign_kind` | 办理对象类型：人 / 部门 / 角色 / 组 / 自定义 |
| **`SignId`** | `tk_sign_id` | 对象引用键：人为用户编码；非人为平台 Dept/Role/Group/Custom Id |
| **`SignName`** | `tk_sign_name` | 展示名快照 |

```text
SignKind=User , SignId=zhangsan     → 精确到张三
SignKind=Dept , SignId=org_finance  → 财务部办理池（任一有权者可办）
SignKind=Role , SignId=duty_auditor → 审计岗角色池
SignKind=Group, SignId=wg_night     → 夜班工作群
SignKind=Custom, SignId=pool:urgent → 宿主自定义池码
```

### 3.2 办理分组袋（Leave 用 · R1）

| 属性 | 列 | 含义 |
|------|-----|------|
| `GroupId` | `tk_group_id` | 办理组别；组内抢办/协办/队长只读此维 |
| `GroupName` | `tk_group_name` | 组展示名 |
| `GroupRole` | `tk_group_role` | 组内身份码（Leader/Member…） |

**注意：** `SignKind=Group` 的「主体是组」≠ `GroupId`「Leave 分组键」。前者回答「任务挂给谁」；后者回答「多人/多组如何算离开」。可同值双写，语义仍正交。

### 3.3 企业单位袋（展示 / 筛选 · R1）

| 属性 | 列 | 含义 |
|------|-----|------|
| `UnitId` / `UnitCode` / `UnitName` | `tk_unit_*` | 办理单位；列表筛、对接；**Leave 不读** |

与流程实例 `StartUnit*`（发起单位）分离，互不覆盖。

### 3.4 席位与计权袋

| 属性 | 列 | 含义 |
|------|-----|------|
| `AssigneeRole` | `tk_assignee_role` | 0=审批办理人；1=沟通参与者（无推进权） |
| `Weight` | `tk_weight` | 票权；默认 1 |
| `ProxySignId` | `tk_proxy_sign_id` | 委托代批所代理的根席；空=自己是根 |
| `ConsultSrcTaskId` | `tk_consult_src_task_id` | 沟通参与者挂靠的源审批 Task |
| `EntrustedTaskId` | `tk_entr_task_id` | 委派链回指原 Task |

### 3.5 业务挂接袋

| 属性 | 列 | 含义 |
|------|-----|------|
| `BizChannel` / `BizPK` | `tk_biz_*` | 业务通道与单据主键冗余 |
| `ProcessId` / `ActivityId` / `ActivityName` | `tk_*` | 定义键与名称快照 |
| `RowPara` | `tk_row_para` | 行级扩展（窄字符串，勿塞组织树） |

### 3.6 生命周期袋（非主体）

| 属性 | 含义 |
|------|------|
| `TaskState` / `EndType` / `EndText` | 开闭态与办结原因 |
| `SendAt` / `GotAt` / `ActiveAt` / `DueAt` / `CompletedAt` | 个人时钟 |
| `IsRead` / `ReadAt` / `Msg` | 已阅与意见 |
| `EndedUserId` / `EndedUserName` | **实际办结人**（模糊主体场景必填真人） |
| `Created*` / `Updated*` / `RecordStatusInvalid` | 审计与软删 |

---

## 4. 概念对照（勿混用）

| 术语 | 是 | 不是 |
|------|----|------|
| **办理主体** | Task 上 `SignKind`+`SignId` 所指对象 | `TaskId`；也不是 Runner.`UserId`（命令发起人） |
| **精确人** | Kind=User 且 SignId=用户编码 | 「名单里有很多人」——那是多行 Task |
| **模糊主体** | Kind∈{Dept,Role,Group,Custom}，成员由业务主数据解释 | 引擎内嵌成员展开表 |
| **办理池** | 业务中心按维度过滤后的可见 Task 集 | 引擎 Leave 判定本身 |
| **Performer** | Resolve/Filter 载荷 → 落库 Task 主体与维度 | 抄送人 / 通知人（R5） |
| **IsMine** | 精确人路径的引擎鉴权 | 模糊主体的池可见性判定 |

```text
命令发起人 Runner.UserId
        ≠
办理主体   Task.SignId（+Kind）
        ≠
实际办结人 Task.EndedUserId   （模糊池场景：办的人 ∈ 池权限，未必 = SignId）
```

---

## 5. 场景分析

### 5.1 精确指派（默认主路径）

- **设置**：`SignKind=User`，`SignId=账号`。  
- **可见**：工作台「我的待办」=`SignId==当前用户`。  
- **办理**：`IsMine` 通过后 `Run` / 转办 / 加签等。  
- **适用**：责任清晰、可追责到个人；常规 OA。

### 5.2 多人精确名单 + 抢办 / 会签

- **设置**：同一 AI 下 N 条 User Task；Leave=`Race` / `Collaborate` / `Group*`。  
- **与模糊池区别**：人名单在发送时已封闭；引擎关旁人 / 计权均有明确 SignId。  
- **适用**：已知候选集，且人数可控。

### 5.3 模糊部门 / 岗位池（时效优先 · 用户口述主场景）

```text
业务要求：7 节点 × 一上午必须走完
设计侧：各节点只配「约属哪个部门 / 群组 / 岗位」
发送侧：SignKind=Dept|Role|Group，SignId=组织键；通常 1 条 Task / 节点
展示侧：业务中心「办理池」列出打开的模糊任务
权限侧：按用户部门、岗位、角色、群组成员属性过滤
办理侧：有权用户打开 → 提交同意 → EndedUserId=真人；Leave 常 Race（一人办过即走）
```

| 优点 | 代价 / 约束 |
|------|-------------|
| 不卡在「人选不出」 | 审计看 `EndedUser*`，不能只盯 `SignId` |
| 名单自然随组织变动 | 可见性规则在宿主，须与主数据一致 |
| 与抢办语义契合 | 并发双办需 Leave=Race + 关闭旁人（单模糊行时旁人=无） |

**产品建议：** 池内办理可先「打开/认领」再批（产品可选 Claim），但**认领不是主体模型的一部分**——主体仍是部门/角色；认领只是把短时独占写到运行态（若做，另案动词，勿改 SignKind）。

### 5.4 角色 / 职务相对人

- Resolve：`RoleGroup` / `JobRelative` 等算出人或直接落 Role 主体。  
- **两种落法**：① 展开为多 User Task（精确）；② 保留 `SignKind=Role` 一条（模糊池）。  
- 选型看「是否必须事前锁人」：合规锁人用 ①；时效池用 ②。

### 5.5 组间模式（Group* Leave）

- 多单位并行收件：多 Task，各带 `GroupId`（可与 Unit 双写），Leave=`GroupRace`/`GroupCollaborate`。  
- **主体仍可以是人**；Group* 只服务「组内/组间何时离开」。  
- 若再叠加模糊主体：每组一条 Dept Task + GroupId=单位，业务池按「我所属单位 ∩ 任务 Unit/Group」过滤。

### 5.6 沟通参与者 / 委托代批

- `AssigneeRole=Participant`：主体仍是人，但无推进权。  
- 代批：`ProxySignId` 挂根席；EffectiveWeight 同根去重（权重专题）。  
- **模糊主体一般不走沟通/代批主路径**；先认领成精确人或直接以真人办结。

### 5.7 场景选型速查

| 问自己 | 倾向 |
|--------|------|
| 发送时能否点出具体账号？ | 能 → User；不能 → Dept/Role/Group/Custom |
| 失败是否必须追到个人责任？ | 必须且事前指定 → User；事后追责即可 → 模糊 + EndedUser |
| 候选是否封闭且人少？ | 是 → 多 User + Race；人多/变动大 → 模糊单行 |
| 要不要组间都过才走？ | 要 → 填 Group* + Group Leave；与 SignKind 无关 |
| 列表要按法人单位筛？ | 填 Unit*；勿塞进 GroupId |

---

## 6. 业务中心办理池（职责切分）

```text
┌──────────── 引擎 ────────────┐     ┌──────────── 业务中心 / 宿主 ────────────┐
│ 创建 Task（SignKind+SignId） │ ──► │ 查询打开态模糊/精确任务                 │
│  Leave / 流转 / 关旁人       │     │ 按用户 Dept/Role/Group/岗位属性过滤     │
│  IsMine（仅精确人）          │     │ 展示办理池；授权打开                     │
│  落 EndedUser* / 推进        │ ◄── │ Run 时 Runner.UserId=实际办理人         │
└──────────────────────────────┘     └────────────────────────────────────────┘
```

| 层 | 负责 | 不负责 |
|----|------|--------|
| **引擎** | 存主体与维度；精确人 `IsMine`；流转与 Leave | 解释「用户是否属于某部门」 |
| **业务中心** | 池列表、维度 ACL、入口鉴权 | 改写引擎 Leave 语义 |
| **主数据（宿主）** | Org/User/Role/Job 关系 | 写进 pure schema |

**办理时建议契约：**

1. 业务侧确认：当前用户对 `SignKind`+`SignId`（及 Unit/Group 附加条件）有权。  
2. 调用 `Run` 等动词：`Runner.UserId` = 当前用户；`TaskId` = 池中该行。  
3. 引擎精确人 `IsMine`：对 **User** 任务仍校验 SignId；对 **非 User** 任务，产品层应在进引擎前完成池鉴权（或后续扩展「模糊主体放行钩子」——本期以业务中心前置鉴权为准）。  
4. 成功后 `EndedUserId` 必为真人，供审计与轨迹。

---

## 7. 与 Performer / Resolve 的衔接

`Performer` 今日字段以 User + Group* + Unit* + Weight 为主；落库 Task 时：

| Performer | → Task |
|-----------|--------|
| `UserId` / `UserName` | `SignId` / `SignName`（Kind=User 或未标 Kind） |
| （扩展）主体类型 + 非人引用 | `SignKind` + `SignId`/`SignName` |
| `Group*` / `Unit*` / `Weight` / `ProxySignId` | 同名列 |

**扩展约定（S7）：** 模糊主体由宿主 Resolver/Specific 显式产出 Kind≠User；禁止只写「部门名在 UserName、UserId 空」的灰数据。空人策略仍走 R10 EmptyAction，与模糊主体无关——模糊主体是**有对象、对象非人**，不是空集。

---

## 8. 红线

1. **禁止**用 `TaskId` 当办理人 Id；**禁止**用活动 `SignIds` 做办理权。  
2. **禁止** GroupId 冒充企业单位；单位用 Unit*（R1）。  
3. **禁止**把多部门/多角色塞进一个 `SignId` 逗号串；一主体一行。  
4. **禁止**引擎为模糊主体强制展开全员 Task（名单爆炸、与池模型对打）。  
5. **禁止**模糊任务办结不记 `EndedUser*`（审计空洞）。  
6. **禁止**把抄送/通知人写入办理主体（R5）。  
7. **精确人路径**不得绕过 `IsMine`；模糊路径不得假装已是引擎内置成员匹配。

---

## 9. 与相关专题的边界

| 专题 | 分工 |
|------|------|
| [节点实例与任务](./专题-节点实例与任务.md) | AI↔Task 基数、主/子、办理权看 Task |
| [办理人权重与委托](./专题-办理人权重与委托体系.md) | Weight / Proxy / EffectiveWeight |
| [待办人管道](../特性说明-待办人管道.md) | Resolve / Leave / Filter；Group≠Unit |
| [多人办理模式](../OA/专题-多人办理模式.md) | Leave 类型与完成条件 |
| **本专题** | **主体是什么、多维怎么分、精确 vs 模糊池** |

---

## 10. 开放项（不阻塞本契约）

| 项 | 说明 |
|----|------|
| 引擎侧「非 User IsMine / Claim」 | 是否增加模糊主体放行 Gate，避免仅靠业务前置 |
| Performer 一等公民 `SignKind` | 载荷显式 Kind，减少落库猜测 |
| 池认领动词 | 可选 Claim：短时独占，不改 SignKind |
| Custom Kind 码表 | 宿主注册 `pool:*` 等命名空间约定 |

---

## 11. 实现锚点

| 锚点 | 说明 |
|------|------|
| `TaskEntity.SignKind` / `SignId` / `SignName` | 主体身份袋 |
| `SignKindEnum` / `SignKindHelper.IsPerson` | Kind 枚举与人判定 |
| `Performer` Group* / Unit* / Weight | 维度与计权透传 |
| `TaskManager.IsMine` | 精确人：`SignId == userId` |
| `TaskManager` 建待办自 Performer | Group/Unit 自 Performer 或 AI 回落 |
| 待办人管道 R1 / R3 | Group≠Unit；身份键=用户编码 |
