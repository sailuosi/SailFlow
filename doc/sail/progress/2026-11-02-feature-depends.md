# 2026-11-02 · Feature DependsOn / 已装能力探针

> 状态：已落地  
> 对照：Elsa Feature+DependsOn 务实落点（不搬 Module 全家桶）

## 摘要

- `SailFeatureDescriptor.DependsOn` + `SailFeatureDependencyValidator`（`Build()` 开头校验）
- `ProvideFeature` / `SailFeatureNames`：Property 插槽与约定能力名
- `SailClient.InstalledFeatures` / `HasFeature`
- 宿主 `GET api/Wf2Xml/ProbeInstalledFeatures`
- 关键依赖：Outbox/邮件/立刻归档→`json`；Schema/sail.data→`database`；biz 样例→`assignee`

## 验收

- [x] 引擎单测 `SailFeatureDependencyTests`
- [ ] 宿主启动后 P3 探针（联调清单）

## 文档

- `业务管道特性设计.md` §4.2
- `最佳实践-宿主能力包UseXxx.md` 规则 6
- `联调清单-产品缺口.md` P3
