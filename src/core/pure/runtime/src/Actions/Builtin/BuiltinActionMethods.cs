using sailflow.lanes;
using sailflow.hosting;

namespace sailflow.runtime.Actions;

/// <summary>
/// pure 内置 Action/Script/Service 方法（WebApi / SQL / 存储过程 / LocalService / CSharpLibrary）。
/// 不含 Python（见 sailflow.action）。经 <c>UseSailRuntimeDefaults</c> 挂载，不随 engine 自动注册。
/// </summary>
public static class BuiltinActionMethods
{
    public static void RegisterAll(
        ActionMethodRegistry actions,
        ScriptMethodRegistry scripts,
        ServiceMethodRegistry services)
    {
        actions.Register(new LocalServiceActionHandler());
        actions.Register(new CSharpLibraryActionHandler());
        actions.Register(new WebApiActionHandler());
        actions.Register(new SqlActionHandler());
        actions.Register(new StoreProcedureActionHandler());

        scripts.Register(new SqlScriptHandler());

        services.Register(new LocalServiceServiceHandler());
        services.Register(new CSharpLibraryServiceHandler());
        services.Register(new WebApiServiceHandler());
        services.Register(new StoreProcedureServiceHandler());
    }

    public static SailClient RegisterAll(SailClient client)
    {
        ArgumentNullException.ThrowIfNull(client);
        RegisterAll(client.ActionMethods, client.ScriptMethods, client.ServiceMethods);
        return client;
    }

    /// <summary>Builder 期：写入 pending，<see cref="SailClientBuilder.Build"/> 时落到 Client。</summary>
    public static ISailCollection RegisterAll(ISailCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddActionMethod(new LocalServiceActionHandler());
        services.AddActionMethod(new CSharpLibraryActionHandler());
        services.AddActionMethod(new WebApiActionHandler());
        services.AddActionMethod(new SqlActionHandler());
        services.AddActionMethod(new StoreProcedureActionHandler());

        services.AddScriptMethod(new SqlScriptHandler());

        services.AddServiceMethod(new LocalServiceServiceHandler());
        services.AddServiceMethod(new CSharpLibraryServiceHandler());
        services.AddServiceMethod(new WebApiServiceHandler());
        services.AddServiceMethod(new StoreProcedureServiceHandler());
        return services;
    }
}
