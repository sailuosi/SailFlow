using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using mooSQL.data;
using sailflow.data;
using sailflow.hub.Localize;
using sailflow.hub.Resource;
using sailflow.assignee;
using sailflow.engine.Common;
using sailflow.hooks;
using sailflow.engine.Xpdl;
using sailflow.engine.Result;
using sailflow.engine.Client;
using sailflow.log;
using sailflow.run;
using sailflow.engine.Factories;
using sailflow.WebUtility;

namespace sailflow.data;

/// <summary>契约 · ProcessManager（实现位于 sailflow.runtime）。</summary>
public interface IProcessManager
{
    ProcessEntity GetById(string id);

    ProcessEntity GetById(SQLBuilder kit, string id);

    ProcessEntity GetByVersion(string processId, string version, bool throwException = true);

    ProcessEntity GetByVersion(SQLBuilder kit, string processId, string version, bool throwException = true);

    ProcessEntity GetVersionUsing(string processId, bool isNotThrownException = true);

    ProcessEntity GetVersionUsing(SQLBuilder kit, string processId, bool isNotThrownException);

    ProcessEntity GetByName(string processName, string version = null);

    ProcessEntity GetByCode(string processCode, string version = null);

    ProcessEntity GetByVersionInternal(SQLBuilder kit, string processId, string version);

    ProcessEntity GetByMessage(string topic);

    ProcessEntity GetSingleByAppType(string appType);

    ProcessEntity GetSingleByAppType(SQLBuilder kit, string appType);

    List<ProcessEntity> GetAll();

    string Insert(ProcessEntity entity);

    string Insert(SQLBuilder kit, ProcessEntity entity);

    void Update(ProcessEntity entity);

    void Update(SQLBuilder kit, ProcessEntity entity);

    void UpdateUsingState(string processId, string version, byte usingState);

    string Upgrade(string processId, string version, string newVersion);

    void UpgradePoolProcess(SQLBuilder kit, string originProcessId, string newProcessId, string newVersion);

    void Delete(string id);

    void Delete(string processId, string version);

    void Delete(SQLBuilder kit, string processId);

    void DeleteProcess(string processId, string version);

    string CreateProcess(ProcessEntity entity);

    ProcessFileEntity CreateProcessByXML(string xmlContent);

    void SaveProcessFile(ProcessFileEntity entity);

    void SaveProcessFileSingle(ProcessFileEntity entity);

    void SaveProcessFileWithPool(ProcessFileEntity entity, XmlDocument xmlDoc, XmlNode xmlNodeCollaboration);

    void SetProcessTimerType(string processId, string version);

    void ImportProcess(string xmlContent);

    ProcessFileEntity GetProcessFile(string processId, string version, IXPDLStorage extStorage = null);

    ProcessFileEntity GetProcessFile(SQLBuilder kit, string processId, string version, IXPDLStorage extStorage = null);

    ProcessFileEntity GetProcessFileById(string id, IXPDLStorage extStorage = null);

    XmlDocument GetProcessXmlDocument(string processId, string version, IXPDLStorage extStorage = null);
}
