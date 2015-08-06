using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ
{
    public interface ISend
    {
        bool HasOut { get; }
        bool TrySendFrame(ref Frame frame, TimeSpan timeout);
    }
}
