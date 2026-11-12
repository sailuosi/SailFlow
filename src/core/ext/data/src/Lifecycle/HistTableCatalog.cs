using System;
using sailflow.data;
using sailflow.engine.Entity;
using sailflow.engine.Client;

namespace sailflow.data.Lifecycle
{
    /// <summary>流程归档闭包涉及的热表 → 逻辑基名（供 <see cref="SailDataLifecycleOptions.FormatHistTableName"/>）。</summary>
    public static class HistTableCatalog
    {
        public static readonly HistTableSpec ProcessInstance = Spec<ProcessInstanceEntity>("process_instance");
        public static readonly HistTableSpec ActivityInstance = Spec<ActivityInstanceEntity>("activity_instance");
        public static readonly HistTableSpec Task = Spec<TaskEntity>("task");
        public static readonly HistTableSpec TransitionInstance = Spec<TransitionInstanceEntity>("transition_instance");
        public static readonly HistTableSpec ProcessVariable = Spec<ProcessVariableEntity>("process_variable");

        public static readonly HistTableSpec[] Closure =
        {
            ProcessInstance,
            ActivityInstance,
            Task,
            TransitionInstance,
            ProcessVariable
        };

        private static HistTableSpec Spec<TEntity>(string logicalBase) where TEntity : class
            => new HistTableSpec(typeof(TEntity), SailEntityMapper.ResolveTableName<TEntity>(), logicalBase);

        public static string ResolveHistName(SailDataLifecycleOptions options, HistTableSpec spec, int endedYear)
        {
            options ??= new SailDataLifecycleOptions();
            return options.FormatHistTableName(spec.LogicalBaseName, endedYear);
        }
    }

    public sealed class HistTableSpec
    {
        public HistTableSpec(Type entityType, string hotTableName, string logicalBaseName)
        {
            EntityType = entityType;
            HotTableName = hotTableName;
            LogicalBaseName = logicalBaseName;
        }

        public Type EntityType { get; }
        public string HotTableName { get; }
        public string LogicalBaseName { get; }
    }
}
