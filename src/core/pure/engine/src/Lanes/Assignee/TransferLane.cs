using System;
using mooSQL.data;
using sailflow.hosting;
using sailflow.utils;
using sailflow.events;
using sailflow.hooks;
using sailflow.result;

namespace sailflow.lanes
{
    /// <summary>
    /// 转办：本节点交人、不推进下一节点。族群时序由 <see cref="AssigneeLaneBase"/> 固化。
    /// </summary>
    public sealed class TransferLane : AssigneeLaneBase
    {
        public TransferLane(SailClient sail, SQLBuilder kit, FlowCommandContext runner)
            : base(sail, kit, runner)
        {
        }

        public override FlowOperation Operation => FlowOperation.Transfer;

        protected override EventFireTypeEnum SuccessEvent => EventFireTypeEnum.OnProcessTransferred;

        /// <summary><see cref="Manager.ITaskManager.Transfer"/> 内已 FireTaskLifecycle(Entrusted/Created)。</summary>
        protected override bool FireTaskLifecycleAfterCore => false;

        protected override string ErrorLogName => "WF_PROCESS_TRANSFER_ERROR";

        protected override bool TryLoadContext(out AssigneeLaneContext ctx, WfExecutedResult result)
        {
            ctx = null!;
            if (Context == null || Context.TaskId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Transfer_ErrorArguments;
                result.Message = Sail.Messages.GetEngineMessage("taskmanager.transfer.requiresTaskId.warn");
                return false;
            }

            var targets = FlattenTransferTargets(Context);
            if (targets.Count == 0)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Transfer_NoneTargets;
                result.Message = Sail.Messages.GetEngineMessage("taskmanager.transfer.noneTargets.warn");
                return false;
            }

            var tm = Sail.ManagerFactory.Tasks;
            var taskView = tm.GetTaskView(Kit, Context.TaskId);
            if (taskView?.ActivityInstanceId == null)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = WfExceptionType.Transfer_ErrorArguments;
                result.Message = Sail.Messages.GetEngineMessage("taskmanager.transfer.taskNotFound.warn");
                return false;
            }

            var activityInstance = Sail.ManagerFactory.Activities.GetById(Kit, taskView.ActivityInstanceId);
            ctx = new AssigneeLaneContext
            {
                TaskView = taskView,
                ActivityInstance = activityInstance,
                ProcessInstanceId = activityInstance.ProcessInstanceId,
                TaskId = Context.TaskId
            };
            // stash targets for Core via runner already; keep local via field
            _targets = targets;
            return true;
        }

        PerformerList _targets;

        protected override void ExecuteCore(AssigneeLaneContext ctx, WfExecutedResult result)
        {
            var managed = Sail.ManagerFactory.Tasks.Transfer(Kit, ctx.TaskId, Context, _targets);
            if (managed.Status != WfDataManagedStatusEnum.Success)
            {
                result.Status = WfExecutedStatus.Exception;
                result.ExceptionType = MapTransferException(managed.Message);
                result.Message = managed.Message;
                return;
            }

            result.Status = WfExecutedStatus.Success;
            result.Message = Sail.Messages.GetEngineMessage("taskmanager.transfer.success");
        }

        protected override string FormatFailedMessage(Exception e)
        {
            var error = e.InnerException != null ? e.InnerException.Message : e.Message;
            return Sail.Messages.GetEngineMessage("workflowservice.transferprocess.error", error);
        }

        protected override void ApplyFailedExceptionType(WfExecutedResult result, Exception e)
        {
            result.ExceptionType = WfExceptionType.Transfer_RuntimeError;
        }

        internal static PerformerList FlattenTransferTargets(FlowCommandContext runner)
        {
            var list = new PerformerList();
            if (runner?.NextActivityPerformers == null)
                return list;
            foreach (var kv in runner.NextActivityPerformers)
            {
                if (kv.Value == null) continue;
                foreach (var p in kv.Value)
                {
                    if (p != null && !string.IsNullOrWhiteSpace(p.UserId))
                        list.Add(p);
                }
            }
            return list;
        }

        internal static string MapTransferException(string message)
        {
            if (string.IsNullOrEmpty(message))
                return WfExceptionType.Transfer_RuntimeError;

            // 稳定键（Passthrough）/ 中英文文案；「已有待办」须先于 generic noneTargets
            if (ContainsAny(message, "targetHasOpen", "already has an open task", "已有打开待办"))
                return WfExceptionType.Transfer_TargetHasOpen;

            if (ContainsAny(message, "toSelf", "Cannot transfer to self", "不能转办给自己",
                    "notMine", "current user's", "只能转办当前用户"))
                return WfExceptionType.Transfer_NotMineTask;

            if (ContainsAny(message, "activityNotRunning", "notTodo", "ready/running", "to-do",
                    "不在就绪或运行", "不在待办状态"))
                return WfExceptionType.Transfer_NotInRunning;

            if (ContainsAny(message, "taskNotFound", "task not found", "任务不存在",
                    "requiresTaskId", "requires TaskId", "需要指定待办"))
                return WfExceptionType.Transfer_ErrorArguments;

            if (ContainsAny(message, "noneTargets", "at least one target", "至少需要一个"))
                return WfExceptionType.Transfer_NoneTargets;

            return WfExceptionType.Transfer_RuntimeError;
        }

        static bool ContainsAny(string message, params string[] tokens)
        {
            foreach (var t in tokens)
            {
                if (!string.IsNullOrEmpty(t)
                    && message.Contains(t, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
