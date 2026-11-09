using saiflow.engine.Common;
using saiflow.engine.Entity;
using saiflow.engine.Manager;
using saiflow.engine.Storage;
using saiflow.engine.Xpdl;
using System;
using System.Collections.Generic;
using System.Text;

namespace saiflow.engine.Service
{
    /// <summary>
    /// Workflow Service - Process Definition
    /// 工作流服务 - 流程定义读取
    /// </summary>
    public partial class WorkflowService : IWorkflowService
    {
        #region Process Definition Data

        /// <summary>
        /// Get Process by Version
        /// 流程定义数据读取
        /// </summary>
        public ProcessEntity GetProcessByVersion(string processId, string version = null)
        {
            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetByVersion(processId, version);
            return entity;
        }

        /// <summary>
        /// Get Process by Name
        /// 根据流程名称获取流程定义
        /// </summary>
        public ProcessEntity GetProcessByName(string processName, string version = null)
        {
            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetByName(processName, version);
            return entity;
        }

        /// <summary>
        /// Get Process by Code
        /// 根据流程代码获取流程定义
        /// </summary>
        public ProcessEntity GetProcessByCode(string processCode, string version)
        {
            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetByCode(processCode, version);
            return entity;
        }

        /// <summary>
        /// Retrieve the current using version of the process 
        /// 获取当前版本的流程定义记录
        /// </summary>
        public ProcessEntity GetProcessUsing(string processId)
        {
            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetVersionUsing(processId);
            return entity;
        }

        /// <summary>
        /// Retrieve the process by id
        /// 获取流程定义记录
        /// </summary>
        public ProcessEntity GetProcessById(string id)
        {
            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetById(id);
            return entity;
        }

        /// <summary>
        /// Retrieve the process file by id
        /// 获取流程定义文件
        /// </summary>
        public ProcessFileEntity GetProcessFileById(string id)
        {
            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetProcessFileById(id, Sail.ProcessModelFactory.CreateXpdlStorage());
            return entity;
        }

        /// <summary>
        /// Get process list
        /// 获取流程定义数据
        /// </summary>
        public IList<ProcessEntity> GetProcessList()
        {
            var pm = Sail.ManagerFactory.Processes;
            var list = pm.GetAll();
            return list;
        }

        /// <summary>
        /// Get process file
        /// 流程定义的XML文件获取
        /// </summary>
        public ProcessFileEntity GetProcessFile(string processId, string version)
        {
            var pm = Sail.ManagerFactory.Processes;
            var entity = pm.GetProcessFile(processId, version, Sail.ProcessModelFactory.CreateXpdlStorage());
            return entity;
        }

        /// <summary>
        /// Save process file
        /// 保存流程定义的xml文件
        /// </summary>
        public void SaveProcessFile(ProcessFileEntity entity)
        {
            var pm = Sail.ManagerFactory.Processes;
            pm.SaveProcessFile(entity);
        }

        /// <summary>
        /// Insert process
        /// 创建流程定义记录
        /// </summary>
        public string InsertProcess(ProcessEntity entity)
        {
            var pm = Sail.ManagerFactory.Processes;
            var processId = pm.Insert(entity);
            return processId;
        }

        /// <summary>
        /// Create process
        /// 创建流程定义记录
        /// </summary>
        public string CreateProcess(ProcessEntity entity)
        {
            var pm = Sail.ManagerFactory.Processes;
            var processId = pm.CreateProcess(entity);
            return processId;
        }

        /// <summary>
        /// Create process version
        /// 创建流程定义记录新版本
        /// </summary>
        public string CreateProcessVersion(ProcessEntity entity)
        {
            string newProcessId = null;
            var pm = Sail.ManagerFactory.Processes;
            var entityDB = pm.GetById(entity.Id);
            if (entity != null)
            {
                newProcessId = pm.Upgrade(entityDB.ProcessId, entityDB.Version, entity.Version);
            }
            return newProcessId;
        }

        /// <summary>
        /// Create process by xml content
        /// 根据XML创建新流程
        /// </summary>
        public ProcessFileEntity CreateProcessByXML(string xmlContent)
        {
            var pm = Sail.ManagerFactory.Processes;
            var processFileEntity = pm.CreateProcessByXML(xmlContent);
            return processFileEntity;
        }

        /// <summary>
        /// Update process
        /// 更新流程定义记录
        /// </summary>
        public void UpdateProcess(ProcessEntity entity)
        {
            var processManager = Sail.ManagerFactory.Processes;
            processManager.Update(entity);
        }

        /// <summary>
        /// Update process state
        /// 更新流程使用状态
        /// </summary>
        public void UpdateProcessUsingState(string processId,
            string version,
            byte usingState)
        {
            var processManager = Sail.ManagerFactory.Processes;
            processManager.UpdateUsingState(processId, version, usingState);
        }

        /// <summary>
        /// Upgrade process
        /// 升级流程记录
        /// </summary>
        public void UpgradeProcess(string processId, string version, string newVersion)
        {
            var processManager = Sail.ManagerFactory.Processes;
            processManager.Upgrade(processId, version, newVersion);
        }

        /// <summary>
        /// Delete process
        /// 删除流程定义记录
        /// </summary>
        public void DeleteProcess(string processId, string version)
        {
            var pm = Sail.ManagerFactory.Processes;
            pm.DeleteProcess(processId, version);
        }

        /// <summary>
        /// Delete process
        /// 删除流程定义记录
        /// </summary>
        public void DeleteProcess(string id)
        {
            var pm = Sail.ManagerFactory.Processes;
            pm.Delete(id);
        }

        /// <summary>
        /// Delete instance internal
        /// 删除流程实例包括其关联数据
        /// </summary>
        public bool DeleteInstanceInt(string processId, string version)
        {
            var pim = Managers.ProcessInstances;
            return pim.Delete(processId, version);
        }

        /// <summary>
        /// Import process file and generate a new process
        /// 导入流程XML文件，并生成新流程
        /// </summary>
        public void ImportProcess(string xmlContent)
        {
            var pm = Sail.ManagerFactory.Processes;
            pm.ImportProcess(xmlContent);
        }

        /// <summary>
        /// Validate process
        /// 流程校验
        /// </summary>
        public ProcessValidateResult ValidateProcess(ProcessEntity entity)
        {
            var result = ProcessValidator.Validate(entity);
            if (result.Messages == null)
                result.Messages = new List<ProcessValidateMessage>();

            SyncIssuesToMessages(result);
            AppendNextHopMessages(entity, result);
            AppendSummaryMessage(result);
            return result;
        }

        /// <summary>将 Issues 同步为结构化 Messages（Code=Issue）。</summary>
        private static void SyncIssuesToMessages(ProcessValidateResult result)
        {
            if (result?.Issues == null || result.Issues.Count == 0)
                return;

            foreach (var issue in result.Issues)
            {
                if (string.IsNullOrWhiteSpace(issue))
                    continue;
                result.Messages.Add(new ProcessValidateMessage
                {
                    Level = "Error",
                    Code = "Issue",
                    Text = issue
                });
            }
        }

        /// <summary>
        /// 对全部节点调用 GetNextActivityListWithoutCondition，写入 NextHop 消息并标注待办人规则。
        /// </summary>
        private void AppendNextHopMessages(ProcessEntity entity, ProcessValidateResult result)
        {
            if (entity == null || string.IsNullOrWhiteSpace(entity.XmlContent) || result == null)
                return;

            try
            {
                var model = Sail.ProcessModelFactory.CreateByProcess(entity);
                if (model == null)
                    return;

                var activities = model.GetActivityList();
                if (activities == null || activities.Count == 0)
                    return;

                foreach (var activity in activities)
                {
                    if (activity == null || string.IsNullOrWhiteSpace(activity.ActivityId))
                        continue;

                    IList<Activity> nextList;
                    try
                    {
                        nextList = model.GetNextActivityListWithoutCondition(activity.ActivityId)
                                   ?? Array.Empty<Activity>();
                    }
                    catch
                    {
                        nextList = Array.Empty<Activity>();
                    }

                    var nextIds = new List<string>();
                    var nextNames = new List<string>();
                    var nextAssigneeParts = new List<string>();
                    foreach (var n in nextList)
                    {
                        if (n == null || string.IsNullOrWhiteSpace(n.ActivityId))
                            continue;
                        nextIds.Add(n.ActivityId);
                        var nLabel = string.IsNullOrWhiteSpace(n.ActivityName) ? n.ActivityId : n.ActivityName;
                        nextNames.Add(nLabel);
                        if (n.ActivityType == ActivityTypeEnum.TaskNode
                            || n.ActivityType == ActivityTypeEnum.MultiSignNode)
                        {
                            var resolve = string.IsNullOrWhiteSpace(n.AssigneeResolveType) ? "(未配置)" : n.AssigneeResolveType;
                            var leave = string.IsNullOrWhiteSpace(n.LeaveJudgeType) ? "(未配置)" : n.LeaveJudgeType;
                            nextAssigneeParts.Add(nLabel + ":" + resolve + "/" + leave);
                        }
                    }

                    var selfLabel = string.IsNullOrWhiteSpace(activity.ActivityName)
                        ? activity.ActivityId
                        : activity.ActivityName + "(" + activity.ActivityId + ")";
                    var typeName = activity.ActivityType.ToString();

                    var text = new StringBuilder();
                    text.Append('[').Append(typeName).Append("] ").Append(selfLabel);

                    var isTaskLike = activity.ActivityType == ActivityTypeEnum.TaskNode
                                     || activity.ActivityType == ActivityTypeEnum.MultiSignNode;
                    var resolveType = activity.AssigneeResolveType;
                    var leaveType = activity.LeaveJudgeType;
                    if (isTaskLike)
                    {
                        var resolveDisp = string.IsNullOrWhiteSpace(resolveType) ? "(未配置)" : resolveType;
                        var leaveDisp = string.IsNullOrWhiteSpace(leaveType) ? "(未配置)" : leaveType;
                        text.Append(" · Resolve=").Append(resolveDisp)
                            .Append(" / Leave=").Append(leaveDisp);
                    }

                    if (nextNames.Count == 0)
                        text.Append(" → (无后续节点)");
                    else
                        text.Append(" → ").Append(string.Join(", ", nextNames));

                    var level = "Info";
                    if (isTaskLike
                        && (string.IsNullOrWhiteSpace(resolveType) || string.IsNullOrWhiteSpace(leaveType)))
                    {
                        level = "Warning";
                    }

                    result.Messages.Add(new ProcessValidateMessage
                    {
                        Level = level,
                        Code = "NextHop",
                        Text = text.ToString(),
                        ActivityId = activity.ActivityId,
                        ActivityName = activity.ActivityName,
                        ActivityType = typeName,
                        AssigneeResolveType = resolveType,
                        LeaveJudgeType = leaveType,
                        NextActivityIds = nextIds,
                        NextActivityNames = nextNames,
                        NextAssigneeSummary = string.Join("; ", nextAssigneeParts)
                    });
                }
            }
            catch
            {
                // 演算失败不影响基础校验结果
            }
        }

        private static void AppendSummaryMessage(ProcessValidateResult result)
        {
            if (result == null)
                return;

            var code = result.ProcessValidatedResultType;
            string text;
            string level;
            switch (code)
            {
                case ProcessValidateResultTypeEnum.Successed:
                    text = "校验通过：存在开始/结束事件，且有通路；用户任务 Resolve/Leave 已配置";
                    level = "Info";
                    break;
                case ProcessValidateResultTypeEnum.WithoutStartEvent:
                    text = "校验未通过：缺少开始事件";
                    level = "Error";
                    break;
                case ProcessValidateResultTypeEnum.WithoutEndEvent:
                    text = "校验未通过：缺少结束事件";
                    level = "Error";
                    break;
                case ProcessValidateResultTypeEnum.WithoutStartEndPath:
                    text = "校验未通过：开始到结束无连通路径";
                    level = "Error";
                    break;
                case ProcessValidateResultTypeEnum.AssigneeConfigIncomplete:
                    text = "校验未通过：用户任务缺少 AssigneeResolve 或 LeaveJudge Type（发布前须配置）";
                    level = "Error";
                    break;
                default:
                    text = "未得到校验结果";
                    level = "Warning";
                    break;
            }

            result.Messages.Add(new ProcessValidateMessage
            {
                Level = level,
                Code = "Summary",
                Text = text
            });
        }

        /// <summary>
        /// Reset Cache
        /// 重置缓存中的流程定义信息
        /// </summary>
        public void ResetCache(string processId, string version = null)
        {
            throw new NotImplementedException();
        }
        
        #endregion
    }
}
