using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using saiflow.engine.Common;

namespace saiflow.engine.Xpdl.Entity
{
    /// <summary>
    /// Receiver
    /// </summary>
    public class Receiver
    {
        public ReceiverTypeEnum ReceiverType
        {
            get;
            set;
        }

        public int Candidates
        {
            get;
            set;
        }
    }
}
