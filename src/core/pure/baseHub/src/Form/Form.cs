using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace saiflow.hub.Form
{
    /// <summary>
    /// Form
    /// 表单
    /// </summary>
    public class Form
    {
        public string FormId { get; set; }
        public string FormName { get; set; }
        public string FormCode { get; set; }
        public string Version { get; set; }
        /// <summary>form-create option JSON（存 TemplateContent）。</summary>
        public string Conf { get; set; }
        /// <summary>form-create fields JSON 数组字符串（存 HTMLContent）。</summary>
        public string FieldsJson { get; set; }
        public string Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public IList<FieldActivityEdit> FieldActivityEditList { get; set; }
    }
}
