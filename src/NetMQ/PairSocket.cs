using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ.Protocol;

namespace NetMQ
{
    public class PairSocket : Socket, IDuplexSocket
    {
        public PairSocket() : base(o => new PairProtocol(o))
        {
        }

        public bool HasOut
        {
            get
            {
                return HasInInternal;
            }
        }

        public bool HasIn
        {
            get
            {
                return HasOutInternal;
            }
        }

        public bool TryReceiveFrame(ref Frame frame, TimeSpan timeout)
        {
            return base.TryReceiveFrameInternal(ref frame, timeout);
        }

        public bool TrySendFrame(ref Frame frame, TimeSpan timeout)
        {
            return base.TrySendFrameInternal(ref frame, timeout);
        }
    }
}
