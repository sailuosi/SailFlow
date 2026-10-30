using System;
using saiflow.hub.Resource;
using saiflow.hub.Form;
using saiflow.engine.Client;

namespace saiflow.engine.Service
{
    /// <summary>
    /// Workflow Service - Main Entry
    /// 工作流服务 - 主入口
    /// </summary>
    public partial class WorkflowService : IWorkflowService
    {
        #region Property and Constructor
        /// <summary>
        /// 运行根实例（对齐 mooSQL DBInstance 下传）。
        /// </summary>
        protected SailClient Sail { get; }

        /// <summary>
        /// Role User Resource Service Interface
        /// 角色用户资源服务接口
        /// </summary>
        protected IResourceService ResourceService { get; private set; }

        /// <summary>
        /// Form Resource Service Interface
        /// 表单资源服务接口
        /// </summary>
        protected IFormService FormService { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public WorkflowService(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
            ResourceService = ResourceServiceLocator.Resolve();
            FormService = FormServiceLocator.Resolve();
        }
        #endregion
    }
}
