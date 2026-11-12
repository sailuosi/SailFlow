using System.Collections.Generic;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Structured validate message (path next-hop / issue / summary).
    /// </summary>
    public class ProcessValidateMessage
    {
        /// <summary>Info / Warning / Error</summary>
        public string Level { get; set; }

        /// <summary>Summary / NextHop / Issue / …</summary>
        public string Code { get; set; }

        public string Text { get; set; }

        public string ActivityId { get; set; }

        public string ActivityName { get; set; }

        public string ActivityType { get; set; }

        public string AssigneeResolveType { get; set; }

        public string LeaveJudgeType { get; set; }

        public IList<string> NextActivityIds { get; set; }

        public IList<string> NextActivityNames { get; set; }

        /// <summary>e.g. "A:ApproverPick/Collaborate; B:RoleGroup/Race"</summary>
        public string NextAssigneeSummary { get; set; }

        public ProcessValidateMessage()
        {
            Level = "Info";
            Code = string.Empty;
            Text = string.Empty;
            NextActivityIds = new List<string>();
            NextActivityNames = new List<string>();
            NextAssigneeSummary = string.Empty;
        }
    }
}
