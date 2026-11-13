using System;
using sailflow.engine.Hosting;

namespace sailflow.biz.Hosting;

public static class SchemaSailExtensions
{
    /// <summary>
    /// 兼容占位：不再执行建表。建表请在业务宿主 PostBuild 自行注册（见 samples SampleSchemaInitializer）。
    /// </summary>
    [Obsolete("UseSchema 不再建表。请在宿主 AddFeature(PostBuild) 调用自有 SchemaInitializer。")]
    public static ISailCollection UseSchema(
        this ISailCollection services,
        Action<SchemaOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        configure?.Invoke(new SchemaOptions());

        // 仍注册 Schema 特性名，便于旧 DependsOn 解析；PostBuild 为空
        return services.AddFeature(new SailFeatureDescriptor
        {
            Name = SailFeatureNames.Schema,
            Phase = SailFeaturePhase.PostBuild,
            Order = 900,
            DependsOn = new[] { SailFeatureNames.Database },
            PostBuild = (_, _) => { }
        });
    }
}
