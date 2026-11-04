using System;
using saiflow.data;
using mooSQL.data;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Common;
using saiflow.engine.Result;
using saiflow.engine.Runtime;
using saiflow.engine.Xpdl;
using saiflow.engine.Client;
using saiflow.engine.Core;

namespace saiflow.engine.Runtime
{
    /// <summary>
    /// Workflow Runtime Manager
    /// 流程运行时管理
    /// </summary>
    internal abstract class WfRuntimeManager
    {
        #region Abstract Method
        internal abstract void ExecuteInstanceImp(SQLBuilder kit);
        #endregion

        #region Property and Basic Method
        internal WfAppRunner AppRunner { get; set; }
        internal IProcessModel ProcessModel { get; set; }
        internal ProcessInstanceEntity ParentProcessInstance { get; set; }
        internal NodeBase InvokedSubProcessNode { get; set; }
        internal ActivityResource ActivityResource { get; set; }
        internal TaskViewEntity TaskView { get; set; }
        internal ActivityInstanceEntity RunningActivityInstance { get; set; }
        internal int ProcessInstanceId { get; set; }
        internal ProcessInstanceEntity ProcessInstance { get; set; }
        
        internal BackwardContext BackwardContext { get; set; }
        internal Boolean IsBackward { get; set; }
        internal SendBackOperation SendBackOperation { get; set; }

        internal WfExecutedResult WfExecutedResult { get; set; }
        internal SailClient Sail { get; set; }

        internal WfRuntimeManager()
        {
            AppRunner = new WfAppRunner();
            BackwardContext = new BackwardContext();
        }
        #endregion

        #region Execute Method
        /// <summary>
        /// Execute Method
        /// 执行方法
        /// </summary>
        /// <returns></returns>
        internal bool Execute(SQLBuilder kit)
        {
            try
            {
                ExecuteInstanceImp(kit);
            }
            catch (WfAssigneeException ax)
            {
                // R4：办理人拒绝写进结果供 API 读 ReasonCode，不再向外抛（事务由上层按 Status 决定是否 commit）
                WfExecutedResult.ApplyAssigneeException(ax);
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, AppRunner, ax);
            }
            catch (WfRuntimeException rx)
            {
                WfExecutedResult.Status = WfExecutedStatus.Exception;
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, AppRunner, rx);
                throw;
            }
            catch (System.Exception e)
            {
                WfExecutedResult.Status = WfExecutedStatus.Failed;
                LogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, AppRunner, e);
                throw;
            }
            finally
            {
                Callback(WfExecutedResult);
            }

            return true;
        }

        /// <summary>
        /// Callback
        /// 事件回调
        /// <param name="result"></param>
        internal void Callback(WfExecutedResult result)
        {
            WfEventArgs args = new WfEventArgs(result);
            _onWfProcessExecuted.Invoke(this, args);
        }
        #endregion

        #region Register Event
        private event EventHandler<WfEventArgs> _onWfProcessExecuting;
        internal event EventHandler<WfEventArgs> OnWfProcessExecuting
        {
            add
            {
                _onWfProcessExecuting += value;
            }
            remove
            {
                _onWfProcessExecuting -= value;
            }
        }

        private event EventHandler<WfEventArgs> _onWfProcessExecuted;
        internal event EventHandler<WfEventArgs> OnWfProcessExecuted
        {
            add
            {
                _onWfProcessExecuted += value;
            }
            remove
            {
                _onWfProcessExecuted -= value;
            }
        }

        /// <summary>
        /// Register Event
        /// 注册事件
        /// </summary>
        /// <param name="executing"></param>
        /// <param name="executed"></param>
        internal WfRuntimeManager RegisterEvent(EventHandler<WfEventArgs> executing, EventHandler<WfEventArgs> executed)
        {
            if (executing != null)
                this.OnWfProcessExecuting += executing;

            if (executed != null)
                this._onWfProcessExecuted += executed;

            return this;
        }

        /// <summary>
        /// Unregister Event
        /// 解除注册事件
        /// </summary>
        /// <param name="executing"></param>
        /// <param name="executed"></param>
        internal WfRuntimeManager UnRegiesterEvent(EventHandler<WfEventArgs> executing, EventHandler<WfEventArgs> executed)
        {
            if (executing != null)
                this.OnWfProcessExecuting -= executing;

            if (executed != null)
                this._onWfProcessExecuted -= executed;

            return this;
        }
        #endregion
    }
}

            