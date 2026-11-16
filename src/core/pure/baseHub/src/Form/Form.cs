using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sailflow.hub.Form
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
        /// <summary>授权类型；业务侧权限过滤预留。</summary>
        public string AuthType { get; set; }
        /// <summary>授权编码；业务侧权限过滤预留。</summary>
        public string AuthCode { get; set; }
        /// <summary>授权主体 Id；业务侧权限过滤预留。</summary>
        public string AuthId { get; set; }
        /// <summary>密级；业务侧权限过滤预留。</summary>
        public int? AuthLevel { get; set; }
        /// <summary>创建人 Id。</summary>
        public string Creator { get; set; }
        /// <summary>创建人姓名。</summary>
        public string CreatorName { get; set; }
        /// <summary>逻辑删除：0=有效，1=已删。</summary>
        public int Deleted { get; set; }
        /// <summary>逻辑删除时间。</summary>
        public DateTime? DeleteTime { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? LastUpdatedDate { get; set; }
        public IList<FieldActivityEdit> FieldActivityEditList { get; set; }
    }
}
