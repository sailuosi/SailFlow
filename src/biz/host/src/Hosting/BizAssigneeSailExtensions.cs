using saiflow.assignee.Hosting;
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;

namespace saiflow.biz.Hosting;

/// <summary>Biz 组合：默认启用办理人管道（sailflow.assignee）。</summary>
public static class BizAssigneeSailExtensions
{
    /// <summary>挂载内置 Resolve/Leave（后写可覆盖）。</summary>
    public static ISailCollection UseBizAssignees(this ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        return services.UseAssignees();
    }

    public static SailClientBuilder UseBizAssignees(this SailClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.UseAssignees();
    }

    /// <summary>已 Build 的 Client 上启用。</summary>
    public static SailClient UseBizAssignees(this SailClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.UseAssignees();
    }
}
