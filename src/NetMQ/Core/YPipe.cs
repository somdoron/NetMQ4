using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQ.Core
{
    enum ReaderStatus
    {
        Alive,
        Asleep
    }

    class YPipe<T>
    {
        private readonly YQueue<T> m_queue;

        //  Index of the first un-flushed item. This variable is used
        //  exclusively by writer thread.
        private int m_flushFromIndex;

        //  Index of the first item to be flushed in the future.
        private int m_flushToIndex;

        //  Index of the first un-prefetched item. This variable is used
        //  exclusively by reader thread.
        private int m_unfetchedIndex;

        //  The single point of contention between writer and reader thread.
        //  Index of the last flushed item. If it is -1,
        //  reader is asleep. This index should be always accessed using
        //  atomic operations.
        private int m_flushedIndex;

        public YPipe()
        {
            m_queue = new YQueue<T>();
        }

        public void Write(ref T value, bool incomplete)
        {
            m_queue.Enqueue(ref value);

            // Move the flush up to here index
            if (!incomplete)
                m_flushToIndex = m_queue.TailPosition;
        }

        public ReaderStatus Flush()
        {
            if (m_flushFromIndex != m_flushToIndex)                            
            {
                if (Interlocked.CompareExchange(ref m_flushedIndex, m_flushToIndex, m_flushFromIndex) !=
                    m_flushFromIndex)
                {
                    //  Compare-and-swap was unseccessful because 'm_flushedIndex' is -1.
                    //  This means that the reader is asleep. Therefore we don't
                    //  care about thread-safeness and update m_flushedIndex in non-atomic
                    //  manner. We'll return false to let the caller know
                    //  that reader is sleeping.
                    m_flushedIndex = m_flushToIndex;
                    m_flushFromIndex = m_flushToIndex;
                    return ReaderStatus.Asleep;
                }

                //  Reader is alive. Nothing special to do now. Just move
                //  the 'm_flushFromIndex' index to 'm_flushToIndex'.
                m_flushFromIndex = m_flushToIndex;
            }

            return ReaderStatus.Alive;
        }

        public bool CheckRead()
        {
            if (m_queue.HeadPosition != m_unfetchedIndex && m_unfetchedIndex != -1)
                return true;

            //  There's no prefetched value, so let us prefetch more values.
            //  Prefetching is to simply retrieve the
            //  index from flushedIndex in atomic fashion. If there are no
            //  items to prefetch, set flushedIndex to -1 (using compare-and-swap).
            m_unfetchedIndex = Interlocked.CompareExchange(ref m_flushedIndex, -1, m_queue.HeadPosition);

            //  If there are no elements prefetched, exit.
            //  During pipe's lifetime unfetchedIndex should never be -1, however,
            //  it can happen during pipe shutdown when items
            //  are being deallocated.
            if (m_queue.HeadPosition == m_unfetchedIndex || m_unfetchedIndex == -1)
                return false;

            //  There was at least one value prefetched.
            return true;
        }

        public bool TryRead(out T value)
        {
            //  Try to prefetch a value.
            if (!CheckRead())
            {
                value = default(T);
                return false;
            }
            else
            {
                //  There was at least one value prefetched.
                //  Return it to the caller.
                m_queue.Dequeue(out value);
                return true;
            }
        }

        public bool Probe(Func<T, bool> predicate)
        {
            CheckRead();

            T value = default(T);

            m_queue.Peek(ref value);

            return predicate(value);
        }
    }
}
