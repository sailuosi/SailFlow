using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Client;
using sailflow.engine.Common;

namespace sailflow.engine.Event
{
    /// <summary>
    /// Event Service Interface
    /// 事件服务接口
    /// </summary>
    public interface IEventService
    {
        SailClient Sail { get; }
        string GetProcessInstanceId();
        SQLBuilder GetKit();
        string GetVariable(ProcessVariableScopeEnum variableType, string name);
        string GetVariableByScopePriority(string name);
        void SaveVariable(ProcessVariableScopeEnum variableType, string name, string value);
        string GetCondition(string name);
        void SetCondition(string name, string value);
        T GetInstance<T>(string id) where T : class;
    }
}
