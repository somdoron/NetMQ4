using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AsyncIO;

namespace NetMQ.Core
{
    class IOThread : BaseObject, IDisposable
    {
        public class IOThreadMailbox : IMailbox
        {
            private readonly CompletionPort m_completionPort;            

            public IOThreadMailbox(CompletionPort completionPort)
            {
                m_completionPort = completionPort;                
            }

            public void Send(Command command)
            {                
                // using the completion port as a queue, might not yield good performance
                m_completionPort.Signal(command);
            }
        }

        class StopSignal
        {
            
        }

        private CompletionPort m_completionPort;
        private CompletionStatus[] m_completionStatuses;
        private Thread m_thread;
        

        public IOThread(int slotId)
            : base(slotId)
        {
            m_completionPort = CompletionPort.Create();
            Mailbox = new IOThreadMailbox(m_completionPort);
            m_completionStatuses = new CompletionStatus[100];
        
        }

        public IMailbox Mailbox { get; private set; }

        public void Start()
        {
            m_thread = new Thread(Run);
            m_thread.IsBackground = true;            

            m_thread.Start();
        }

        private void Run()
        {
            while (true)
            {
                int fetched;

                if (m_completionPort.GetMultipleQueuedCompletionStatus(Timeout.Infinite, m_completionStatuses,
                    out fetched))
                {
                    for (int i = 0; i < fetched; i++)
                    {
                        if (m_completionStatuses[i].OperationType == OperationType.Signal)
                        {
                            if (m_completionStatuses[i].State is StopSignal)
                            {
                                return;
                            }
                            else
                            {
                                Command command = (Command)m_completionStatuses[i].State;
                                command.Destination.Process((dynamic)command);
                            }
                        }
                    }   
                }
                else
                {
                    
                }
            }
        }

        public void Dispose()
        {
            // TODO: send a command instead
            m_completionPort.Signal(new StopSignal());
            m_thread.Join();
        }
    }
}
