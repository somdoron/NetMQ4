using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ.Core;

namespace NetMQ.Protocol
{
    public abstract class BaseProtocol
    {        
        public BaseProtocol(SocketOptions socketOptions)
        {
            SocketOptions = socketOptions;
        }

        protected SocketOptions SocketOptions { get; private set; }

        protected internal abstract bool HasOut { get; }
        protected internal abstract bool TrySend(ref Frame frame);

        protected internal abstract bool HasIn { get; }
        protected internal abstract bool TryReceive(ref Frame frame);

        protected internal abstract void OnNewPipe(Pipe pipe);
        protected internal abstract void OnReadActivated(Pipe pipe);
        protected internal abstract void OnWriteActivated(Pipe pipe);
        protected internal abstract void OnHiccuped(Pipe pipe);
        protected internal abstract void OnPipeClosed(Pipe pipe);
    }
}
