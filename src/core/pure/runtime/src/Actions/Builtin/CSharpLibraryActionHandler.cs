using sailflow.lanes;
using sailflow.utils;
using sailflow.events;
using sailflow.xml;
using ActionEntity = sailflow.xml.Action;

namespace sailflow.runtime.Actions;

internal sealed class CSharpLibraryActionHandler : IActionMethodHandler
{
    public ActionMethodEnum Method => ActionMethodEnum.CSharpLibrary;

    public void Execute(ActionEntity action, IEventService eventService)
    {
        try
        {
            var methodInfo = action.MethodInfo;
            var assemblyFullName = methodInfo.AssemblyFullName;
            var methodName = methodInfo.MethodName;
            var executingPath = ConfigHelper.GetExecutingDirectory();
            var pluginAssemblyName = string.Format("{0}\\{1}\\{2}.dll", executingPath, "plugin", assemblyFullName);
            var pluginAssemblyTypes = System.Reflection.Assembly.LoadFile(pluginAssemblyName).GetTypes();
            Type outerClass = pluginAssemblyTypes
                .Single(t => !t.IsInterface && t.FullName == methodInfo.TypeFullName);
            object instance = Activator.CreateInstance(outerClass)!;
            System.Reflection.MethodInfo mi = outerClass.GetMethod(methodName)!;

            object[]? methodParams = null;
            if (!string.IsNullOrEmpty(action.Arguments.Trim()))
            {
                methodParams = EventUtil.CompositeParameterValues(action.Arguments, eventService);
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
