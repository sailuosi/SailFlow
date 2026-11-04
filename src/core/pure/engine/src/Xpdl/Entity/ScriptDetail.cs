using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.engine.Common;
using saiflow.engine.Xpdl;


namespace saiflow.engine.Xpdl
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
