using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using saiflow.data;
using saiflow.engine.Business.Entity;
using saiflow.engine.Client;

namespace saiflow.engine.Business.Manager
{
    /// <summary>
    /// Rule set manager (wf_rule_set)。终态：Client / kit，无 Session、conn、trans。
    /// </summary>
    public class RuleSetManager
    {
        private const string RuleSetTable = "wf_rule_set";

        public IList<RuleSetEntity> GetAll()
            => SailClientAccess.UseRunSQL()
                .useRepo<RuleSetEntity>()
                .GetList()
                .OrderBy(x => x.RuleSetCode)
                .ThenBy(x => x.Id)
                .ToList();

        public RuleSetEntity GetByCode(string ruleSetCode)
        {
            if (string.IsNullOrEmpty(ruleSetCode))
                return null;
            return SailClientAccess.UseRunSQL()
                .useRepo<RuleSetEntity>()
                .GetFirst(x => x.RuleSetCode == ruleSetCode);
        }

        public int Insert(RuleSetEntity entity)
        {
            var kit = SailClientAccess.UseRunSQL();
            return Insert(kit, entity);
        }

        public int Insert(SQLBuilder kit, RuleSetEntity entity)
        {
            entity.CreatedDateTime = DateTime.UtcNow;
            if (entity.IsEnabled == 0) { /* keep */ }
            else if (entity.IsEnabled != 1) entity.IsEnabled = 1;
            var newId = kit.useRepo<RuleSetEntity, int>().InsertWithPK(entity);
            entity.Id = newId;
            return newId;
        }

        public void Update(RuleSetEntity entity)
        {
            var kit = SailClientAccess.UseRunSQL();
            Update(kit, entity);
        }

        public void Update(SQLBuilder kit, RuleSetEntity entity)
        {
            entity.UpdatedDateTime = DateTime.UtcNow;
            kit.useRepo<RuleSetEntity>().Update(entity);
        }

        public void DeleteByCode(string ruleSetCode)
        {
            if (string.IsNullOrEmpty(ruleSetCode))
                return;
            using (var kit = SailClientAccess.UseRunSQL())
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
            kit.setTable(RuleSetTable)
                .where("rule_set_code", ruleSetCode)
                .doDelete();
        }
    }
}
