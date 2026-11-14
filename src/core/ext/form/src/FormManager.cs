using System;
using System.Collections.Generic;
using System.Linq;
using mooSQL.data;
using Newtonsoft.Json.Linq;
using sailflow.data;
using sailflow.engine.Client;

namespace sailflow.hub.Form
{
    /// <summary>表单。终态：SailSqlAccess / kit。</summary>
    internal class FormManager
    {
        public FormManager(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }

        protected SailClient Sail { get; }

        internal List<FormEntity> GetAll()
        {
            var clip = Sail.UseRunSQL().useClip();
            clip.from<FormEntity>(out var f)
                .orderBy(() => f.FormName);
            return clip.select(f).queryList().ToList();
        }

        internal FormEntity GetById(string formId)
            => Sail.UseRunSQL().findRowById<FormEntity>(formId);

        internal string Insert(FormEntity entity)
        {
            entity.CreatedDate ??= DateTime.Now;
            entity.LastUpdatedDate = DateTime.Now;
            if (string.IsNullOrEmpty(entity.Version))
                entity.Version = "1";
            // AutoKey=SailId：Id 为 null 时由 InsertReturnId 回填
            var id = Sail.UseRunSQL().useRepo<FormEntity, string>().InsertReturnId(entity);
            entity.FormId = id;
            return id;
        }

        internal void Update(FormEntity entity)
        {
            entity.LastUpdatedDate = DateTime.Now;
            Sail.UseRunSQL().useRepo<FormEntity>().Update(entity);
        }

        internal bool Delete(string formId)
        {
            var entity = GetById(formId);
            if (entity == null) return false;
            Sail.UseRunSQL().useRepo<FormEntity, string>().DeleteById(formId);
            return true;
        }

        internal IList<FieldEntity> GetFieldSummaryList(string formId)
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
