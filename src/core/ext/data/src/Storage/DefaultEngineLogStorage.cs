using System;
using mooSQL.data;
using sailflow.data;
using sailflow.data.Lifecycle;
using sailflow.storage;

namespace sailflow.data.Storage;

/// <summary>默认 <see cref="IEngineLogStorage"/>。</summary>
public class DefaultEngineLogStorage : IEngineLogStorage
{
    public virtual void InsertLog(SQLBuilder kit, LogEntity entity, string physicalTable, string templateTable)
        => InsertByEntity(kit, physicalTable, templateTable, entity);

    public virtual void InsertJobLog(SQLBuilder kit, JobLogEntity entity, string physicalTable, string templateTable)
        => InsertByEntity(kit, physicalTable, templateTable, entity);

    static void InsertByEntity<T>(SQLBuilder kit, string physicalTable, string templateTable, T entity)
        where T : class, new()
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        TemplateTableEnsurer.Ensure(
            kit,
            physicalTable,
            templateTable,
            entityType: typeof(T),
            configure: TemplateTableEnsurer.ApplySailColumnTags,
            rethrow: false);

        var useBaseTable = string.IsNullOrWhiteSpace(physicalTable)
            || string.Equals(physicalTable, templateTable, StringComparison.OrdinalIgnoreCase);

        if (useBaseTable)
        {
            if (!kit.useRepo<T>().Insert(entity))
                throw new InvalidOperationException(
                    "IEngineLogStorage repo.Insert failed for " + typeof(T).Name);
            return;
        }

        var n = kit.insert(entity, physicalTable);
        if (n < 0)
            throw new InvalidOperationException(
                "IEngineLogStorage entity insert failed for table " + physicalTable);
    }
}
