using System;
using System.Collections.Generic;
using mooSQL.data;
using sailflow.data;
using sailflow.hosting;
using sailflow.run;
using sailflow.storage;

namespace sailflow.data
{
    /// <summary>
    /// Rule set manager (sf_rule_set)。终态：Client / kit，无 Session、conn、trans。
    /// </summary>
    public class RuleSetManager : IRuleSetManager
    {
        public RuleSetManager(SailClient client)
        {
            Sail = client ?? throw new ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        protected IRuleSetStorage Storage =>
            Sail.Storages.GetRequired<IRuleSetStorage>();

        public IList<RuleSetEntity> GetAll()
            => Storage.GetAll(Sail.UseRunSQL());

        public RuleSetEntity GetByCode(string ruleSetCode)
        {
            if (string.IsNullOrEmpty(ruleSetCode))
                return null;
            return Storage.GetByCode(Sail.UseRunSQL(), ruleSetCode);
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
            return Storage.Insert(kit, entity);
        }

        public void Update(RuleSetEntity entity)
        {
            var kit = Sail.UseRunSQL();
            Update(kit, entity);
        }

        public void Update(SQLBuilder kit, RuleSetEntity entity)
        {
            entity.UpdatedDateTime = DateTime.UtcNow;
            Storage.Update(kit, entity);
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
            Storage.DeleteByCode(kit, ruleSetCode);
        }
    }
}
