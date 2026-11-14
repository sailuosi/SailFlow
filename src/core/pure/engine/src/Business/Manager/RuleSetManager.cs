using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Entity;
using sailflow.engine.Client;

namespace sailflow.engine.Manager
{
    /// <summary>
    /// Rule set manager (wf_rule_set)。终态：Client / kit，无 Session、conn、trans。
    /// </summary>
    public class RuleSetManager
    {
        public RuleSetManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        public IList<RuleSetEntity> GetAll()
            => Sail.UseRunSQL()
                .useRepo<RuleSetEntity>()
                .GetList()
                .OrderBy(x => x.RuleSetCode)
                .ThenBy(x => x.RuleSetId)
                .ToList();

        public RuleSetEntity GetByCode(string ruleSetCode)
        {
            if (string.IsNullOrEmpty(ruleSetCode))
                return null;
            return Sail.useRunClip()
                .from<RuleSetEntity>(out var r)
                .where(()=> r.RuleSetCode ,ruleSetCode)
                .select(r)
                .queryUnique();
        }

        public string Insert(RuleSetEntity entity)
        {
            var kit = Sail.UseRunSQL();
            return Insert(kit, entity);
        }

        public string Insert(SQLBuilder kit, RuleSetEntity entity)
        {
            entity.CreatedDateTime = DateTime.UtcNow;
            if (entity.IsEnabled == 0) { /* keep */ }
            else if (entity.IsEnabled != 1) entity.IsEnabled = 1;
            var newId = kit.insertReturnId<RuleSetEntity, string>(entity);
            entity.RuleSetId = newId;
            return newId;
        }

        public void Update(RuleSetEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        public void Update(SQLBuilder kit, RuleSetEntity entity)
        {
            entity.UpdatedDateTime = DateTime.UtcNow;
            kit.update(entity);
        }

        public void DeleteByCode(string ruleSetCode)
        {
            if (string.IsNullOrEmpty(ruleSetCode))
                return;
            using (var kit = Sail.UseRunSQL())
            {
                kit.beginTransaction();
                try
                {
                    DeleteByCode(kit, ruleSetCode);
                    kit.commit();
                }
                catch
                {
                    throw;
                }
            }
        }

        public void DeleteByCode(SQLBuilder kit, string ruleSetCode)
        {
            if (string.IsNullOrEmpty(ruleSetCode))
                return;
            kit.useClip()
                .setTable<RuleSetEntity>(out var r)
                .where(() => r.RuleSetCode, ruleSetCode)
                .doDelete();
        }
    }
}
