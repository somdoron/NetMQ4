using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQ.Core
{
    class YQueue<T>
    {
        private const int ChunkSize = 256;

        class Chunk
        {
            public Chunk(int globalOffset)
            {
                Values = new T[ChunkSize];
                GlobalOffset = globalOffset;
            }

            public T[] Values { get; private set; }                         
            public int GlobalOffset { get; private set; }
            public Chunk Next { get; set; }
        }

        private Chunk m_spareChunk;
        
        private Chunk m_headChunk;
        private int m_headPosition;
        
        private Chunk m_tailChunk;                      
        private int m_tailPosition;

        private int m_nextGlobalIndex;

        public YQueue()
        {
            m_headChunk = m_tailChunk = new Chunk(m_nextGlobalIndex);
            m_nextGlobalIndex += ChunkSize;
        }

        public int HeadPosition
        {
            get { return m_headPosition + m_headChunk.GlobalOffset; }
        }

        public int TailPosition
        {
            get { return m_tailPosition + m_tailChunk.GlobalOffset; }
        }

        public void Enqueue(ref T value)
        {
            m_tailChunk.Values[m_tailPosition] = value;
            m_tailPosition++;

            if (m_tailPosition == ChunkSize)
            {
                Chunk spare = Interlocked.Exchange(ref m_spareChunk, null);

                if (spare != null)
                {                    
                    m_tailChunk.Next = spare;                    
                }
                else
                {
                    m_tailChunk.Next = new Chunk(m_nextGlobalIndex);
                    m_nextGlobalIndex += ChunkSize;
                }

                m_tailChunk = m_tailChunk.Next;
                m_tailPosition = 0;   
            }
        }

        public void Peek(ref T value)
        {
            value = m_headChunk.Values[m_headPosition];
        }

        public void Dequeue(out T value)
        {
            value = m_headChunk.Values[m_headPosition];
            m_headChunk.Values[m_headPosition] = default(T);
            m_headPosition++;

            if (m_headPosition == ChunkSize)
            {
                var old = m_headChunk;
                m_headChunk = m_headChunk.Next;
                m_headPosition = 0;

                // save the chunk as spare chunk
                Interlocked.Exchange(ref m_spareChunk, old);
            }
        }
    }
}
