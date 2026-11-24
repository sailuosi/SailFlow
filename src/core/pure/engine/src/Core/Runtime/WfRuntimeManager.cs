using System;
using sailflow.data;
using mooSQL.data;
using sailflow.engine.Common;
using sailflow.engine.Result;
using sailflow.run;
using sailflow.engine.Xpdl;
using sailflow.engine.Client;

namespace sailflow.run{
    /// <summary>
    /// B · 命令执行编排袋（L-Cmd）。由 RuntimeFactory 按次 Create；字段遵守专题 B.0。
    /// 持本命令导航态 + 对 A（<see cref="Sail"/>）的只读引用；用毕即弃。
    /// 入参袋是 <see cref="FlowCommandContext"/>——本类不是入参 Context。
    /// </summary>
    internal abstract class WfRuntimeManager : IWfRuntime
    {
        #region Abstract Method
        internal abstract void ExecuteInstanceImp(SQLBuilder kit);
        #endregion

        #region Property and Basic Method
        /// <summary>本命令入参袋（B）；通常来自 RunBuilder.Build。</summary>
        internal FlowCommandContext CmdContext { get; set; }
        public IProcessModel ProcessModel { get; set; }
        internal ProcessInstanceEntity ParentProcessInstance { get; set; }
        internal NodeBase InvokedSubProcessNode { get; set; }
        internal ActivityResource ActivityResource { get; set; }
        public TaskViewEntity TaskView { get; set; }
        public ActivityInstanceEntity RunningActivityInstance { get; set; }
        public string ProcessInstanceId { get; set; }
        internal ProcessInstanceEntity ProcessInstance { get; set; }
        
        internal BackwardContext BackwardContext { get; set; }
        internal Boolean IsBackward { get; set; }
        internal SendBackOperation SendBackOperation { get; set; }

        public WfExecutedResult WfExecutedResult { get; set; }
        internal SailClient Sail { get; set; }

        internal WfRuntimeManager()
        {
            CmdContext = new FlowCommandContext();
            BackwardContext = new BackwardContext();
        }
        #endregion

        #region Execute Method
        /// <summary>
        /// Execute Method
        /// 执行方法
        /// </summary>
        /// <returns></returns>
        public bool Execute(SQLBuilder kit)
        {
            try
            {
                ExecuteInstanceImp(kit);
            }
            catch (WfAssigneeException ax)
            {
                // R4：办理人拒绝写进结果供 API 读 ReasonCode，不再向外抛（事务由上层按 Status 决定是否 commit）
                WfExecutedResult.ApplyAssigneeException(ax);
                ILogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, CmdContext, ax);
            }
            catch (WfRuntimeException rx)
            {
                WfExecutedResult.Status = WfExecutedStatus.Exception;
                ILogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, CmdContext, rx);
                throw;
            }
            catch (System.Exception e)
            {
                WfExecutedResult.Status = WfExecutedStatus.Failed;
                ILogManager.RecordLog(Sail, WfDefine.WF_PROCESS_RUN_ERROR, LogEventType.Error, LogPriority.High, CmdContext, e);
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
            _onWfProcessExecuted?.Invoke(this, args);
        }
        #endregion

        #region Runtime Executed Callback
        // 子流程等内部仍用 OnWfProcessExecuted；生命周期对外扩展统一走 Gate/Hook/Notifier
        private event EventHandler<WfEventArgs> _onWfProcessExecuted;
        public event EventHandler<WfEventArgs> OnWfProcessExecuted
        {
            add { _onWfProcessExecuted += value; }
            remove { _onWfProcessExecuted -= value; }
        }
        #endregion
    }
}

            