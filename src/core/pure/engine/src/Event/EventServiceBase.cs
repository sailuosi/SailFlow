using System;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.engine.Entity;
using sailflow.engine.Manager;
using sailflow.engine.Client;

using sailflow.engine.Factories;

namespace sailflow.engine.Event
{
    /// <summary>
    /// Event Service Base
    /// 事件服务基类
    /// </summary>
    public abstract class EventServiceBase
    {
        #region Property, Abstract and Constructor
        public EventContext EventContext { get; set; }
        public SQLBuilder Kit { get; set; }
        public SailClient Sail { get; }

        public abstract T GetInstance<T>(string id) where T : class;

        public EventServiceBase(SQLBuilder kit,
            EventContext context,
            SailClient client)
        {
            Kit = kit;
            EventContext = context;
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }
        #endregion

        /// <summary>
        /// Read instance primary key Id
        /// 读取实例主键Id
        /// </summary>
        /// <returns></returns>
        public string GetProcessInstanceId()
        {
            return this.EventContext.ProcessInstanceId;
        }

        /// <summary>
        /// Get transaction kit
        /// </summary>
        /// <returns></returns>
        public SQLBuilder GetKit()
        {
            return Kit;
        }

        /// <summary>
        /// Get Condition Value
        /// 获取条件参数数值
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetCondition(string name)
        {
            var value = this.EventContext.ActivityResource.ConditionKeyValuePair[name];
            return value;
        }
        /// <summary>
        /// Set Condition Value
        /// 设置条件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        public void SetCondition(string name, string value)
        {
            this.EventContext.ActivityResource.ConditionKeyValuePair[name] = value;
        }

        /// <summary>
        /// Set Activity Resource
        /// 设置活动资源
        /// </summary>
        /// <param name="activityResource"></param>
        internal void SetActivityResource(ActivityResource activityResource)
        {
            this.EventContext.ActivityResource = activityResource;
        }

        /// <summary>
        /// Save Variable
        /// 保存变量
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="variableType"></param>
        public void SaveVariable(ProcessVariableScopeEnum variableType, string name, string value)
        {
            ProcessVariableEntity processVariable = null;

            if (variableType == ProcessVariableScopeEnum.Process)
            {
                processVariable = new ProcessVariableEntity
                {
                    VariableScope = ProcessVariableScopeEnum.Process.ToString(),
                    AppInstanceId = this.EventContext.AppInstanceId,
                    ProcessId = this.EventContext.ProcessId,
                    ProcessInstanceId = this.EventContext.ProcessInstanceId,
                    ActivityInstanceId = this.EventContext.ActivityInstanceId,
                    Name = name,
                    Value = value,
                    UpdatedDateTime = System.DateTime.UtcNow
                };
            }
            else if (variableType == ProcessVariableScopeEnum.Activity)
            {
                processVariable = new ProcessVariableEntity
                {
                    VariableScope = ProcessVariableScopeEnum.Activity.ToString(),
                    AppInstanceId = this.EventContext.AppInstanceId,
                    ProcessId = this.EventContext.ProcessId,
                    ProcessInstanceId = this.EventContext.ProcessInstanceId,
                    ActivityInstanceId = this.EventContext.ActivityInstanceId,
                    ActivityId = this.EventContext.ActivityId,
                    ActivityName = this.EventContext.ActivityName,
                    Name = name,
                    Value = value,
                    UpdatedDateTime = System.DateTime.UtcNow
                };
            }
            var kit = Kit.useSQL();
            var pvm = SailFactoryAccess.Managers(Sail).ProcessVariables;
            pvm.SaveVariable(kit, processVariable);
        }

        /// <summary>
        /// Get Variable
        /// 获取变量
        /// </summary>
        /// <param name="name"></param>
        /// <param name="variableType"></param>
        /// <returns></returns>
        public string GetVariable(ProcessVariableScopeEnum variableType, string name)
        {
            var kit = Kit.useSQL();
            var pvm = SailFactoryAccess.Managers(Sail).ProcessVariables;
            var variable = pvm.GetVariable(kit, variableType, name, this.EventContext);
            return variable;
        }

        /// <summary>
        /// Get Variable Throughtly
        /// 获取变量内容
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetVariableByScopePriority(string name)
        {
            var kit = Kit.useSQL();
            var pvm = SailFactoryAccess.Managers(Sail).ProcessVariables;
            var variable = pvm.GetVariableByScopePriority(kit, name, this.EventContext);
            return variable;
        }
    }
}
