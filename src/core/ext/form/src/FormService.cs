using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using sailflow.engine.Client;

namespace sailflow.hub.Form
{
    /// <summary>
    /// Form data service
    /// 表单数据服务
    /// </summary>
    public class FormService : IFormService
    {
        public FormService(SailClient client)
        {
            Sail = client ?? throw new System.ArgumentNullException(nameof(client));
        }
        protected SailClient Sail { get; }

        /// <summary>
        /// Read from list
        /// 获取表单列表
        /// </summary>
        /// <returns></returns>
        public IList<Form> GetFormAll()
        {
            var formManager = new FormManager(Sail);
            var itemList = formManager.GetAll();
            IList<Form> formList = new List<Form>();
            foreach (var item in itemList)
            {
                formList.Add(ToForm(item, includeContent: false));
            }
            return formList;
        }

        public Form GetForm(string formId)
        {
            if (string.IsNullOrWhiteSpace(formId))
                return null;
            var entity = new FormManager(Sail).GetById(formId);
            return entity == null ? null : ToForm(entity, includeContent: true);
        }

        public string SaveForm(Form form)
        {
            if (form == null) throw new ArgumentNullException(nameof(form));
            var fm = new FormManager(Sail);
            FormEntity entity;
            var isNew = string.IsNullOrWhiteSpace(form.FormId);
            if (isNew)
            {
                entity = new FormEntity();
            }
            else
            {
                entity = fm.GetById(form.FormId) ?? new FormEntity { FormId = form.FormId };
                isNew = string.IsNullOrWhiteSpace(entity.FormId);
            }

            entity.FormName = form.FormName;
            entity.FormCode = form.FormCode;
            entity.Version = string.IsNullOrEmpty(form.Version) ? "1" : form.Version;
            entity.Description = form.Description;
            entity.TemplateContent = form.Conf;
            entity.HTMLContent = form.FieldsJson;
            entity.FieldSummary = BuildFieldSummary(form.FieldsJson);

            if (isNew)
            {
                var newId = fm.Insert(entity);
                return newId;
            }

            fm.Update(entity);
            return entity.FormId ?? form.FormId;
        }

        public bool DeleteForm(string formId)
        {
            if (string.IsNullOrWhiteSpace(formId))
                return false;
            return new FormManager(Sail).Delete(formId);
        }

        private static Form ToForm(FormEntity item, bool includeContent)
        {
            var form = new Form
            {
                FormId = item.FormId,
                FormCode = item.FormCode,
                FormName = item.FormName,
                Version = item.Version,
                Description = item.Description,
                CreatedDate = item.CreatedDate,
                LastUpdatedDate = item.LastUpdatedDate
            };
            if (includeContent)
            {
                form.Conf = item.TemplateContent;
                form.FieldsJson = item.HTMLContent;
            }
            return form;
        }

        /// <summary>从 form-create fields JSON 提取 field 名列表，写入 FieldSummary。</summary>
        private static string BuildFieldSummary(string fieldsJson)
        {
            if (string.IsNullOrWhiteSpace(fieldsJson))
                return "[]";
            try
            {
                using var doc = JsonDocument.Parse(fieldsJson);
                var names = new List<string>();
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var el in doc.RootElement.EnumerateArray())
                    {
                        // fieldsJson 元素可能是 rule 对象或已是 JSON 字符串
                        if (el.ValueKind == JsonValueKind.String)
                        {
                            try
                            {
                                using var ruleDoc = JsonDocument.Parse(el.GetString() ?? "{}");
                                if (ruleDoc.RootElement.TryGetProperty("field", out var f) ||
                                    ruleDoc.RootElement.TryGetProperty("name", out f) ||
                                    ruleDoc.RootElement.TryGetProperty("title", out f))
                                {
                                    var name = f.GetString();
                                    if (!string.IsNullOrEmpty(name)) names.Add(name);
                                }
                            }
                            catch { /* skip bad rule */ }
                        }
                        else if (el.ValueKind == JsonValueKind.Object)
                        {
                            if (el.TryGetProperty("field", out var f) ||
                                el.TryGetProperty("name", out f))
                            {
                                var name = f.GetString();
                                if (!string.IsNullOrEmpty(name)) names.Add(name);
                            }
                        }
                    }
                }
                return JsonSerializer.Serialize(names);
            }
            catch
            {
                return "[]";
            }
        }


        /// <summary>
        /// Get field activity edit info by formid
        /// 根据表单获取字段权限列表
        /// </summary>
        public IList<FieldActivityEdit> GetFieldActivityEditByForm(string formId, FieldQuery query)
        {
            var fieldList = new List<FieldActivityEdit>();
            //get field summary list 
            var formManager = new FormManager(Sail);
            var fieldManager = new FieldManager(Sail);

            var summartyFieldList = formManager.GetFieldSummaryList(formId).ToList();
            var fieldActivityEditItem = fieldManager.GetFieldActivityEditListByForm(formId, 
                query.ProcessId, query.ProcessVersion, query.ActivityId);

            if (fieldActivityEditItem != null)
            {
                //Fill field permission list
                FillFieledPermission(summartyFieldList, fieldActivityEditItem);
            }

            //转换为Model的标准数据格式
            //Convert to the standard data format of the Model
            foreach (var item in summartyFieldList)
            {
                var fieldActivityEdit = new FieldActivityEdit
                {
                    FieldName = item.FieldName,
                    IsNotVisible = item.IsNotVisible == true,
                    IsReadOnly = item.IsReadOnly == true
                };
                fieldList.Add(fieldActivityEdit);
            }
            return fieldList;
        }

        /// <summary>
        /// Fill field permission data
        /// 填充字段权限数据
        /// </summary>
        /// <param name="summartyFieldList"></param>
        /// <param name="fieldActivityEditItem"></param>
        private void FillFieledPermission(List<FieldEntity> summartyFieldList, FieldActivityEditEntity fieldActivityEditItem)
        {
            var fieldPermission = fieldActivityEditItem.FieldsPermission;
            if (!string.IsNullOrEmpty(fieldPermission))
            {
                var fieldPermissionList = JsonSerializer.Deserialize<List<FieldEntity>>(fieldPermission);
                if (fieldPermissionList != null)
                {
                    foreach (var field in summartyFieldList)
                    {
                        var fieldName = field.FieldName;
                        var permissionItem = fieldPermissionList.Single<FieldEntity>(f => f.FieldName == fieldName);
                        if (permissionItem != null)
                        {
                            field.IsNotVisible = permissionItem.IsNotVisible;
                            field.IsReadOnly = permissionItem.IsReadOnly;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save process node field editing permission data
        /// 保存流程节点字段编辑权限数据
        /// </summary>
        /// <param name="form"></param>
        public void SaveFieldActivityEditInfo(FieldActivityEditInfo info)
        {
            var fm = new FieldManager(Sail);
            var itemDB = fm.GetFieldActivityEditListByForm(info.FormId.ToString(), info.ProcessId, info.ProcessVersion, info.ActivityId);
            if (itemDB != null)
            {
                itemDB.FieldsPermission = JsonSerializer.Serialize(info.FieldActivityEditList);
                fm.UpdateFormFieldEdit(itemDB);
            }
            else
            {
                var formManager = new FormManager(Sail);
                var formDB = formManager.GetById(info.FormId);

                var entity = new FieldActivityEditEntity();
                entity.FormId = info.FormId;
                entity.FormId = info.ProcessDefId;
                entity.ProcessId = info.ProcessId; 
                entity.ProcessName = info.ProcessName;
                entity.ProcessVersion = info.ProcessVersion;
                entity.ActivityId = info.ActivityId;
                entity.ActivityName = info.ActivityName;
                entity.FormName = info.FormName;
                entity.FormVersion = formDB.Version;
                entity.FieldsPermission = JsonSerializer.Serialize(info.FieldActivityEditList);
                fm.InsertFormFieldEdit(entity);
            }
        }
    }
}
