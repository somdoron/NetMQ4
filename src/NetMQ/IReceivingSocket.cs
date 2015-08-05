using System;
using System.Threading.Tasks;

namespace NetMQ
{
    public interface IReceivingSocket
    {
        bool TryReceiveFrame(ref Frame frame, TimeSpan timeout);
    }
}