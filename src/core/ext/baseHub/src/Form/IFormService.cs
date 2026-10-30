using saiflow.hub.Resource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace saiflow.hub.Form
{
    /// <summary>
    /// Form Service
    /// 表单服务
    /// </summary>
    public interface IFormService
    {
        IList<Form> GetFormAll();
        IList<FieldActivityEdit> GetFieldActivityEditByForm(string formId, FieldQuery query);
        void SaveFieldActivityEditInfo(FieldActivityEditInfo info);
    }
}
