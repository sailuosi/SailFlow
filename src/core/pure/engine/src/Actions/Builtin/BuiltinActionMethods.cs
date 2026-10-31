namespace saiflow.engine.Actions
{
    /// <summary>
    /// 引擎内置 Action/Script/Service 方法（不含 Python；Python 由 sailflow.action 扩展）。
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
    }
}
