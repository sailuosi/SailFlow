# 审计：sailflow 相对常见 OA 引擎流程能力对照

> 状态：**只读审计结论**（2026-11-14；**2026-11-15 对照代码复核**；**同日二次抽检维持**）  
> 用途：记录相对常见钉钉式 / 中国式 OA 工作流引擎的流程能力对齐快照；**不直接当实施清单**。  
> 口径：  
>
> - **Sailflow** = 引擎仓 + 文档里约定的 Host（`sleveen-admin`）产品接线  
> - **常见 OA 引擎** = 市面常见仿钉钉审批 / 低代码 OA 工作流（对照样本含 .NET 实现）；声明有但运行时未接线的标为「部分」  
> - **🟡 引擎有未产品化**：引擎 `*Process` / Action 已落地，Host 码表 / Handler / UX 未挂  
> 关联：[特性说明-引擎能力基线.md](../sail/特性说明-引擎能力基线.md) · [特性说明-中国式OA审批要素.md](../sail/特性说明-中国式OA审批要素.md) · [存疑参考功能点.md](../sail/存疑参考功能点.md) · [todo-产品缺口.md](../sail/todo-产品缺口.md) · [专题-流转动作骨架.md](../sail/专题-流转动作骨架.md)（A1–A4） · [指南-待办人预选场景.md](../helps/指南-待办人预选场景.md)

按**流程能力**对照（不含前端壳、部署运维细节）。

图例：

| 标记 | 含义 |
|------|------|
| ✅ | 有（已落地可接） |
| 🟡 | 部分 / 弱 / 枚举有但未接线 / 引擎有未产品化 |
| ❌ | 无 |
| — | 无对等产品表达（用其他动词覆盖） |

表头缩写：**OA** = 常见 OA 引擎侧。

---

## 办理动词（审批动作）

| 特性 | Sailflow | OA | 说明 |
|------|:--------:|:--:|------|
| 发起 / 提交 | ✅ | ✅ | 两边都有；Sail=`StartProcess` |
| 同意 / 通过 | ✅ | ✅ | Sail=`RunProcess`；OA 侧多为同意/审批通过按钮 |
| 驳回 / 不同意 | ✅ | ✅ | 语义略不同：Sail 的 Reject ≠ 整单作废 |
| 退回 / 打回修改 | ✅ | ✅ | OA 侧常见多型 Disagree；Sail 用「目标 × 再发送」两维 |
| 退回任意历史节点 | ✅ | 🟡 | Sail=`SendBack` + 历史树；OA 侧多靠 Disagree 类型 + 指定节点 |
| 撤回 | ✅ | 🟡 | Sail 引擎稳定 + 码表已开；OA 侧常有按钮声明，部分实现仓未接线 |
| 转办 | ✅ | ✅ | Sail=`TransferProcess`（码表+Handler 已接线；联调 §F 待勾）；≠ 委派 |
| 委托 / 委派 | 🟡 | 🟡 | Sail 引擎=`DelegateProcess`（A4，办结自动回转）；Host 码表**暂缓**；另有遗留 `EntrustTask` |
| 变更处理人 | 🟡 | ✅ | Sail 引擎=`ChangeAssigneeProcess`（A4）；Host 码表 / Handler **未挂**；OA 侧常见管理改派 |
| 承办（或签先认领） | ❌* | ✅ | Sail **刻意不跟**；用 Leave=`Race` 表达抢办 |
| 加签（当前节点加人） | ✅ | 🟡 | Sail=`SignForward`；OA 侧常有声明，部分实现未接线 |
| 加批（生新审批段） | 🟡 | ✅ | OA 侧加批完整；Sail 与加签揉在一起，未拆 |
| 减签 | 🟡 | 🟡 | Sail 引擎=`RemoveAssigneeProcess`（A4）；Host 办理条**待产品**；OA 侧常有枚举、adaptor 未齐 |
| 未来节点加/减/换人 | — | 🟡 | 无一等 `*FutureAssignee` 动词；主场景用 **预选表 + `Preselect` Resolve** 覆盖（改预选=换人）；见 [指南-待办人预选场景.md](../helps/指南-待办人预选场景.md) |
| 跳转（自由跳） | ✅ | 🟡 | Sail=`JumpProcess`；OA 侧引擎可跳，常无独立产品按钮 |
| 转发 | 🟡 | ✅ | OA 侧常见转发；Sail 主通道归抄送（`Copyto`，非 FlowOperation） |
| 终止 / 作废 | 🟡 | ✅ | Sail=`Terminate`/`Discard` 引擎+Host Handler 有，码表默认关；OA 侧常见 |
| 取消 | 🟡 | ✅ | Sail=`CancelProcess`（Lifecycle）；Host 码表 / Handler **未挂**（todo 与 Suspend 同列暂缓） |
| 关闭 | ✅ | — | Sail 管理向；OA 侧多用终止/作废表达结束 |
| 挂起 / 恢复 | 🟡 | 🟡 | Sail=`SuspendProcess`/`ResumeProcess` 引擎有、码表暂缓；≠ WaitToken 统一 Resume |
| 重发 / 返送退回人 | ✅ | 🟡 | Sail 特色 `ResendProcess`；OA 侧靠「打回后重新提交」组合 |
| 修订 / 反转 | 🟡 | ❌ | Sail 有、码表默认关；OA 侧少见对等动词 |
| 催办 | 🟡 | 🟡 | Sail 引擎=`UrgeProcess`（A4，不改状态）；Host 未挂；≠「加急」优先级 |
| 沟通 / 征询 | ✅ | 🟡 | Sail=`Consult`+参与者 `AssigneeRole=1`+等待锁；阅办=`Acknowledge`；收回=`RecallConsult`；≠ 抄送 |

 承办：Sail 文档明确「默认不抄常见 OA 引擎按钮」，不算缺口债。

---

## 多人办理模式

| 特性 | Sailflow | OA | 说明 |
|------|:--------:|:--:|------|
| 会签 | ✅ | ✅ | Sail=`SignTogether` / `Collaborate` Leave |
| 或签 | ✅ | ✅ | Sail=`Race`；OA 侧多为或签 SignType |
| 顺序会签 | ✅ | ✅ | Sail=`MergeType.Sequence` |
| 组间抢办 / 组间协办 | ✅ | ❌ | Sail Leave 独有维度 |
| 队长关卡 | ✅ | ❌ | Sail=`Captain` |
| 真并行会签 | 🟡 | ✅ | `MergeType.Parallel` 有路径；基线仍记语义债 |
| 审批人去重 | 🟡 | ✅ | OA 侧常有去重配置；Sail 走 Filter/空人策略，非一等产品开关 |
| 层层审批 / 循环节点 | 🟡 | ✅ | OA 侧 Loop/层层审批适配器较齐；Sail 靠 Resolve 扩展（岗位相对等） |

---

## 图与节点能力

| 特性 | Sailflow | OA | 说明 |
|------|:--------:|:--:|------|
| 条件 / 排他分支 | ✅ | ✅ | |
| 并行网关 | ✅ | ✅ | |
| 包容网关 | 🟡 | ❌ | Sail 解析有，运行近似 OR（Inclusive→Or）；OA 侧少见 |
| 抄送节点 | 🟡 | ✅ | OA 侧常为一等节点；Sail 独立产品表，不进算人 |
| 子流程 | ✅ | ❌/弱 | Sail 有 Nested；OA 侧模型多偏扁平钉钉图 |
| 服务 / 脚本 / 规则 / AI 节点 | ✅ | ❌/弱 | Sail Mediator + UseXxx；OA 侧偏表单+人员 |
| Timer / Message / Signal | ✅ | ❌ | Sail BPMN 事件类；OA 侧少见定时器节点 |
| 流程定义多版本 | ✅ | ✅ | |
| 流程预览（下一跳） | ✅ | ✅ | |
| 标准 BPMN 文件读写 | ✅ | ❌ | Sail 有 `sf:` 扩展；OA 侧多为自有 conf 模型 |

---

## 算人 / 表单 / 消息 / 扩展

| 特性                         | Sailflow | OA  | 说明                                            |
| -------------------------- | :------: | :-: | --------------------------------------------- |
| 指定人 / 角色 / 发起人自选           | ✅        | ✅   | 含多节点预选（`Preselect`）；≠ 下方「未来节点动词」                 |
| 直属领导 / HRBP / 业务表 / 外部传入   | 🟡       | ✅   | OA 侧人员适配器产品化更全；Sail 用回调/表达式/宿主 Lookup         |
| 空人 / 同人 / 超时自动过            | ✅        | 🟡  | Sail R10 锚点+Host Job；OA 侧常有超时提醒配置，定时 Job 不一定齐 |
| 低代码表单                      | 🟡       | ✅   | OA 侧成熟；Sail 字段元数据+Host 动态表                    |
| DIY 自定义表单插件                | 🟡       | ✅   | OA 侧表单操作适配器成熟；Sail=`FormBiz` 宿主接线中            |
| 外部系统 / SaaS 接入             | 🟡       | ✅   | OA 侧多模式接入文档齐全；Sail 偏引擎嵌入                      |
| 消息通知                       | ✅        | ✅   |                                               |
| Gate / Hook / Outbox 可靠副作用 | ✅        | 🟡  | Sail 三线分离成熟；OA 侧有回调/消息，少见同级 Outbox 模型         |
| 审批意见                       | ✅        | ✅   | Sail=`sf_task.tk_msg`（`Msg`）；亦可 Host/`Conditions.comment` |
| 附件                         | ❌/弱      | 🟡  | 两边都非强一等公民                                     |
| 加急（优先级）                    | 🟡       | ❌   | Sail=`sf_task.tk_idx`（`Idx`）已落列；产品按钮仍弱；**勿与催办** `Urge` **混称** |
| 分表 / 冷热归档                  | ✅        | ❌   | Sail=`ProcessArchiveService` + HotShard；运维向能力 |

---

## 对齐结论（一句话）

| 维度 | 态势 |
|------|------|
| **中国式办理动词** | 核心路径（同意/驳回/退回/转办/会签或签）大致对齐；常见 OA 引擎更偏钉钉产品按钮集，Sail 更偏引擎命令 + 正交 Leave |
| **Sail 明显强** | BPMN 事件节点、子流程、跳转/撤回产品化、Assignee 管道（组/队长）、Gate-Hook-Outbox、多库/分表、插件分层；Assignee 动词引擎面（改派/减签/委派/催办）已齐、待 Host |
| **常见 OA 引擎明显强** | 加批、承办、人员策略产品化、低代码/DIY/外部接入一体化、审批意见闭环；管理改派等产品按钮开箱更齐 |
| **两边都弱 / 名义有实装薄** | 挂起恢复产品化、加急优先级；OA 侧撤回/加签减签在部分实现仓多为枚举未接线 |
| **刻意不对齐** | 承办（Sail 不跟）；退回五型（Sail 改两维）；加批 vs 加签（Sail 尚未拆）；未来节点改人走预选表而非 `*FutureAssignee` 动词 |

若只盯「钉钉式 OA 开箱按钮」，常见 OA 引擎产品面更齐；若盯「可嵌入、可扩展的流程引擎能力」，Sailflow 在图语义、插槽与可靠性上更完整。Assignee 侧引擎命令（减签/改派/委派/催办）已于 2026-11-15 落地，**产品空位主要在 Host 码表与办理条**。

---

## 复核纪要（2026-11-15）

对照 `src/core/pure/engine`（`FlowOperation` / `*Action` / `WorkflowService.ProcessExecution`）与 Host `buttonsCatalog.ts` / `Wf2XmlController` / `SailOperationHostingExtensions`：

| 原标记（11-14） | 复核后 | 证据要点 |
|-----------------|--------|----------|
| 变更处理人 ❌ | **🟡** | `ChangeAssigneeProcess` + `ChangeAssigneeAction`；Host 无 API/码表 |
| 减签 ❌ | **🟡** | `RemoveAssigneeProcess` + `RemoveAssigneeAction`；Host 办理条待产品 |
| 委托 🟡「Entrust 痕迹」 | **🟡**（措辞更新） | 一等 `DelegateProcess`/`DelegateAction`；遗留 `EntrustTask` 并存；Host 暂缓 |
| 加急 ❌ | **拆行** | 加急（优先级）仍 ❌；催办 `UrgeProcess` 另列 🟡 |
| 转办 / 终止作废 / 挂起 / 修订反转 | 维持 | 与码表 `defaultEnable`、todo 联调状态一致 |
| 包容网关 / 加批 / 真并行会签 / 抄送侧路 / 归档分表 | 维持 | 与 XPDL/Mediator、`ProcessArchiveService`、基线债表一致 |

**未改结论的项**（抽检通过）：核心 Transition 动词、Leave 五型、子流程/BPMN 事件、Gate-Hook-Outbox、承办刻意不跟。

**二次抽检（同日）**：主表 ✅/🟡/❌ 与代码一致；补「取消」行；转办说明改为「接线已归档、联调 §F 待勾」（避免把未联调误读成未接线）。沟通三件套（`Consult`/`Acknowledge`/`RecallConsult`）码表+Handler 齐，维持 ✅。

**口径修正（同日）**：未来节点加/减/换人 ❌ → **—**（无对等动词，预选表覆盖主场景）；与「发起人自选 ✅」分工写清，避免把预选当成能力缺口。

**连带债（非本审计改基线）：**

- `特性说明-引擎能力基线.md` §3 命令表尚未补 `RemoveAssignee` / `ChangeAssignee` / `Delegate` / `Urge`（A4）；`CancelProcess` 已在基线、本表此前漏列已补。
- `特性说明-中国式OA审批要素.md` §4 委派现状仍写「引擎落地中」→ 宜改为「引擎已落地；Host 待产品」。
- `存疑参考功能点.md` §1 仍写转办「尚缺命令」→ 过时（`TransferProcess` 已有）。

---

## 修订记录

| 日期         | 说明                                                          |
| ---------- | ----------------------------------------------------------- |
| 2026-11-14 | 初版：从对话对照结论原样归档                                              |
| 2026-11-15 | **对照代码复核**：改派/减签升 🟡；委派措辞对齐 `DelegateProcess`；催办与加急拆分；补复核纪要 |
| 2026-11-15 | **二次抽检**：主表维持；补取消行；转办措辞澄清；记关联文档口径漂移 |
| 2026-11-15 | 未来节点加/减/换人 ❌→**—**：预选表覆盖；对齐结论「两边都弱」去掉该项 |
