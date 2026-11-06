using System;
using System.Collections.Concurrent;
using System.Xml;
using mooSQL.data;
using saiflow.engine.Client;
using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Storage;
using saiflow.engine.Utility;
using saiflow.engine.Xpdl;
using saiflow.WebUtility;

namespace saiflow.engine.Factories;

/// <summary>
/// A · 默认 ProcessModel 工厂：缓存挂本实例；并入原 XPDLStorage / NextActivitySchedule。
/// </summary>
public sealed class DefaultProcessModelFactory : IProcessModelFactory
{
    private readonly SailClient _sail;
    private readonly ConcurrentDictionary<string, IProcessModel> _modelCache = new();

    public DefaultProcessModelFactory(SailClient sail)
    {
        _sail = sail ?? throw new ArgumentNullException(nameof(sail));
    }

    public IXPDLStorage CreateXpdlStorage()
    {
        var config = _sail.Config;
        if (config == null)
            return null;

        var storageType = config.WorkflowFileStorageType;
        if (!string.IsNullOrEmpty(storageType)
            && string.Equals(storageType, "localfile", StringComparison.OrdinalIgnoreCase))
        {
            return new XPDLLocalStorage(config.WorkflowFileServer);
        }

        return null;
    }

    internal NextActivityScheduleBase CreateActivityScheduleGateway(
        IProcessModel processModel,
        GatewaySplitJoinTypeEnum splitJoinType,
        int? activityInstanceId = null)
    {
        if (splitJoinType == GatewaySplitJoinTypeEnum.Split)
            return new NextActivityScheduleSplit(processModel, activityInstanceId, _sail);
        if (splitJoinType == GatewaySplitJoinTypeEnum.Join)
            return new NextActivityScheduleJoin(processModel);
        throw new Exception(_sail.Messages.GetEngineMessage("nextactivityschedulefactory.unknownnodetype"));
    }

    internal static NextActivityScheduleBase CreateActivityScheduleIntermediate(IProcessModel processModel)
        => new NextActivityScheduleIntermediate(processModel);

    public IProcessModel CreateByProcess(ProcessEntity entity)
        => new ProcessModelBPMN(_sail, entity);

    public IProcessModel CreateByProcess(string processId, string version)
    {
        var kit = _sail.UseRunSQL();
        return CreateByProcess(kit, processId, version);
    }

    public IProcessModel CreateByProcess(SQLBuilder kit, string processId, string version)
    {
        if (string.IsNullOrEmpty(processId) || string.IsNullOrEmpty(version))
            throw new WfXpdlException(_sail.Messages.GetEngineMessage("processmodel.factory.processnullexception"));

        var cacheKey = $"{processId}:{version}";
        if (_modelCache.TryGetValue(cacheKey, out var cached))
            return cached;

        var entity = _sail.ManagerFactory.Processes.GetByVersion(kit, processId, version, false);
        if (entity == null)
            throw new WfXpdlException(_sail.Messages.GetEngineMessage("processmodel.factory.processnullexception"));

        IProcessModel processModel = new ProcessModelBPMN(_sail, entity);
        _modelCache.TryAdd(cacheKey, processModel);
        return processModel;
    }

    public IProcessModel CreateByProcessInstance(ProcessInstanceEntity processInstance)
    {
        var kit = _sail.UseRunSQL();
        return CreateByProcessInstance(kit, processInstance);
    }

    public IProcessModel CreateByProcessInstance(SQLBuilder kit, ProcessInstanceEntity processInstance)
    {
        if (processInstance.SubProcessType == null)
            return CreateByProcess(kit, processInstance.ProcessId, processInstance.Version);

        if (!string.IsNullOrEmpty(processInstance.SubProcessId))
            return CreateSubByProcessInstance(kit, processInstance);

        return null;
    }

    public IProcessModel CreateByTask(TaskViewEntity taskView)
    {
        var kit = _sail.UseRunSQL();
        return CreateByTask(kit, taskView);
    }

    public IProcessModel CreateByTask(SQLBuilder kit, TaskViewEntity taskView)
    {
        if (taskView.SubProcessType == null)
            return CreateByProcess(kit, taskView.ProcessId, taskView.Version);

        if (!string.IsNullOrEmpty(taskView.SubProcessId))
            return CreateSubByTask(kit, taskView);

        return null;
    }

    public IProcessModel CreateSubByNode(SQLBuilder kit, SubProcessNode subProcessNode)
    {
        ProcessEntity entity = null;
        if (subProcessNode != null)
        {
            if (subProcessNode.SubProcessNested == null)
            {
                entity = _sail.ManagerFactory.Processes.GetById(kit, subProcessNode.SubProcessDefId);
            }
            else
            {
                entity = BuildProcessEntityFromXpdl(subProcessNode.SubProcessNested);
            }
        }

        if (entity == null)
            throw new WfXpdlException(_sail.Messages.GetEngineMessage("processmodel.factory.processnullexception"));

        return new ProcessModelBPMN(_sail, entity);
    }

    public void ClearCache(string processId, string version)
        => _modelCache.TryRemove($"{processId}:{version}", out _);

    public void ClearAllCache() => _modelCache.Clear();

    public void Invalidate(string processId, string version)
    {
        if (!string.IsNullOrEmpty(processId) && !string.IsNullOrEmpty(version))
            ClearCache(processId, version);

        if (_sail.Cache != null && !string.IsNullOrEmpty(processId) && !string.IsNullOrEmpty(version))
            XPDLMemoryCachedHelper.RemoveXpdlCache(_sail.Cache, processId, version);
    }

    static ProcessEntity BuildProcessEntityFromXpdl(Process subProcess)
    {
        return new ProcessEntity
        {
            ProcessId = subProcess.ProcessId,
            ProcessName = subProcess.Name,
            ProcessCode = subProcess.Code,
            Version = "1",
            XmlContent = subProcess.XmlContent,
        };
    }

    IProcessModel CreateSubByProcessInstance(SQLBuilder kit, ProcessInstanceEntity processInstance)
    {
        var subProcessType = SubProcessTypeEnum.None;
        if (processInstance.SubProcessType != null)
            subProcessType = EnumHelper.TryParseEnum<SubProcessTypeEnum>(processInstance.SubProcessType.ToString());
        return CreateSubInternal(
            kit,
            processInstance.ProcessId,
            processInstance.Version,
            subProcessType,
            processInstance.SubProcessDefId.Value,
            processInstance.SubProcessId);
    }

    IProcessModel CreateSubByTask(SQLBuilder kit, TaskViewEntity taskView)
    {
        var subProcessType = SubProcessTypeEnum.None;
        if (taskView.SubProcessType != null)
            subProcessType = EnumHelper.TryParseEnum<SubProcessTypeEnum>(taskView.SubProcessType.ToString());
        return CreateSubInternal(
            kit,
            taskView.ProcessId,
            taskView.Version,
            subProcessType,
            taskView.SubProcessDefId.Value,
            taskView.SubProcessId);
    }

    IProcessModel CreateSubInternal(
        SQLBuilder kit,
        string processId,
        string version,
        SubProcessTypeEnum? subProcessType,
        int subProcessDefId,
        string subProcessId)
    {
        var pm = _sail.ManagerFactory.Processes;
        ProcessEntity processEntity = null;
        if (subProcessType == SubProcessTypeEnum.Referenced)
        {
            processEntity = pm.GetById(kit, subProcessDefId);
        }
        else if (subProcessType == SubProcessTypeEnum.Nested)
        {
            processEntity = pm.GetByVersion(kit, processId, version, true);
            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(processEntity.XmlContent);
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

        return new ProcessModelBPMN(_sail, processEntity);
    }
}
