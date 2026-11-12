using System;
using sailflow.engine.Hosting;
using sailflow.hub.Form;

namespace sailflow.hub.Hosting
{
    /// <summary>
    /// Form 插槽：默认挂 <see cref="FormService"/>（FbForm 流程附属表）；
    /// 宿主可用 <see cref="UseForm(ISailCollection, IFormService)"/> 覆盖。
    /// </summary>
    public static class FormSailExtensions
    {
        /// <summary>
        /// 注册默认表单实现（引擎库 <c>FbForm</c> / 字段节点权限，属流程附属配置，非宿主组织主数据）。
        /// </summary>
        public static ISailCollection UseForm(this ISailCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.Form,
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 120,
                ConfigureClient = (client, coll) =>
                {
                    var formService = FormServiceFactory.Create(client);
                    coll.Properties[SailBuilderKeys.FormService] = formService;
                    FormServiceLocator.Register(formService);
                }
            });
        }

        /// <summary>注册宿主自定义 <see cref="IFormService"/>（覆盖默认 FbForm 实现）。</summary>
        public static ISailCollection UseForm(this ISailCollection services, IFormService implementation)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(implementation);

            return services.AddFeature(new SailFeatureDescriptor
            {
                Name = SailFeatureNames.Form,
                Phase = SailFeaturePhase.ConfigureClient,
                Order = 120,
                ConfigureClient = (_, coll) =>
                {
                    coll.Properties[SailBuilderKeys.FormService] = implementation;
                    FormServiceLocator.Register(implementation);
                }
            });
        }
    }
}
