using System;
using sailflow.action.Python;
using sailflow.engine.Client;
using sailflow.engine.Hosting;

namespace sailflow.action.Hosting
{
    /// <summary>
    /// 注册 sailflow.action 扩展（IronPython Action/Script）。
    /// <para>
    /// 不含连线条件求值：<see cref="PythonConditionEvaluator"/> 须显式
    /// <c>UseNextStepRoute(r => r.AddConditionEvaluator&lt;PythonConditionEvaluator&gt;(…))</c>，
    /// 切勿与 ScriptMethod 混用。
    /// </para>
    /// </summary>
    public static class ActionSailExtensions
    {
        /// <summary>挂载 Python Action + Script 处理器到 Builder（不含条件 Evaluator）。</summary>
        public static SailClientBuilder UseIronPython(this SailClientBuilder builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.AddActionMethod(new PythonActionHandler());
            builder.AddScriptMethod(new PythonScriptHandler());
            return builder;
        }

        /// <summary>挂载 Python Action + Script 处理器到服务集合。</summary>
        public static ISailCollection UseIronPython(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);
            services.AddActionMethod(new PythonActionHandler());
            services.AddScriptMethod(new PythonScriptHandler());
            return services;
        }

        /// <summary>在已构建的 Client 上挂载 Python 处理器。</summary>
        public static SailClient UseIronPython(this SailClient client)
        {
            ArgumentNullException.ThrowIfNull(client);
            client.addActionMethod(new PythonActionHandler());
            client.addScriptMethod(new PythonScriptHandler());
            return client;
        }
    }
}
