using System;
using mooSQL.data;
using mooSQL.data.model;
using sailflow.AI.Abstractions;
using sailflow.AI.Entity;
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
        /// Ensures AI-related tables exist (ai_model_provider / ai_activity_config).
        /// Call from host startup when AI is enabled; not part of core engine schema.
        /// </summary>
        public static string EnsureTables(SQLBuilder kit)
        {
            if (kit == null)
                throw new ArgumentNullException(nameof(kit));

            var wk = kit.useDBInit();
            wk.upsertMode()
                .columnTypeByTag("sail.id", DataFam.Guid)
                .columnTypeByTag("sail.xml", DataFam.Json)
                .createTable<AiModelProviderEntity>()
                .createTable<AiActivityConfigEntity>();

            return wk.WorkingProgress?.ToString() ?? string.Empty;
        }
    }
}
