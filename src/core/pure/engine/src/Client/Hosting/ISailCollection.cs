using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace saiflow.engine.Client.Hosting;

/// <summary>
/// Sail 能力注册集合（类比 IServiceCollection）。
/// 扩展方法应对本接口编程，勿依赖具体 <see cref="SailCollection"/>。
/// </summary>
public interface ISailCollection : IList<SailFeatureDescriptor>
{
    /// <summary>构建期共享属性袋（配置片段、开关、自定义对象）。</summary>
    IDictionary<object, object?> Properties { get; }

    /// <summary>可选：桥接到宿主 MS DI；未设置时走 Sail 内置解析。</summary>
    IServiceCollection? ServiceCollection { get; set; }

    /// <summary>追加一个功能描述符。</summary>
    ISailCollection AddFeature(SailFeatureDescriptor descriptor);
}
