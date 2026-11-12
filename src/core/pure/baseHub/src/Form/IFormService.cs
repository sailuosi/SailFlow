using sailflow.hub.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sailflow.hub.Form
{
    /// <summary>
    /// Form Service
    /// 表单服务
    /// </summary>
    public interface IFormService
    {
        IList<Form> GetFormAll();
        /// <summary>含 conf/fields 的完整表单（设计器/运行时）。</summary>
        Form GetForm(string formId);
        /// <summary>新建或更新；返回 FormId。</summary>
        string SaveForm(Form form);
        bool DeleteForm(string formId);
        IList<FieldActivityEdit> GetFieldActivityEditByForm(string formId, FieldQuery query);
        void SaveFieldActivityEditInfo(FieldActivityEditInfo info);
    }
}
