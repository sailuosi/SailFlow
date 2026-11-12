using sailflow.assignee;
using sailflow.assignee.Hosting;
using sailflow.biz.Assignee;
using sailflow.engine.Client;
using sailflow.engine.Hosting;
using sailflow.engine.Common;

namespace sailflow.biz.Hosting;

/// <summary>Biz 组合：默认启用办理人管道 + S4 业务样例注册。</summary>
public static class BizAssigneeSailExtensions
{
    /// <summary>挂载内置 Resolve/Leave，并注册业务样例（可再后写覆盖）。</summary>
    public static ISailCollection UseBizAssignees(this ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.UseAssignees();
        services.AddFeature(new SailFeatureDescriptor
        {
            Name = SailFeatureNames.BizAssigneeSamples,
            Order = 41,
            Phase = SailFeaturePhase.ConfigureClient,
            DependsOn = new[] { SailFeatureNames.Assignee },
            ConfigureClient = (client, _) => RegisterBizSamples(client)
        });
        return services;
    }

    public static SailClientBuilder UseBizAssignees(this SailClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.Services.UseBizAssignees();
        return builder;
    }

    /// <summary>已 Build 的 Client 上启用。</summary>
    public static SailClient UseBizAssignees(this SailClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.UseAssignees();
        return RegisterBizSamples(client);
    }

    /// <summary>
    /// S4 样例：Business.ClassStudents Type；ClassStudents 回调名；TenantCollaborate 同码覆盖 Collaborate。
    /// </summary>
    public static SailClient RegisterBizSamples(SailClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        client.useAssigneeResolver(new ClassStudentsAssigneeResolver());
        client.useLeaveJudge(new TenantCollaborateLeaveJudge());
        client.useAssigneeBusinessCallback("ClassStudents", ctx =>
        {
            // 回调名用法：AssigneeResolve Type=BusinessCallback, Params.callback=ClassStudents
            var nested = ConfigPair.Create(ClassStudentsAssigneeResolver.TypeCode, ctx.Config.Params);
            return new ClassStudentsAssigneeResolver().Resolve(new AssigneeResolveContext
            {
                Request = ctx.Request,
                Config = nested
            });
        });
        return client;
    }
}
