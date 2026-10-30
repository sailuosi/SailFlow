using mooSQL.data;
using saiflow.data;

namespace saiflow.hub.Form
{
    /// <summary>字段权限。终态：SailSqlAccess / kit。</summary>
    internal class FieldManager
    {
        internal FieldActivityEditEntity GetFieldActivityEditListByForm(
            string formId,
            string processId,
            string processVersion,
            string activityId)
        {
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("F.*")
                .from("FbFormFieldActivityEdit F")
                .where("ProcessId", processId)
                .where("ProcessVersion", processVersion)
                .where("ActivityId", activityId)
                .where("FormId", int.Parse(formId))
                .queryUnique<FieldActivityEditEntity>();
        }

        internal void InsertFormFieldEdit(FieldActivityEditEntity entity)
            => SailSqlAccess.UseRunSQL().useRepo<FieldActivityEditEntity>().Insert(entity);

        internal void InsertFormFieldEdit(SQLBuilder kit, FieldActivityEditEntity entity)
            => kit.useRepo<FieldActivityEditEntity>().Insert(entity);

        internal void UpdateFormFieldEdit(FieldActivityEditEntity entity)
            => SailSqlAccess.UseRunSQL().useRepo<FieldActivityEditEntity>().Update(entity);

        internal void UpdateFormFieldEdit(SQLBuilder kit, FieldActivityEditEntity entity)
            => kit.useRepo<FieldActivityEditEntity>().Update(entity);
    }
}
