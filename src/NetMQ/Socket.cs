using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetMQ.Core;
using NetMQ.Protocol;
using NetMQ.Transport;
using NetMQ.Transport.InProc;
using NetMQ.Util;

namespace NetMQ
{
    public class Socket : Own, IReceivingSocket, ISendingSocket, ISocket
    {
        //  Determines how often does socket poll for new commands when it
        //  still has unprocessed messages to handle. Thus, if it is set to 100,
        //  socket will process 100 inbound messages before doing the poll.
        //  If there are no unprocessed messages available, poll is done
        //  immediately. Decreasing the value trades overall latency for more
        //  real-time behaviour (less latency peaks).
        private const int InboundPollRate = 100;

        private SocketMailbox m_mailbox;
        private List<Pipe> m_pipes;

        private IDictionary<string, BaseTransport> m_transports; 

        private Dictionary<string, Pipe> m_inprocs;        

        private bool m_disposed;
        private long m_lastTimestamp;
        private BaseProtocol m_protocol;

        private int m_ticks;

        protected Socket(Func<SocketOptions, BaseProtocol> protocolFactory) : base(new SocketOptions())
        {
            m_inprocs = new Dictionary<string, Pipe>();            
            m_mailbox = new SocketMailbox();

            SlotId = SocketManager.AddMailbox(m_mailbox);            
            m_pipes = new List<Pipe>();
            m_disposed = false;
            m_protocol = protocolFactory(Options);
            m_transports = new Dictionary<string, BaseTransport>();
        }

        public bool HasOut
        {
            get
            {
                ProcessCommands(TimeSpan.Zero, false);
                return m_protocol.HasOut;                 
            }
        }

        public bool HasIn
        {
            get
            {
                ProcessCommands(TimeSpan.Zero, false);
                return m_protocol.HasIn;
            }
        }

        public bool TrySendFrame(ref Frame frame, TimeSpan timeout)
        {
            //  Process pending commands, if any.
            ProcessCommands(TimeSpan.Zero, true);

            //  Try to send the message.
            bool isFrameSent = m_protocol.TrySend(ref frame);

            if (isFrameSent)
                return true;

            //  In case of non-blocking send (Zero timeout) we'll simply return false
            if (timeout == TimeSpan.Zero)
                return false;

            if (timeout == Timeout.InfiniteTimeSpan)
            {
                while (true)
                {
                    ProcessCommands(timeout, false);

                    if (m_protocol.TrySend(ref frame))
                        return true;
                }
            }
            else
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                TimeSpan timeLeft = timeout;

                while (true)
                {                    
                    ProcessCommands(timeLeft, false);

                    if (m_protocol.TrySend(ref frame))
                        return true;

                    timeLeft = timeout - stopwatch.Elapsed;
                    if (timeLeft <= TimeSpan.Zero)
                        return false;                    
                }    
            }            
        }

        public bool TryReceiveFrame(ref Frame frame, TimeSpan timeout)
        {
            //  Once every InboundPollRate messages check for signals and process
            //  incoming commands. This happens only if we are not polling altogether
            //  because there are messages available all the time. If poll occurs,
            //  ticks is set to zero and thus we avoid this code.
            //
            //  Note that 'TryReceiveFrame' uses different command throttling algorithm (the one
            //  described above) from the one used by 'TrySendFrame'. This is because counting
            //  ticks is more efficient than doing GetTimeSpan all the time.

            m_ticks++;
            if (m_ticks == InboundPollRate)
            {
                ProcessCommands(TimeSpan.Zero, false);
                m_ticks = 0;
            }

            //  Get the message.
            bool isFrameReceived = m_protocol.TryReceive(ref frame);

            //  If we have the message, return immediately.
            if (isFrameReceived)
                return true;

            //  If the message cannot be fetched immediately, there are two scenarios.
            //  For non-blocking recveive (zero timeout), commands are processed in case there's an
            //  activate reader command already waiting int a command pipe.
            //  If it's not, return false.
            if (timeout == TimeSpan.Zero)
            {
                ProcessCommands(TimeSpan.Zero, false);
                m_ticks = 0;

                return m_protocol.TryReceive(ref frame);
            }      

            if (timeout == Timeout.InfiniteTimeSpan)
            {
                while (true)
                {
                    ProcessCommands(timeout, false);

                    if (m_protocol.TryReceive(ref frame))
                    {
                        m_ticks = 0;
                        return true;
                    }                    
                }
            }
            else
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                TimeSpan timeLeft = timeout;

                while (true)
                {
                    ProcessCommands(timeLeft, false);

                    if (m_protocol.TryReceive(ref frame))
                    {
                        m_ticks = 0;
                        return true;
                    }

                    timeLeft = timeout - stopwatch.Elapsed;
                    if (timeLeft <= TimeSpan.Zero)
                        return false; 
                }
            }
        }

        

        private void AttachPipe(Pipe pipe)
        {
            //  First, register the pipe so that we can terminate it later on.
            m_pipes.Add(pipe);

            pipe.PipeDisposed += PipeDisposed;
            pipe.WriteActivated += WriteActivated;
            pipe.ReadActivated += ReadActivated;
            pipe.Hiccuped += Hiccuped;

            //  Let the protocol know about new pipe.
            m_protocol.OnNewPipe(pipe);

            //  If the socket is already being closed, ask any new pipes to terminate
            //  straight away.
            if (IsDisposing)
            {
                RegisterDisposeAcks(1);
                pipe.Dispose(false);
            }
        }      

        private void WriteActivated(object sender, PipeEventArgs e)
        {
            m_protocol.OnWriteActivated(e.Pipe);
        }

        private void ReadActivated(object sender, PipeEventArgs e)
        {
            m_protocol.OnReadActivated(e.Pipe);
        }

        private void Hiccuped(object sender, PipeEventArgs e)
        {
            m_protocol.OnHiccuped(e.Pipe);
        }

        private void PipeDisposed(object sender, PipeEventArgs e)
        {
            m_protocol.OnPipeDisposed(e.Pipe);

            bool found = false;
            string foundAddress = "";

            // Remove pipe from inproc pipes
            foreach (var inprocPipe in m_inprocs)
            {
                if (inprocPipe.Value == e.Pipe)
                {
                    found = true;
                    foundAddress = inprocPipe.Key;
                    break;
                }
            }

            if (found)
                m_inprocs.Remove(foundAddress);

            //  Unregister to pipe events
            e.Pipe.PipeDisposed -= PipeDisposed;
            e.Pipe.WriteActivated -= WriteActivated;
            e.Pipe.ReadActivated -= ReadActivated;
            e.Pipe.Hiccuped -= Hiccuped;

            //  Remove the pipe from the list of attached pipes and confirm its
            //  dispose if we are already disposing.
            m_pipes.Remove(e.Pipe);            

            if (IsDisposing)
                UnregisterDisposeAck();
        }

        private bool TryGetTransport(string protocol, out BaseTransport transport)
        {
            if (m_transports.TryGetValue(protocol, out transport))
                return true;
            else if (TransportManager.TryCreateTransport(this, protocol, out transport))
            {
                m_transports.Add(protocol, transport);
                LaunchChild(transport);
                return true;
            }

            return false;            
        }

        public void Bind(string address)
        {
            //  Process pending commands, if any.
            ProcessCommands(TimeSpan.Zero, false);

            Core.Uri uri;
            if (!Core.Uri.TryParse(address, out uri))
                throw new ArgumentException("uri not valid ", "address");
            
            if (uri.Protocol == "inproc")
            {
                if (!InProcManager.TryRegisterEndpoint(this, address))
                    throw new NetMQException(ErrorCode.AddressAlreadyInUse);
            }
            else
            {
                BaseTransport transport;

                if (TryGetTransport(uri.Protocol, out transport))
                {
                    if (!transport.ValidateBindUri(uri))
                        throw new ArgumentException("uri not valid", "address");

                    transport.Bind(uri);                    
                }
                else
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }

        public void Unbind(string address)
        {
            //  Process pending commands, if any, since there could be pending unprocessed ProcessOwn()'s
            //  (from LaunchChild() for example) we're asked to dispose now.
            ProcessCommands(TimeSpan.Zero, false);

            Core.Uri uri;

            if (!Core.Uri.TryParse(address, out uri))
                throw new ArgumentException("uri not valid ", "address");

            switch (uri.Protocol)
            {
                case "inproc":
                    if (!InProcManager.TryUnregisterEndpoint(this, address))
                        throw new ArgumentException("address is not bound", "address");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Connect(string address)
        {
            //  Process pending commands, if any.
            ProcessCommands(TimeSpan.Zero, false);

            Core.Uri uri;
            if (!Core.Uri.TryParse(address, out uri))
                throw new ArgumentException("not valid uri", "address");

            switch (uri.Protocol)
            {
                case "inproc":
                    var pipe = InProcManager.ConnectEndpoint(this, address);                    

                    //  Attach local end of the pipe to this socket object.
                    AttachPipe(pipe);

                    // remember inproc connections for disconnect
                    m_inprocs.Add(address, pipe);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Disconnect(string address)
        {
            //  Process pending commands, if any, since there could be pending unprocessed ProcessOwn()'s
            //  (from LaunchChild() for example) we're asked to dispose now.
            ProcessCommands(TimeSpan.Zero, false);

            Core.Uri uri;

            if (!Core.Uri.TryParse(address, out uri))
                throw new ArgumentException("uri not valid ", "address");

            switch (uri.Protocol)
            {
                case "inproc":
                    Pipe pipe;
                    if (!m_inprocs.TryGetValue(address, out pipe))
                        throw new ArgumentException("address was not foundand cannot be disconnected", "address");

                    pipe.Dispose(true);
                    m_inprocs.Remove(address);

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }        

        private void ProcessCommands(TimeSpan timeout, bool throttle)
        {
            Command command;

            bool commandReceived;

            if (timeout != TimeSpan.Zero)
            {
                //  If we are asked to wait, simply ask mailbox to wait.
                commandReceived = m_mailbox.TryReceive(timeout, out command);
            }
            else
            {
                //  If we are asked not to wait, check whether we haven't processed
                //  commands recently, so that we can throttle the new commands.

                if (throttle)
                {
                    //  Get the CPU's tick counter. If 0, the counter is not available.
                    long timestamp = Clock.GetTimestamp();

                    //  Optimised version of command processing - it doesn't have to check
                    //  for incoming commands each time. It does so only if certain time
                    //  elapsed since last command processing. Command delay varies
                    //  depending on CPU speed: It's ~1ms on 3GHz CPU, ~2ms on 1.5GHz CPU
                    //  etc. The optimisation makes sense only on platforms where getting
                    //  a timestamp is a very cheap operation (tens of nanoseconds).
                    if (timestamp != 0)
                    {
                        //  Check whether certain time have elapsed since
                        //  last command processing. If it didn't do nothing.
                        if (timestamp - m_lastTimestamp <= Clock.MaxCommandDelay)
                            return;
                        m_lastTimestamp = timestamp;
                    }
                }

                //  Check whether there are any commands pending for this thread.
                commandReceived = m_mailbox.TryReceive(timeout, out command);
            }

            while (commandReceived)
            {
                command.Destination.Process((dynamic)command);
                commandReceived = m_mailbox.TryReceive(TimeSpan.Zero, out command);
            }
        }

        internal override void Process(BindCommand command)
        {
            AttachPipe(command.Pipe);
            base.Process(command);
        }

        internal override void Process(DisposeCommand command)
        {
            //  Unregister all inproc endpoints associated with this socket.
            //  Doing this we make sure that no new pipes from other sockets (inproc)
            //  will be initiated.
            InProcManager.UnregisterEndpoints(this);

            //  Ask all attached pipes to dispose.
            foreach (var pipe in m_pipes)
            {
                pipe.Dispose(false);
            }

            RegisterDisposeAcks(m_pipes.Count);

            base.Process(command);
        }

        protected override void ProcessDisposed()
        {
            m_disposed = true;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (m_disposed)
                SocketManager.RemoveMailbox(SlotId);
            else
            {
                // Completion in thread is not possible as Multiple inproc can be dependent on each other to complete dispose and might be using the same thread
                ThreadPool.QueueUserWorkItem(s =>
                {
                    // wait until socket is disposed
                    while (!m_disposed)
                    {
                        ProcessCommands(Timeout.InfiniteTimeSpan, false);
                    }

                    // Remove the mailbox from global
                    SocketManager.RemoveMailbox(SlotId);
                });
            }
        }        
    }
}
