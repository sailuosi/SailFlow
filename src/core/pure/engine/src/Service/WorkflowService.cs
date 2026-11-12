using System;
using sailflow.hub.Resource;
using sailflow.hub.Form;
using sailflow.engine.Client;
using sailflow.engine.Factories;

namespace sailflow.engine.Service
{
    /// <summary>
    /// H · 无状态办理执行器：持 A=<see cref="SailClient"/>，动词只吃 <see cref="Common.FlowCommandContext"/> 参数；
    /// 不持链式会话 / Runner 字段。建造见 <c>Sail.CreateRunBuilder</c>。
    /// </summary>
    public partial class WorkflowService : IWorkflowService
    {
        #region Property and Constructor
        /// <summary>
        /// 运行根实例（对齐 mooSQL DBInstance 下传）。
        /// </summary>
        protected SailClient Sail { get; }

        /// <summary>A · Runtime 工厂（默认转发旧 static）。</summary>
        protected DefaultRuntimeFactory Runtimes => SailFactoryAccess.Runtime(Sail);

        /// <summary>A · Manager 工厂（含 internal ProcessInstances）。</summary>
        protected DefaultManagerFactory Managers => SailFactoryAccess.Managers(Sail);

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
