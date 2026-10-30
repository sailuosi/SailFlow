using System;
using System.Collections.Generic;
using System.Text;
using saiflow.engine.Xpdl.Entity;

namespace saiflow.engine.Xpdl.Common
{
    /// <summary>
    /// Process Validate Result
    /// </summary>
    public class ProcessValidateResult
    {
        public ProcessValidateResultTypeEnum ProcessValidatedResultType { get; set; }
        public ProcessValidateResult()
        {
            ProcessValidatedResultType = ProcessValidateResultTypeEnum.None;
        }
    }
}
