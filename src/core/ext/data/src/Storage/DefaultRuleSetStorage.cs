using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.storage;

namespace sailflow.data.Storage;

/// <summary>默认 <see cref="IRuleSetStorage"/>。</summary>
public class DefaultRuleSetStorage : IRuleSetStorage
{
    public virtual IList<RuleSetEntity> GetAll(SQLBuilder kit)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        return kit.useRepo<RuleSetEntity>()
            .GetList()
            .OrderBy(x => x.RuleSetCode)
            .ThenBy(x => x.RuleSetId)
            .ToList();
    }

    public virtual RuleSetEntity GetByCode(SQLBuilder kit, string ruleSetCode)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrEmpty(ruleSetCode))
            return null!;
        return kit.useClip()
            .from<RuleSetEntity>(out var r)
            .where(() => r.RuleSetCode, ruleSetCode)
            .select(r)
            .queryUnique();
    }

    public virtual string Insert(SQLBuilder kit, RuleSetEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        var newId = kit.insertReturnId<RuleSetEntity, string>(entity);
        entity.RuleSetId = newId;
        return newId;
    }

    public virtual void Update(SQLBuilder kit, RuleSetEntity entity)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
        kit.update(entity);
    }

    public virtual void DeleteByCode(SQLBuilder kit, string ruleSetCode)
    {
        if (kit == null)
            throw new ArgumentNullException(nameof(kit));
        if (string.IsNullOrEmpty(ruleSetCode))
            return;
        kit.useClip()
            .setTable<RuleSetEntity>(out var r)
            .where(() => r.RuleSetCode, ruleSetCode)
            .doDelete();
    }
}
