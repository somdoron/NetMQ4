using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ
{
    public struct Frame
    {
        private enum Status
        {
            Empty, GC, Pool
        }

        [Flags]
        private enum Flags
        {
            None = 0,
            More = 1,
            Identity =2,
            Delimiter = 3,
        }

        private Status m_status;
        private byte[] m_data;
        private int m_length;
        private Flags m_flags;

        /// <summary>
        /// Create a message, <paramref name="data"/> is owned by the Message.
        /// </summary>
        /// <param name="data">data of the message, owned by the message after the allocation</param>
        public Frame(byte[] data)
        {
            m_length = data.Length;
            m_data = data;
            m_status = Status.GC;
            m_flags = Flags.None;
        }

        /// <summary>
        /// Create a message, <paramref name="data"/> is owned by the Message.
        /// </summary>
        /// <param name="data">data of the message, owned by the message after the allocation</param>
        /// <param name="length"></param>
        public Frame(byte[] data, int length)
        {
            m_length = length;
            m_data = data;
            m_status = Status.GC;
            m_flags = Flags.None;
        }

        /// <summary>
        /// Create a message and the internal buffer
        /// </summary>
        /// <param name="length"></param>
        public Frame(int length)
        {
            m_length = length;
            m_data = new byte[length];
            m_status = Status.GC;
            m_flags = Flags.None;
        }

        public void Init()
        {
            m_data = null;
            m_length = 0;
            m_status = Status.Empty;
            m_flags = Flags.None;
        }

        public void Close()
        {
            // TODO: release buffer pool 
            Init();
        }

        public byte[] Data
        {
            get { return m_data; }
        }

        public int Length
        {
            get { return m_length; }
        }

        public bool More
        {
            get { return m_flags.HasFlag(Flags.More); }
            set
            {
                if (value)
                    m_flags |= Flags.More;
                else
                    m_flags &= ~Flags.More;

            }
        }

        internal bool Delimiter
        {
            get { return m_flags.HasFlag(Flags.Delimiter); }
            set
            {
                if (value)
                    m_flags |= Flags.Delimiter;
                else
                {
                    m_flags &= ~Flags.Delimiter;
                }
            }
        }

        internal bool Identity
        {
            get { return m_flags.HasFlag(Flags.Identity); }
            set
            {
                if (value)
                    m_flags |= Flags.Identity;
                else
                    m_flags &= ~Flags.Identity;
            }
        }

        public byte[] CloneData()
        {
            byte[] data = new byte[Length];
            Buffer.BlockCopy(Data, 0, data, 0, Length);

            return data;
        }

        public void CopyDataTo(byte[] buffer)
        {
            Buffer.BlockCopy(Data, 0, buffer, 0, Length);
            
        }
    }
}
