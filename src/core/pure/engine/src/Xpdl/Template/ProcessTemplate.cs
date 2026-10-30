using saiflow.engine.Xpdl.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace saiflow.engine.Xpdl.Template
{
    public class ProcessTemplate
    {
        public string TemplateName { get; set; }
        public ProcessTemplateType TemplateType { get; set; }
        public string Content { get; set; }
    }
}
