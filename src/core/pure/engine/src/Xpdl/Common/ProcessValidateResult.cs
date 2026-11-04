using System;
using System.Collections.Generic;
using System.Text;
using saiflow.engine.Xpdl;

namespace saiflow.engine.Xpdl
{
    /// <summary>
    /// Process Validate Result
    /// </summary>
    public class ProcessValidateResult
    {
        public ProcessValidateResultTypeEnum ProcessValidatedResultType { get; set; }

        /// <summary>R6：失败明细（如缺 Resolve/Leave 的活动 Id/名称）。</summary>
        public IList<string> Issues { get; set; }

        /// <summary>可读摘要（与 Issues 同源）。</summary>
        public string Message { get; set; }

        public ProcessValidateResult()
        {
            ProcessValidatedResultType = ProcessValidateResultTypeEnum.None;
            Issues = new List<string>();
            Message = string.Empty;
        }
    }
}
