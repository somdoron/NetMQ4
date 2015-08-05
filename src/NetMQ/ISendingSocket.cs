using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ
{
    public interface ISendingSocket
    {
        bool TrySendFrame(ref Frame frame, TimeSpan timeout);
    }
}
