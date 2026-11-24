using sailflow.xml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace sailflow.xml
{
    public class ProcessTemplate
    {
        public string TemplateName { get; set; }
        public ProcessTemplateType TemplateType { get; set; }
        public string Content { get; set; }
    }
}
