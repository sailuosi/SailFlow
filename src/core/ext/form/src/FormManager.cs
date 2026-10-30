using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using Newtonsoft.Json.Linq;
using saiflow.data;

namespace saiflow.hub.Form
{
    /// <summary>表单。终态：SailSqlAccess / kit。</summary>
    internal class FormManager
    {
        internal List<FormEntity> GetAll()
        {
            var kit = SailSqlAccess.UseRunSQL();
            return kit
                .select("F.*")
                .from("FbForm F")
                .orderBy("F.FormName")
                .query<FormEntity>()
                .ToList();
        }

        internal FormEntity GetById(int formId)
            => SailSqlAccess.UseRunSQL().useRepo<FormEntity, int>().GetById(formId);

        internal IList<FieldEntity> GetFieldSummaryList(int formId)
        {
            var list = new List<FieldEntity>();
            var entity = GetById(formId);
            var fieldSummary = entity?.FieldSummary;

            if (!string.IsNullOrEmpty(fieldSummary))
            {
                var fieldSummaryList = JArray.Parse(fieldSummary).Values<string>().ToList();
                foreach (var field in fieldSummaryList)
                {
                    if (field != null)
                        list.Add(new FieldEntity { FieldName = field });
                }
            }
            return list;
        }
    }
}
