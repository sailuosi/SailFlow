using System;
using System.Linq;
using System.Reflection;
using saiflow.engine.Common;
using saiflow.engine.Event;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Actions
{
    internal sealed class CSharpLibraryServiceHandler : IServiceMethodHandler
    {
        public ServiceMethodEnum Method => ServiceMethodEnum.CSharpLibrary;

        public void Execute(ServiceDetail service, IEventService eventService)
        {
            try
            {
                var methodInfo = service.MethodInfo;
                var assemblyFullName = methodInfo.AssemblyFullName;
                var methodName = methodInfo.MethodName;
                var executingPath = ConfigHelper.GetExecutingDirectory();
                var pluginAssemblyName = string.Format("{0}\\{1}\\{2}.dll", executingPath, "plugin", assemblyFullName);
                var pluginAssemblyTypes = Assembly.LoadFile(pluginAssemblyName).GetTypes();
                Type outerClass = pluginAssemblyTypes
                    .Single(t => !t.IsInterface && t.FullName == methodInfo.TypeFullName);
                object instance = Activator.CreateInstance(outerClass);
                System.Reflection.MethodInfo mi = outerClass.GetMethod(methodName);

                object[] methodParams = null;
                if (!string.IsNullOrEmpty(service.Arguments.Trim()))
                {
                    methodParams = EventUtil.CompositeParameterValues(service.Arguments, eventService);
                }
                mi.Invoke(instance, methodParams);
            }
            catch (Exception ex)
            {
                throw new WorkflowException(eventService.Sail.Messages.GetEngineMessage(
                    "actionexecutor.ExecuteCSharpLibraryMethod.exception", ex.Message));
            }
        }
    }
}
