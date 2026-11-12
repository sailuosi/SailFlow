using System;

namespace sailflow.AI.Abstractions
{
    /// <summary>
    /// Host-side registration point for <see cref="IAiNodeExecutor"/>.
    /// Call <see cref="Register"/> at startup (typically from sailflow.biz.AI bootstrap).
    /// </summary>
    public static class AiNodeExecutorLocator
    {
        private static IAiNodeExecutor? _executor;

        public static bool IsRegistered => _executor != null;

        public static void Register(IAiNodeExecutor executor)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
        }

        public static void Clear() => _executor = null;

        public static IAiNodeExecutor Resolve()
        {
            if (_executor == null)
            {
                throw new InvalidOperationException(
                    "AI node executor is not registered. Reference sailflow.biz.AI and call AiRuntime.UseDefaultExecutor(client) at host startup.");
            }

            return _executor;
        }
    }
}
