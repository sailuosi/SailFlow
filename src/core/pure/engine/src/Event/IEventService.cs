using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Common;

namespace saiflow.engine.Event
{
    /// <summary>
    /// Event Service Interface
    /// 事件服务接口
    /// </summary>
    public interface IEventService
    {
        int GetProcessInstanceId();
        SQLBuilder GetKit();
        string GetVariable(ProcessVariableScopeEnum variableType, string name);
        string GetVariableByScopePriority(string name);
        void SaveVariable(ProcessVariableScopeEnum variableType, string name, string value);
        string GetCondition(string name);
        void SetCondition(string name, string value);
        T GetInstance<T>(int id) where T : class;
    }

    /// <summary>
    /// Event Subscription List
    /// 事件订阅列表
    /// </summary>
    public class EventSubscriptionList
        : List<KeyValuePair<EventFireTypeEnum, Func<EventContext, IEventService, Boolean>>>
    {

    }
}
