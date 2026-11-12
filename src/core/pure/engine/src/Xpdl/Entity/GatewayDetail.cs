using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.engine.Common;

namespace sailflow.engine.Xpdl
{
    /// <summary>
    /// Gateway Detail
    /// </summary>
    public class GatewayDetail
    {
        public GatewaySplitJoinTypeEnum SplitJoinType { get; set; }

        public GatewayDirectionEnum DirectionType { get; set; }

        public GatewayJoinPassEnum JoinPassType { get; set; }

    }
}
