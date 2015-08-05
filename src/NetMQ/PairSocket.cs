using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ.Protocol;

namespace NetMQ
{
    public class PairSocket : Socket
    {
        public PairSocket()
            : base(o => new PairProtocol(o))
        {
        }
    }
}
