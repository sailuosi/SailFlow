# saiflow

**English:** saiflow (sailflow) is a plugin-style .NET workflow engine for **.NET 6 / 8 / 10**, with pluggable capabilities via `UseXxx`, multi-database access through **mooSQL**, and clear **pure / ext / biz** layering.

**中文：** saiflow（仓库名 sailflow）是面向 .NET 6 / 8 / 10 的插件式流程引擎：通过 `UseXxx` 挂载能力、经 **mooSQL** 适配多数据库，并按 **pure / ext / biz** 分层。

> 迭代中，暂勿用于生产；可能有 break change。

## Links / 链接

|                        |                                                          |
| ---------------------- | -------------------------------------------------------- |
| **Source / 源码**        | sailflow 仓库                                              |
| **Documentation / 文档** | 仓库内 `doc/sail/`                                          |

## Install / 安装

多数宿主直接引用组合根包：

```bash
dotnet add package saiflow.biz
```

只要引擎骨架（无 MQ / 脚本 / 规则等重依赖）：

```bash
dotnet add package saiflow.pure
```

可选实现层（含产品默认 ext 集合）：

```bash
dotnet add package saiflow.ext
```

## Packages / 包说明

暂时只发布 **3 个聚合包**（多 DLL 入同一 nupkg）。叶子工程（`sailflow.engine` 等）**不单独发包**。

| NuGet 包名 | Role / 说明 |
| ---------- | ----------- |
| **saiflow.pure** | 框架内核：basis / baseHub / AI / engine / assignee；依赖 **mooSQL.Ext** |
| **saiflow.ext** | 可选实现：action / msg / bizRule / form / resource / localize / data（含 IronPython、RabbitMQ、NRules 等） |
| **saiflow.biz** | 产品组合根：biz / biz.AI / biz.docs / biz.query；推荐宿主入口 |

依赖关系：

```text
Host → saiflow.biz → saiflow.ext → saiflow.pure → mooSQL.Ext
```

**未纳入本轮发包**：`sailflow.graph`、`samples/external`、测试项目。

> 注意：叶子 csproj 上可能仍残留历史 `PackageId`（含 `saiflow.data` 冲突）。全仓默认 `IsPackable=false`，请勿对叶子工程执行 `dotnet pack`。

## Pack (maintainers) / 打包

在仓库根目录：

须按依赖顺序打包（`nuget.config` 已把 `./release` 配为本地源，供后续包还原）：

```bash
dotnet pack pack/saiflow.pure.Pack.csproj -c Release
dotnet pack pack/saiflow.ext.Pack.csproj -c Release
dotnet pack pack/saiflow.biz.Pack.csproj -c Release
```

输出目录：仓库根 `release/`。

推送到 NuGet 源（示例，按需替换）：

```bash
dotnet nuget push release/saiflow.*.nupkg --source https://api.nuget.org/v3/index.json --api-key <KEY>
```
