using System;
using System.Threading.Tasks;

namespace NetMQ
{
    public interface IReceive
    {
        bool HasIn { get; }
        bool TryReceiveFrame(ref Frame frame, TimeSpan timeout);
    }
}