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
    public abstract class Own : BaseObject, IDisposable
    {        
        //  True if termination was already initiated. If so, we can destroy
        //  the object if there are no more child objects or pending term acks.
        private bool m_disposing;

        //  Sequence number of the last command sent to this object.
        AtomicCounter m_sentSequenceNumber;

        //  Sequence number of the last command processed by this object.
        long m_processedSequenceNumber;

        //  List of all objects owned by this socket. We are responsible
        //  for deallocating them before we quit.
        private List<Own> m_owned;

        //  Number of events we have to get before we can dispose the object.
        private int m_disposeAcks;

        internal Own(Own parent) : base(parent)
        {
            Options = parent.Options;
            m_disposing = false;
            m_sentSequenceNumber = new AtomicCounter(0);
            m_processedSequenceNumber = 0;
            m_disposeAcks = 0;
            m_owned = new List<Own>();        
        }

        internal Own(SocketOptions socketOptions) : base(SocketManager.IOThreadSlotId) 
        {
            Options = socketOptions;
            m_disposing = false;
            m_sentSequenceNumber = new AtomicCounter(0);
            m_processedSequenceNumber = 0;
            m_disposeAcks = 0;
            m_owned = new List<Own>();            
        }

        internal Own(int slotId)
            : base(slotId)
        {
            m_disposing = false;
            m_sentSequenceNumber = new AtomicCounter(0);
            m_processedSequenceNumber = 0;            
            m_disposeAcks = 0;
            m_owned = new List<Own>();
        }      
        
        public SocketOptions Options { get; private set; }

        /// <summary>
        ///  Socket owning this object. It's responsible for shutting down
        ///  this object.
        /// </summary>
        internal Own Owner { get; private set; }

        internal bool IsDisposing
        {
            get { return m_disposing; }
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

        protected void DisposeChild(Own child)
        {
            ProcessDisposeRequest(child);
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

        private void ProcessDisposeRequest(Own child)
        {
            //  When shutting down we can ignore dispose requests from owned
            //  objects. The termination request was already sent to the object.
            if (!m_disposing)
            {
                //  If I/O object is well and alive let's ask it to terminate.
                int index = m_owned.IndexOf(child);

                //  If not found, we assume that termination request was already sent to
                //  the object so we can safely ignore the request.
                if (index != -1)
                {
                    m_owned.Remove(child);
                    RegisterDisposeAcks(1);

                    //  Note that this object is the root of the (partial shutdown) thus, its
                    //  value of linger is used, rather than the value stored by the children.
                    CommandDispatcher.SendDispose(child, Options.Linger);
                }
            }
        }

        internal override void Process(OwnCommand command)
        {
            //  If the object is already being shut down, new owned objects are
            //  immediately asked to terminate. Note that linger is set to zero.
            if (m_disposing)
            {
                RegisterDisposeAcks(1);
                CommandDispatcher.SendDispose(command.Child, TimeSpan.Zero);                
            }
            else
            {
                //  Store the reference to the owned object.
                m_owned.Add(command.Child);   
            }            

            ProcessSequenceNumber();
        }

        public virtual void Dispose()
        {
            //  If dispose is already underway, there's no point
            //  in starting it anew.
            if (!m_disposing)
            {
                //  As for the root of the ownership tree, there's no one to dispose it,
                //  so it has to dispose itself.
                if (Owner == null)
                {
                    Process(new DisposeCommand(this, Options.Linger));                    
                }
                else
                {
                    //  If I am an owned object, I'll ask my owner to dispose me.
                    CommandDispatcher.SendDisposeRequest(Owner, this);                                        
                }                
            }
        }

        internal override void Process(DisposeCommand command)
        {
            //  Double termination should never happen.
            Debug.Assert(!m_disposing);

            //  Send termination request to all owned objects.
            foreach (var child in m_owned)
            {
                CommandDispatcher.SendDispose(child, command.Linger);
            }

            //  Start termination process and check whether by chance we cannot
            //  terminate immediately.
            RegisterDisposeAcks(m_owned.Count);
            m_owned.Clear();

            m_disposing = true;
            CheckDisposingAcks();
        }

        internal override void Process(DisposeAckCommand command)
        {
            UnregisterDisposeAck();
        }

        internal void RegisterDisposeAcks(int count)
        {
            m_disposeAcks += count;
        }

        internal void UnregisterDisposeAck()
        {            
            m_disposeAcks -= 1;
            
            //  This may be a last ack we are waiting for before termination...
            CheckDisposingAcks();
        }

        private void CheckDisposingAcks()
        {
            if (m_disposing && m_processedSequenceNumber == m_sentSequenceNumber.Get() && m_disposeAcks == 0)
            {
                //  Sanity check. There should be no active children at this point.
                Debug.Assert(m_owned.Count == 0);

                //  The root object has nobody to confirm the termination to.
                //  Other nodes will confirm the termination to the owner.
                if (Owner != null)
                    CommandDispatcher.SendDisposeAck(Owner);

                ProcessDisposed();
            }
        }

        private void ProcessSequenceNumber()
        {
            //  Catch up with counter of processed commands.
            m_processedSequenceNumber++;

            //  We may have catched up and still have pending terms acks.
            CheckDisposingAcks();
        }

        protected abstract void ProcessDisposed();
    }
}
