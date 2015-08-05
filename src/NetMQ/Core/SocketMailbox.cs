using System;
using System.Collections.Concurrent;

namespace NetMQ.Core
{
    class SocketMailbox : IMailbox
    {
        private BlockingCollection<Command> m_queue;        

        public SocketMailbox()
        {
            m_queue = new BlockingCollection<Command>();
        }

        public bool TryReceive(TimeSpan timeout, out Command command)
        {
            return m_queue.TryTake(out command, timeout);
        }

        public void Send(Command command)
        {
            m_queue.Add(command);
        }
    }
}