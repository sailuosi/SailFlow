namespace saiflow.engine.Common;

/// <summary>
/// B · 命令入参 Context 的规范类型名（L-Cmd）。
/// <para>
/// 与 <see cref="WfAppRunner"/> 同族：本类继承之，便于 API/文档统一称呼「入参袋」。
/// <see cref="Factories.RunBuilder.Build"/> / <see cref="Factories.RunBuilder.BuildCommandContext"/> 产出本袋。
/// </para>
/// <para>
/// <b>边界</b>：入参袋 ≠ 执行编排袋。
/// Runtime（<c>WfRuntimeManager*</c>）与 Mediator（<c>NodeMediator*</c>）由工厂另一次 Create，
/// 持导航态执行；二者都不是本类型。
/// </para>
/// </summary>
public class FlowCommandContext : WfAppRunner
{
}
