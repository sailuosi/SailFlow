using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.utils;
using sailflow.xml;


namespace sailflow.xml
{
    /// <summary>
    /// Script Detail
    /// </summary>
    public class ScriptDetail
    {
        public ScriptMethodEnum Method { get; set; }
        public string Arguments { get; set; }
        public string ScriptText { get; set; }
    }
}
