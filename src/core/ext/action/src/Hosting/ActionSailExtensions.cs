using System;
using saiflow.action.Python;
using saiflow.engine.Client;
using saiflow.engine.Client.Hosting;

namespace saiflow.action.Hosting
{
    /// <summary>
    /// 注册 sailflow.action 扩展（IronPython Action/Script）。
    /// </summary>
    public static class ActionSailExtensions
    {
        /// <summary>挂载 Python Action + Script 处理器到 Builder。</summary>
        public static SailClientBuilder UseIronPython(this SailClientBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.UseActionMethod(new PythonActionHandler());
            builder.UseScriptMethod(new PythonScriptHandler());
            return builder;
        }

        /// <summary>挂载 Python Action + Script 处理器到服务集合。</summary>
        public static ISailCollection UseIronPython(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.UseActionMethod(new PythonActionHandler());
            services.UseScriptMethod(new PythonScriptHandler());
            return services;
        }

        /// <summary>在已构建的 Client 上挂载 Python 处理器。</summary>
        public static SailClient UseIronPython(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            client.useActionMethod(new PythonActionHandler());
            client.useScriptMethod(new PythonScriptHandler());
            return client;
        }
    }
}
