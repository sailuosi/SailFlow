using System;
using sailflow.engine.Client;
using sailflow.engine.Ids;

namespace sailflow.engine.Hosting
{
    /// <summary>
    /// 发号源 / BPMN codec 装配；Apply 时双注册 moo AutoKey。
    /// 内置 Guid 实现请用 runtime：<c>UseGuidString</c> / <c>UseSailIdDefaults</c> / <c>UseSailRuntimeDefaults</c>。
    /// </summary>
    public static class SailIdExtensions
    {
        public static SailClientBuilder UseSailIds(
            this SailClientBuilder builder,
            Action<SailIdBuilder> configure)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            builder.Services.UseSailIds(configure);
            return builder;
        }

        public static ISailCollection UseSailIds(
            this ISailCollection services,
            Action<SailIdBuilder> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var idBuilder = new SailIdBuilder();
            configure(idBuilder);
            if (idBuilder.Source == null)
                throw new InvalidOperationException(
                    "UseSailIds requires UseSource(...). Built-in Guid: runtime UseGuidString() / UseSailIdDefaults().");
            if (idBuilder.Codec == null)
                throw new InvalidOperationException(
                    "UseSailIds requires UseBpmnCodec(...). Built-in: runtime UseGuidString() sets both.");

            services.Properties[SailBuilderKeys.SailIds] = idBuilder;
            return services;
        }

        /// <summary>已 Build 的 Client：就地换发号源并重挂 moo。</summary>
        public static SailClient UseSailIds(
            this SailClient client,
            Action<SailIdBuilder> configure)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            var idBuilder = new SailIdBuilder();
            configure(idBuilder);
            if (idBuilder.Source == null || idBuilder.Codec == null)
                throw new InvalidOperationException(
                    "UseSailIds requires UseSource + UseBpmnCodec (or runtime UseGuidString).");
            ApplyToClient(client, idBuilder);
            return client;
        }

        internal static void ApplyPending(SailClient client, ISailCollection services)
        {
            if (services.Properties.TryGetValue(SailBuilderKeys.SailIds, out var box)
                && box is SailIdBuilder pending)
            {
                ApplyToClient(client, pending);
            }
            // 无 pending：不自动挂 Guid（内置注册在 runtime）
        }

        internal static void ApplyToClient(SailClient client, SailIdBuilder idBuilder)
        {
            var source = idBuilder.Source
                ?? throw new InvalidOperationException("SailIdBuilder.Source is required.");
            var codec = idBuilder.Codec
                ?? throw new InvalidOperationException("SailIdBuilder.Codec is required.");
            client.Ids = source;
            client.BpmnIds = codec;
            client.IdsGuidCompatibleColumns = idBuilder.GuidCompatibleColumns ?? source.IsGuidCompatible;
            RegisterMooKeyGenerators(client, source);
        }

        /// <summary>EnsurePorts / 首次可用 DB 时补挂（须已 UseSailIds / runtime 默认）。</summary>
        internal static void RegisterMooKeyGenerators(SailClient client, ISailIdSource source = null)
        {
            source ??= client?.TryGetIds();
            if (source == null) return;

            try
            {
                var moo = client?.GetDB()?.Client;
                if (moo == null) return;

                Func<object> gen = () => source.NextEntityRowId();
                var primary = string.IsNullOrWhiteSpace(source.AutoKeyName)
                    ? SailIdNames.AutoKey
                    : source.AutoKeyName.Trim();
                moo.RegisterKeyGenerator(primary, gen);
                moo.RegisterKeyGenerator(SailIdNames.AutoKeyLegacyGuid, gen);
            }
            catch
            {
                // 装配期 DB 未就绪时忽略
            }
        }
    }
}
