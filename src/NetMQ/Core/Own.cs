using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Core
{
    /// <summary>
    ///  Base class for objects forming a part of ownership hierarchy.
    ///  It handles initialisation and destruction of such objects.
    /// </summary>
    public abstract class Own : BaseObject 
    {        
        //  True if close was already initiated. If so, we can destroy
        //  the object if there are no more child objects or pending term acks.
        private bool m_closing;

        //  Sequence number of the last command sent to this object.
        AtomicCounter m_sentSequenceNumber;

        //  Sequence number of the last command processed by this object.
        long m_processedSequenceNumber;

        //  List of all objects owned by this socket. We are responsible
        //  for deallocating them before we quit.
        private List<Own> m_owned;

        //  Number of events we have to get before we can close the object.
        private int m_closeAcks;

        internal Own(Own parent) : base(parent)
        {
            Options = parent.Options;
            m_closing = false;
            m_sentSequenceNumber = new AtomicCounter(0);
            m_processedSequenceNumber = 0;
            m_closeAcks = 0;
            m_owned = new List<Own>();        
        }

        internal Own(SocketOptions socketOptions) : base(SocketManager.IOThreadSlotId) 
        {
            Options = socketOptions;
            m_closing = false;
            m_sentSequenceNumber = new AtomicCounter(0);
            m_processedSequenceNumber = 0;
            m_closeAcks = 0;
            m_owned = new List<Own>();            
        }

        internal Own(int slotId)
            : base(slotId)
        {
            m_closing = false;
            m_sentSequenceNumber = new AtomicCounter(0);
            m_processedSequenceNumber = 0;            
            m_closeAcks = 0;
            m_owned = new List<Own>();
        }      
        
        public SocketOptions Options { get; private set; }

        /// <summary>
        ///  Socket owning this object. It's responsible for shutting down
        ///  this object.
        /// </summary>
        internal Own Owner { get; private set; }

        internal bool IsClosing
        {
            get { return m_closing; }
        }

        internal void IncreaseSequenceNumber()
        {
            m_sentSequenceNumber.Add(1);
        }

        protected void LaunchChild(Own child)
        {
            child.Owner = this;

            //  Plug the object into the I/O thread.
            CommandDispatcher.SendPlug(child);

            CommandDispatcher.SendOwn(this, child);
        }

        protected void CloseChild(Own child)
        {
            Process(new CloseRequestCommand(this, child));
        }

        internal override void Process(PlugCommand command)
        {
            ProcessSequenceNumber();
        }

        internal override void Process(AttachCommand command)
        {
            ProcessSequenceNumber();
        }

        internal override void Process(BindCommand command)
        {
            ProcessSequenceNumber();
        }

        internal override void Process(InProcConnectedCommand command)
        {
            ProcessSequenceNumber();
        }

        internal override void Process(CloseRequestCommand command)
        {
            //  When shutting down we can ignore close requests from owned
            //  objects. The termination request was already sent to the object.
            if (!m_closing)
            {
                //  If I/O object is well and alive let's ask it to terminate.
                int index = m_owned.IndexOf(command.Child);

                //  If not found, we assume that termination request was already sent to
                //  the object so we can safely ignore the request.
                if (index != -1)
                {
                    m_owned.Remove(command.Child);
                    RegisterCloseAcks(1);

                    //  Note that this object is the root of the (partial shutdown) thus, its
                    //  value of linger is used, rather than the value stored by the children.
                    CommandDispatcher.SendClose(command.Child, Options.Linger);
                }
            }
        }

        internal override void Process(OwnCommand command)
        {
            //  If the object is already being shut down, new owned objects are
            //  immediately asked to terminate. Note that linger is set to zero.
            if (m_closing)
            {
                RegisterCloseAcks(1);
                CommandDispatcher.SendClose(command.Child, TimeSpan.Zero);                
            }
            else
            {
                //  Store the reference to the owned object.
                m_owned.Add(command.Child);   
            }            

            ProcessSequenceNumber();
        }

        protected virtual void Close()
        {
            //  If close is already underway, there's no point
            //  in starting it anew.
            if (!m_closing)
            {
                //  As for the root of the ownership tree, there's no one to close it,
                //  so it has to close itself.
                if (Owner == null)
                {
                    Process(new CloseCommand(this, Options.Linger));                    
                }
                else
                {
                    //  If I am an owned object, I'll ask my owner to close me.
                    CommandDispatcher.SendCloseRequest(Owner, this);                                        
                }                
            }
        }

        internal override void Process(CloseCommand command)
        {
            //  Double termination should never happen.
            Debug.Assert(!m_closing);

            //  Send termination request to all owned objects.
            foreach (var child in m_owned)
            {
                CommandDispatcher.SendClose(child, command.Linger);
            }

            //  Start termination process and check whether by chance we cannot
            //  terminate immediately.
            RegisterCloseAcks(m_owned.Count);
            m_owned.Clear();

            m_closing = true;
            CheckDisposingAcks();
        }

        internal override void Process(CloseAckCommand command)
        {
            UnregisterCloseAck();
        }

        internal void RegisterCloseAcks(int count)
        {
            m_closeAcks += count;
        }

        internal void UnregisterCloseAck()
        {            
            m_closeAcks -= 1;
            
            //  This may be a last ack we are waiting for before termination...
            CheckDisposingAcks();
        }

        private void CheckDisposingAcks()
        {
            if (m_closing && m_processedSequenceNumber == m_sentSequenceNumber.Get() && m_closeAcks == 0)
            {
                //  Sanity check. There should be no active children at this point.
                Debug.Assert(m_owned.Count == 0);

                //  The root object has nobody to confirm the termination to.
                //  Other nodes will confirm the termination to the owner.
                if (Owner != null)
                    CommandDispatcher.SendCloseAck(Owner);

                ProcessClosed();
            }
        }

        private void ProcessSequenceNumber()
        {
            //  Catch up with counter of processed commands.
            m_processedSequenceNumber++;

            //  We may have catched up and still have pending terms acks.
            CheckDisposingAcks();
        }

        protected abstract void ProcessClosed();
    }
}
