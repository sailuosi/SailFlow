using sailflow.data;
using sailflow.engine.Xpdl;

namespace sailflow.engine.Pattern;

/// <summary>
/// B · 网关 Mediator 执行表面（工厂 Create 产物）。
/// </summary>
public interface INodeMediatorGateway
{
    Activity GatewayActivity { get; }

    ActivityInstanceEntity GatewayActivityInstance { get; set; }
}
