using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ.Core;

namespace NetMQ.Protocol
{
    class PairProtocol : BaseProtocol
    {
        private Pipe m_pipe;

        public PairProtocol(SocketOptions socketOptions) : base(socketOptions)
        {
        }

        protected internal override void OnNewPipe(Pipe pipe)
        {
            //  Pair socket can only be connected to a single peer.
            //  The socket rejects any further connection requests.
            if (m_pipe == null)
                m_pipe = pipe;
            else
                pipe.Close(false);
        }

        protected internal override void OnPipeClosed(Pipe pipe)
        {
            if (m_pipe == pipe)
            {
                m_pipe = null;
            }
        }

        protected internal override void OnReadActivated(Pipe pipe)
        {
            //  There's just one pipe. No lists of active and inactive pipes.
            //  There's nothing to do here.
        }

        protected internal override void OnWriteActivated(Pipe pipe)
        {
            //  There's just one pipe. No lists of active and inactive pipes.
            //  There's nothing to do here.
        }

        protected internal override void OnHiccuped(Pipe pipe)
        {

        }

        protected internal override bool TrySend(ref Frame frame)
        {
            if (m_pipe == null || !m_pipe.TryWrite(ref frame))
                return false;
            
            if (!frame.More)
                m_pipe.Flush();

            frame.Init();

            return true;
        }

        protected internal override bool TryReceive(ref Frame frame)
        {
            //  Deallocate old content of the message.
            frame.Close();

            if (m_pipe == null || !m_pipe.TryRead(out frame))
            {
                frame.Init();
                return false;
            }
            else
            {
                return true;
            }
        }

        protected internal override bool HasIn
        {
            get
            {
                if (m_pipe== null)
                    return false;

                return m_pipe.CheckRead();
            }
        }
      
        protected internal override bool HasOut
        {
            get
            {
                if (m_pipe == null)
                    return false;

                return m_pipe.CheckWrite();
            }
        }            
    }
}
