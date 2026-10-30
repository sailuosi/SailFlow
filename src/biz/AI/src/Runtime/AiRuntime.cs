using saiflow.AI.Abstractions;

namespace saiflow.AI.Runtime
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
        public static void UseDefaultExecutor()
        {
            AiNodeExecutorLocator.Register(new DefaultAiNodeExecutor());
        }

        /// <summary>
        /// Registers a custom <see cref="IAiNodeExecutor"/> implementation.
        /// </summary>
        public static void UseExecutor(IAiNodeExecutor executor)
        {
            AiNodeExecutorLocator.Register(executor);
        }
    }
}
