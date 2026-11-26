using System;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;

namespace sailflow.runtime.storage;

/// <summary>默认 <see cref="ITransitionInstanceStorage"/>。</summary>
public class DefaultTransitionInstanceStorage : ITransitionInstanceStorage
{
    public virtual string Insert(SQLBuilder kit, TrackEntity entity, string physicalTable = null)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var newId = kit.insertReturnId<TrackEntity, string>(entity, physicalTable);
        entity.TrackId = newId;
        return newId;
    }
}
