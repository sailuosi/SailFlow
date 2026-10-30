
using System;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using IronPython.Hosting;
using saiflow.data;
using saiflow.hub.Localize;
using saiflow.engine.Common;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl.Common;
using saiflow.engine.Xpdl.Entity;
using saiflow.engine.Event;
using saiflow.WebUtility;
using saiflow.engine.External;

namespace saiflow.engine.Core.Pattern.Auto
{
    /// <summary>
    /// Service Executor
    /// �Զ����񷽷���
    /// </summary>
    internal class ServiceExecutor
    {
        /// <summary>
        /// Execute service list
        /// </summary>
        internal static void ExecuteServiceList(IList<ServiceDetail> serviceList,
            ActivityForwardContext forwardContext,
            IEventService eventService)
        {
            if (serviceList != null && serviceList.Count > 0)
            {
                foreach (var service in serviceList)
                {
                    if (service.Method != ServiceMethodEnum.None)
                    {
                        Execute(service, forwardContext, eventService);
                    }
                }
            }
        }

        /// <summary>
        /// Execute
        /// </summary>
        private static void Execute(ServiceDetail service, 
            ActivityForwardContext forwardContext,
            IEventService eventService)
        {
            if (service.Method == ServiceMethodEnum.LocalService)
            {
                ExecuteLocalService(service, eventService);
            }
            else if (service.Method == ServiceMethodEnum.CSharpLibrary)
            {
                ExecuteCSharpLibraryMethod(service, eventService);
            }
            else if (service.Method == ServiceMethodEnum.WebApi)
            {
                ExecuteWebApiMethod(service, eventService);
            }
            else if (service.Method == ServiceMethodEnum.StoreProcedure)
            {
                ExecuteStoreProcedureMethod(service, eventService);
            }
            else
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.Execute.exception", service.Method.ToString()));
            }
        }

        /// <summary>
        /// Execute Local Service
        /// </summary>
        private static void ExecuteLocalService(ServiceDetail service, IEventService eventService)
        {
            try
            {
                //�Ȼ�ȡ����ʵ����
                //First, obtain the specific implementation class
                var instance = ReflectionHelper.GetSpecialInstance<IExternalService>(service.Expression);
                //�ٵ��û����ִ�з���
                //Call the base class executable method again
                var executableInstance = instance as IExecutable;
                executableInstance.Executable(eventService);
            }
            catch (Exception ex)
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.ExecuteLocalService.exception", ex.Message));
            }
        }

        /// <summary>
        /// Execute WebApi Method
        /// </summary>
        private static void ExecuteWebApiMethod(ServiceDetail service, IEventService eventService)
        {
            try
            {
                object result = null;
                if (service.SubMethod == SubMethodEnum.HttpGet)
                {
                    var jsonGetValue = eventService.GetVariableByScopePriority(service.Arguments);
                    var url = string.Format("{0}/{1}", service.Expression, jsonGetValue);
                    var httpGetClient = HttpClientHelper.CreateHelper(url);
                    result = httpGetClient.Get();
                }
                else if (service.SubMethod == SubMethodEnum.HttpPost)
                {
                    string url = service.Expression;
                    var httpClientHelper = HttpClientHelper.CreateHelper(url);
                    var jsonValue = EventUtil.CompositeJsonValue(service.Arguments, eventService);
                    var httpPostClient = HttpClientHelper.CreateHelper(url);
                    result = httpClientHelper.Post(jsonValue);
                }
                else if (service.SubMethod == SubMethodEnum.HttpPut)
                {
                    string url = service.Expression;
                    var httpClientHelper = HttpClientHelper.CreateHelper(url);
                    var jsonValue = EventUtil.CompositeJsonValue(service.Arguments, eventService);
                    var httpPostClient = HttpClientHelper.CreateHelper(url);
                    result = httpClientHelper.Put(jsonValue);
                }
                else if (service.SubMethod == SubMethodEnum.HttpDelete)
                {
                    var jsonGetValue = eventService.GetVariableByScopePriority(service.Arguments);
                    var url = string.Format("{0}/{1}", service.Expression, jsonGetValue);
                    var httpGetClient = HttpClientHelper.CreateHelper(url);
                    result = httpGetClient.Delete();
                }
            }
            catch (Exception ex)
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.ExecuteWebApi.exception", ex.Message));
            }
        }

        /// <summary>
        /// Execute Store procedure
        /// </summary>
        private static void ExecuteStoreProcedureMethod(ServiceDetail service, IEventService eventService)
        {
            try
            {
                var parameters = EventUtil.CompositeSqlParametersValue(service.Arguments, eventService);
                var procedureName = service.Expression;
                var kit = eventService.GetKit();
                SailCmdRunner.ExecuteProc(kit, procedureName, parameters);
            }
            catch (Exception ex)
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.ExecuteStoreProcedureMethod.exception", ex.Message));
            }
        }

        /// <summary>
        /// Execute library
        /// </summary>
        private static void ExecuteCSharpLibraryMethod(ServiceDetail service, IEventService eventService)
        {
            try
            {
                //ȡ����ǰӦ�ó���ִ��·��
                //Retrieve the current application execution path
                var methodInfo = service.MethodInfo;
                var assemblyFullName = methodInfo.AssemblyFullName;
                var methodName = methodInfo.MethodName;
                var executingPath = ConfigHelper.GetExecutingDirectory();
                var pluginAssemblyName = string.Format("{0}\\{1}\\{2}.dll", executingPath, "plugin", assemblyFullName);
                var pluginAssemblyTypes = Assembly.LoadFile(pluginAssemblyName).GetTypes();
                Type outerClass = pluginAssemblyTypes
                                        .Single(t => !t.IsInterface && t.FullName == methodInfo.TypeFullName);
                //object instance = Activator.CreateInstance(outerClass, parameters.ConstructorParameters);
                object instance = Activator.CreateInstance(outerClass);
                System.Reflection.MethodInfo mi = outerClass.GetMethod(methodName);

                object[] methodParams = null;
                if (!string.IsNullOrEmpty(service.Arguments.Trim()))
                {
                    methodParams = EventUtil.CompositeParameterValues(service.Arguments, eventService);
                }
                var result = mi.Invoke(instance, methodParams);
            }
            catch (Exception ex)
            {
                throw new WorkflowException(LocalizeMessagesLocator.Current.GetEngineMessage("actionexecutor.ExecuteCSharpLibraryMethod.exception", ex.Message));
            }
        }
    }
}
