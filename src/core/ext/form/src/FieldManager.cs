using System.Linq;
using mooSQL.data;
using sailflow.data;
using sailflow.engine.Client;

namespace sailflow.hub.Form
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
            var clip = Sail.UseRunSQL().useClip();
            clip.from<FieldActivityEditEntity>(out var f)
                .where(() => f.ProcessId, processId)
                .where(() => f.ProcessVersion, processVersion)
                .where(() => f.ActivityId, activityId)
                .where(() => f.FormId, formId);
            return clip.select(f).queryList().FirstOrDefault();
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
