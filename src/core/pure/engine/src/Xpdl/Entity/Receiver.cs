using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sailflow.utils;

namespace sailflow.xml
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
