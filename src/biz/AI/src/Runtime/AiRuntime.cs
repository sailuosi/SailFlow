using System;
using mooSQL.data;
using sailflow.AI.Abstractions;
using sailflow.engine.Client;

namespace sailflow.AI.Runtime
{
    /// <summary>
    /// Host bootstrap helpers for registering AI business implementations with the engine.
    /// </summary>
    public static class AiRuntime
    {
        /// <summary>
        /// Registers <see cref="DefaultAiNodeExecutor"/> as the engine's AI node executor.
        /// Call once at application startup when AI nodes are enabled.
        /// </summary>
        public static void UseDefaultExecutor(SailClient client)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));
            AiNodeExecutorLocator.Register(new DefaultAiNodeExecutor(client));
        }

        /// <summary>
        /// Registers a custom <see cref="IAiNodeExecutor"/> implementation.
        /// </summary>
        public static void UseExecutor(IAiNodeExecutor executor)
        {
            AiNodeExecutorLocator.Register(executor);
        }

        /// <summary>
        /// 建表已迁出：请在业务宿主 createTable&lt;AiModelProviderEntity/AiActivityConfigEntity&gt;。
        /// </summary>
        [Obsolete("AI 建表归属业务宿主，不再在 sailflow.AI 内执行。EnsureTables 默认应为 false。")]
        public static string EnsureTables(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));
            return string.Empty;
        }
    }
}
