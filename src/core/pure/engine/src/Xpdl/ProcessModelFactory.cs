
using System;
using System.Collections.Concurrent;
using System.Xml;
using mooSQL.data;
using saiflow.hub.Localize;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Business.Entity;
using saiflow.engine.Business.Manager;
using saiflow.engine.Xpdl.Node;
using saiflow.engine.Utility;
using saiflow.WebUtility;

namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// Process Model Factory Class
    /// 1.  Four types of process model creation (by process, by process instance, by task view, by subprocess node)
    /// 2.  Distinguish the creation mode of sub processes (handle the situation where sub processes are embedded into the main process)
    /// 流程模型工厂类
    /// 1. 流程模型创建的4种类型(按流程, 按流程实例, 按任务视图, 按子流程节点)
    /// 2. 区分出子流程的创建模式(子流程嵌入到主流程里面的情况处理)
    /// </summary>
    public class ProcessModelFactory
    {
        /// <summary>
        /// In-process cache: key = "processId:version", value = parsed IProcessModel.
        /// Process definitions are immutable at runtime; the cache is invalidated explicitly
        /// when a definition is updated or deleted via ClearCache().
        /// 进程内缓存：key = "processId:version"，避免每次请求重复查 DB 和解析 BPMN XML。
        /// </summary>
        private static readonly ConcurrentDictionary<string, IProcessModel> _modelCache = new();

        /// <summary>
        /// Evict a specific process model from the cache (call after Update/Delete).
        /// 流程定义更新或删除后调用此方法，清除对应缓存。
        /// </summary>
        public static void ClearCache(string processId, string version)
            => _modelCache.TryRemove($"{processId}:{version}", out _);

        /// <summary>Evict all cached models.</summary>
        public static void ClearAllCache() => _modelCache.Clear();

        /// <summary>
        /// Create by Process
        /// </summary>
        public static IProcessModel CreateByProcess(ProcessEntity entity)
        {
            IProcessModel processModel = new ProcessModelBPMN(entity);
            return processModel;
        }

        /// <summary>
        /// Create by Process version — cache-backed.
        /// </summary>
        public static IProcessModel CreateByProcess(string processId, string version)
        {
            var cacheKey = $"{processId}:{version}";
            if (_modelCache.TryGetValue(cacheKey, out var cached))
                return cached;

            var kit = SailClientAccess.UseRunSQL();
            var processModel = CreateByProcess(kit, processId, version);
            _modelCache.TryAdd(cacheKey, processModel);
            return processModel;
        }

        /// <summary>
        /// Create by Process version
        /// </summary>
        private static IProcessModel CreateByProcess(SQLBuilder kit,
            string processId,
            string version)
        {
            if (string.IsNullOrEmpty(processId)
                || string.IsNullOrEmpty(version))
            {
                throw new WfXpdlException(LocalizeHelper.GetEngineMessage("processmodel.factory.processnullexception"));
            }

            var pm = new ProcessManager();
            var entity = pm.GetByVersion(kit, processId, version, false);

            if (entity == null)
            {
                throw new WfXpdlException(LocalizeHelper.GetEngineMessage("processmodel.factory.processnullexception"));
            }

            IProcessModel processModel = new ProcessModelBPMN(entity);
            return processModel;
        }

        /// <summary>
        /// Create by Process instance
        /// </summary>
        public static IProcessModel CreateByProcessInstance(ProcessInstanceEntity processInstance)
        {
            var kit = SailClientAccess.UseRunSQL();
            return CreateByProcessInstance(kit, processInstance);
        }

        /// <summary>
        /// Create by Process instance
        /// </summary>
        public static IProcessModel CreateByProcessInstance(SQLBuilder kit,
            ProcessInstanceEntity processInstance)
        {
            IProcessModel processModel = null;
            if (processInstance.SubProcessType == null)
            {
                //Single process
                processModel = CreateByProcess(kit, processInstance.ProcessId, processInstance.Version);
            }
            else if (processInstance.SubProcessType != null
                && !string.IsNullOrEmpty(processInstance.SubProcessId))
            {
                //Sub process
                processModel = CreateSubByProcessInstance(kit, processInstance);
            }
            return processModel;
        }

        /// <summary>
        /// Create by task
        /// </summary>
        public static IProcessModel CreateByTask(TaskViewEntity taskView)
        {
            var kit = SailClientAccess.UseRunSQL();
            return CreateByTask(kit, taskView);
        }

        /// <summary>
        /// Create by task
        /// </summary>
        public static IProcessModel CreateByTask(SQLBuilder kit, TaskViewEntity taskView)
        {
            IProcessModel processModel = null;
            if (taskView.SubProcessType == null)
            {
                //Single process
                processModel = CreateByProcess(kit, taskView.ProcessId, taskView.Version);
            }
            else if (taskView.SubProcessType != null
                && !string.IsNullOrEmpty(taskView.SubProcessId))
            {
                //Sub process
                processModel = CreateSubByTask(kit, taskView);
            }
            return processModel;
        }

        /// <summary>
        /// Create by sub process node
        /// </summary>
        public static IProcessModel CreateSubByNode(SQLBuilder kit,
            SubProcessNode subProcessNode)
        {
            ProcessEntity entity = null;
            if (subProcessNode != null)
            {
                if (subProcessNode.SubProcessNested == null)
                {
                    //Referenced from outer
                    var pm = new ProcessManager();
                    entity = pm.GetById(kit, subProcessNode.SubProcessDefId);
                }
                else
                {
                    //Nested sub process
                    entity = BuildProcessEntityFromXpdl(subProcessNode.SubProcessNested);
                }
            }

            if (entity == null)
            {
                throw new WfXpdlException(LocalizeHelper.GetEngineMessage("processmodel.factory.processnullexception"));
            }

            IProcessModel processModel = new ProcessModelBPMN(entity);
            return processModel;
        }

        /// <summary>
        /// Build process objects with nested subprocesses
        /// 构建内嵌子流程的流程对象
        /// </summary>
        private static ProcessEntity BuildProcessEntityFromXpdl(Xpdl.Entity.Process subProcess)
        {
            var processEntity = new ProcessEntity();
            processEntity.ProcessId = subProcess.ProcessId;
            processEntity.ProcessName = subProcess.Name;
            processEntity.ProcessCode = subProcess.Code;
            processEntity.Version = "1";
            processEntity.XmlContent = subProcess.XmlContent;
            return processEntity;
        }

        /// <summary>
        /// Create sub process by process instance
        /// </summary>
        private static IProcessModel CreateSubByProcessInstance(SQLBuilder kit,
            ProcessInstanceEntity processInstance)
        {
            var subProcessType = SubProcessTypeEnum.None;
            if (processInstance.SubProcessType != null) subProcessType = EnumHelper.TryParseEnum<SubProcessTypeEnum>(
                processInstance.SubProcessType.ToString());
            var processModel = CreateSubInternal(kit, processInstance.ProcessId, processInstance.Version, subProcessType, processInstance.SubProcessDefId,
                processInstance.SubProcessId);
            return processModel;
        }

        /// <summary>
        /// Create sub process by task
        /// </summary>
        private static IProcessModel CreateSubByTask(SQLBuilder kit,
            TaskViewEntity taskView)
        {
            var subProcessType = SubProcessTypeEnum.None;
            if (taskView.SubProcessType != null) subProcessType = EnumHelper.TryParseEnum<SubProcessTypeEnum>(
                taskView.SubProcessType.ToString());
            var processModel = CreateSubInternal(kit, taskView.ProcessId, taskView.Version, subProcessType, taskView.SubProcessDefId,
                taskView.SubProcessId);
            return processModel;
        }

        /// <summary>
        /// Create sub process internal
        /// </summary>
        private static IProcessModel CreateSubInternal(SQLBuilder kit,
            string processId,
            string version,
            Nullable<SubProcessTypeEnum> subProcessType,
            int subProcessDefId,
            string subProcessId)
        {
            var pm = new ProcessManager();
            ProcessEntity processEntity = null;
            if (subProcessType.HasValue 
                && subProcessType.Value == SubProcessTypeEnum.Referenced)
            {
                processEntity = pm.GetById(kit, subProcessDefId);
            }
            else if (subProcessType.HasValue
                && subProcessType.Value == SubProcessTypeEnum.Nested)
            {
                processEntity = pm.GetByVersion(kit, processId, version, true);
                //取出子流程节点对象
                //Retrieve sub process node objects
                var xmlContent = processEntity.XmlContent;
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xmlContent);

                //查找子流程节点
                //Find sub process nodes
                var strSubProcessPath = XPDLHelper.GetXmlPathOfProcess(true);
                XmlNode xmlSubProcessNode = xmlDoc.DocumentElement.SelectSingleNode(
                    string.Format("{0}[@id='" + subProcessId + "']", strSubProcessPath),
                    XPDLHelper.GetSailflowXmlNamespaceManager(xmlDoc));

                processEntity.ProcessId = XMLHelper.GetXmlAttribute(xmlSubProcessNode, "id");
                processEntity.ProcessName = XMLHelper.GetXmlAttribute(xmlSubProcessNode, "name");
                processEntity.ProcessCode = XMLHelper.GetXmlAttribute(xmlSubProcessNode, "code");
                processEntity.Version = "1";
                processEntity.XmlContent = xmlSubProcessNode.OuterXml;
            }
            IProcessModel processModel = new ProcessModelBPMN(processEntity);
            return processModel;
        }
    }
}
