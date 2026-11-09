using mooSQL.data;
using saiflow.data;
using saiflow.engine.Client;

namespace saiflow.hub.Form
{
    /// <summary>字段权限。终态：SailSqlAccess / kit。</summary>
    internal class FieldManager
    {
        public FieldManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        internal FieldActivityEditEntity GetFieldActivityEditListByForm(
            string formId,
            string processId,
            string processVersion,
            string activityId)
        {
            var kit = Sail.UseRunSQL();
            return kit
                .select("F.*")
                .from("FbFormFieldActivityEdit F")
                .where("ProcessId", processId)
                .where("ProcessVersion", processVersion)
                .where("ActivityId", activityId)
                .where("FormId", formId)
                .queryUnique<FieldActivityEditEntity>();
        }

        internal void InsertFormFieldEdit(FieldActivityEditEntity entity)
            => Sail.UseRunSQL().useRepo<FieldActivityEditEntity>().Insert(entity);

        internal void InsertFormFieldEdit(SQLBuilder kit, FieldActivityEditEntity entity)
            => kit.useRepo<FieldActivityEditEntity>().Insert(entity);

        internal void UpdateFormFieldEdit(FieldActivityEditEntity entity)
            => Sail.UseRunSQL().useRepo<FieldActivityEditEntity>().Update(entity);

        internal void UpdateFormFieldEdit(SQLBuilder kit, FieldActivityEditEntity entity)
            => kit.useRepo<FieldActivityEditEntity>().Update(entity);
    }
}
