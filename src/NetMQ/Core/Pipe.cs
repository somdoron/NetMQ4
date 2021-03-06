﻿using System;
using System.Runtime.Serialization.Formatters;

namespace NetMQ.Core
{
    class PipeEventArgs : EventArgs
    {
        public PipeEventArgs(Pipe pipe)
        {
            Pipe = pipe;
        }

        public Pipe Pipe { get; private set; }
    }

    public class Pipe : BaseObject
    {
        private const int MaxWatermarkDelta = 500;

        private YPipe<Frame> m_inpipe;
        private YPipe<Frame> m_outpipe;

        private int m_highWatermark;
        private int m_lowWatermark;

        private bool m_inActive;
        private bool m_outActive;

        private long m_messagesRead;
        private long m_messagesWritten;
        private long m_peerMessagesRead;

        private Pipe m_peer;

        enum State
        {
            Active,
            DelimiterReceived,
            WaitingForDelimiter,
            Closed
        }

        private State m_state;
        private bool m_delay;

        public static void CreatePair(Socket connectSocket, Socket bindSocket,
            int connetHighWatermark, int bindHighwatermark, out Pipe connectPipe, out Pipe bindPipe)
        {
            //   Creates two pipe objects. These objects are connected by two ypipes,
            //   each to pass messages in one direction.

            YPipe<Frame> upipe1 = new YPipe<Frame>();
            YPipe<Frame> upipe2 = new YPipe<Frame>();

            connectPipe = new Pipe(connectSocket, upipe1, upipe2, connetHighWatermark, bindHighwatermark);
            bindPipe = new Pipe(bindSocket, upipe2, upipe1, bindHighwatermark, connetHighWatermark);

            connectPipe.SetPeer(bindPipe);
            bindPipe.SetPeer(connectPipe);
        }

        internal Pipe(Socket parent, YPipe<Frame> inpipe, YPipe<Frame> outpipe,
            int inHighWatermark, int outHighWatermark)
            : base(parent)
        {
            m_inpipe = inpipe;
            m_outpipe = outpipe;

            m_inActive = true;
            m_outActive = true;

            m_highWatermark = outHighWatermark;
            ComputeLowWatermark(inHighWatermark);

            m_delay = true;
            m_state = State.Active;
        }

        internal event EventHandler<PipeEventArgs> ReadActivated;
        internal event EventHandler<PipeEventArgs> WriteActivated;
        internal event EventHandler<PipeEventArgs> Hiccuped;
        internal event EventHandler<PipeEventArgs> PipeClosed;

        /// <summary>
        /// Pipe endpoint can store an opaque ID to be used by its clients.
        /// </summary>
        internal byte[] Identity { get; set; }

        public void SetHighWatermarks(int inHighWatermark, int outHighwatermark)
        {
            m_highWatermark = outHighwatermark;
            ComputeLowWatermark(inHighWatermark);
        }

        private void ComputeLowWatermark(int inHighWatermark)
        {
            m_lowWatermark = (inHighWatermark > MaxWatermarkDelta * 2)
                ? inHighWatermark - MaxWatermarkDelta
                : (inHighWatermark + 1) / 2;
        }

        public void SetPeer(Pipe peer)
        {
            m_peer = peer;
        }

        public bool CheckRead()
        {
            if (!m_inActive)
                return false;

            if (m_state != State.Active && m_state != State.WaitingForDelimiter)
                return false;

            //  Check if there's an item in the pipe.
            if (!m_inpipe.CheckRead())
            {
                m_inActive = false;
                return false;
            }

            //  If the next item in the pipe is message delimiter,
            //  initiate termination process.
            if (m_inpipe.Probe(f => f.Delimiter))
            {
                Frame frame;

                m_inpipe.TryRead(out frame);
                ProcessDelimiter();

                return false;
            }

            return true;
        }

        public bool TryRead(out Frame frame)
        {
            if (!m_inActive)
            {
                frame = new Frame();
                return false;
            }


            if (m_state != State.Active && m_state != State.WaitingForDelimiter)
            {
                frame = new Frame();
                return false;
            }

            if (!m_inpipe.TryRead(out frame))
            {
                m_inActive = false;
                return false;
            }

            //  If delimiter was read, start termination process of the pipe.
            if (frame.Delimiter)
            {
                ProcessDelimiter();
                return false;
            }

            if (!frame.More && !frame.Identity)
                m_messagesRead++;

            if (m_lowWatermark > 0 && m_messagesRead % m_lowWatermark == 0)
                CommandDispatcher.SendActivateWrite(m_peer, m_messagesRead);

            return true;
        }

        public bool CheckWrite()
        {
            if (!m_outActive || m_state != State.Active)
                return false;

            // check if pipe is full
            if (m_highWatermark > 0 && m_messagesWritten - m_peerMessagesRead == m_highWatermark)
            {
                m_outActive = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Writes a message to the underlying pipe. Returns false if the
        /// message does not pass CheckWrite. If false, the frame object
        /// retains ownership of its buffer.
        /// </summary>
        /// <param name="frame"></param>
        /// <returns></returns>
        public bool TryWrite(ref Frame frame)
        {
            if (!CheckWrite())
                return false;

            bool more = frame.More;
            bool identity = frame.Identity;

            m_outpipe.Write(ref frame, more);

            if (!more && !identity)
                m_messagesWritten++;

            return true;
        }

        /// <summary>
        /// Flush the messages downstream.
        /// </summary>
        public void Flush()
        {
            //  The peer does not exist anymore at this point.
            if (m_state != State.Closed)
            {
                if (m_outpipe != null && m_outpipe.Flush() == ReaderStatus.Asleep)
                    CommandDispatcher.SendActivateRead(m_peer);
            }
        }

        /// <summary>
        /// Temporarily disconnects the inbound message stream and drops
        /// all the messages on the fly. Causes 'hiccuped' event to be generated
        /// in the peer.
        /// </summary>
        public void Hiccup()
        {
            // If dispose is already under way do nothing.
            if (m_state == State.Active)
            {
                //  We'll drop the reference to the inpipe. From now on, the peer is
                //  responsible for deallocating it.
                m_inpipe = null;

                //  Create new inpipe.
                m_inpipe = new YPipe<Frame>();
                m_inActive = true;

                //  Notify the peer about the hiccup.
                CommandDispatcher.SendHiccup(m_peer, m_inpipe);
            }
        }

        internal override void Process(ActivateReadCommand command)
        {
            if (!m_inActive && (m_state == State.Active || m_state == State.WaitingForDelimiter))
            {
                m_inActive = true;

                var temp = ReadActivated;
                if (temp != null)
                {
                    temp(this, new PipeEventArgs(this));
                }
            }
        }

        internal override void Process(ActivateWriteCommand command)
        {
            //  Remember the peers's message sequence number.
            m_peerMessagesRead = command.MessagesRead;

            if (!m_outActive && m_state == State.Active)
            {
                m_outActive = true;

                var temp = WriteActivated;
                if (temp != null)
                {
                    temp(this, new PipeEventArgs(this));
                }
            }
        }

        internal override void Process(HiccupCommand command)
        {
            //  Destroy old outpipe. Note that the read end of the pipe was already
            //  migrated to this thread.
            m_outpipe.Flush();

            Frame frame;

            // empty the outpipe
            while (m_outpipe.TryRead(out frame))
            {
                if (!frame.More)
                    m_messagesWritten--;

                frame.Close();
            }

            //  Plug in the new outpipe.
            m_outpipe = command.Pipe;
            m_outActive = true;

            //  If appropriate, notify the user about the hiccup.
            if (m_state == State.Active)
            {
                var temp = Hiccuped;
                if (temp != null)
                {
                    temp(this, new PipeEventArgs(this));
                }
            }
        }

        internal override void Process(ClosePipeCommand command)
        {
            //  This is the simple case of peer-induced dispose. If there are no
            //  more pending messages to read, or if the pipe was configured to drop
            //  pending messages, we can move directly to the Closed state.
            //  Otherwise we'll hang up in WaitingForDelimiter state till all
            //  pending messages are read.
            if (m_state == State.Active)
            {
                if (m_delay)
                    m_state = State.WaitingForDelimiter;
                else
                    CompleteClose();
            }
            //  Delimiter happened to arrive before the close command. Now we have the
            //  close command as well, so we can move straight to close state.
            else if (m_state == State.DelimiterReceived)
            {
                CompleteClose();
            }
        }

        private void CompleteClose()
        {
            m_outpipe = null;
            m_state = State.Closed;

            //  We'll deallocate the inbound pipe, the peer will deallocate the outbound
            //  pipe (which is an inbound pipe from its point of view).
            //  Delete all the unread messages in the pipe. We have to do it by
            //  hand because Frame need to release buffer pool memory. 
            Frame frame;
            while (m_inpipe.TryRead(out frame))
            {
                frame.Close();
            }

            //  Notify the user that all the references to the pipe should be dropped.
            var temp = PipeClosed;
            if (temp != null)
            {
                temp(this, new PipeEventArgs(this));
            }
        }

        private void ProcessDelimiter()
        {
            if (m_state == State.Active)
                m_state = State.DelimiterReceived;
            else
                CompleteClose();            
        }

        public void Close(bool delay)
        {
            //  Overload the value specified at pipe creation.
            m_delay = delay;

            //  Drop any unfinished outbound messages.
            // TODO: rollback();
            m_outActive = false;

            //  If close was already called, we can ignore the duplicit invocation.
            if (m_state != State.Closed)
            {
                //  The close case. Ask the peer to close
                if (m_state == State.Active)
                {
                    if (m_outpipe != null)
                    {
                        //  Write the delimiter into the pipe. Note that watermarks are not
                        //  checked; thus the delimiter can be written even when the pipe is full.
                        Frame frame = new Frame();
                        frame.Delimiter = true;
                        m_outpipe.Write(ref frame, false);
                        Flush();
                    }

                    CommandDispatcher.SendClosePipe(m_peer);
                    CompleteClose();
                }
                else if (m_state == State.WaitingForDelimiter)
                {
                    //  There are still pending messages available, but the user calls
                    //  'Close'. We can act as if all the pending messages were read.
                    if (!m_delay)
                        CompleteClose();
                }
                
                // We received delimiter but on the command yet, we will just wait for the command which complete the closing
            }
        }
    }
}