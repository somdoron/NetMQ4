using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQ.Core
{
    class AtomicCounter
    {
        private long m_value;

        public AtomicCounter(long value)
        {            
            m_value = value;
        }
        public void Add(long i)
        {
            Interlocked.Add(ref m_value, i);
        }

        public long Get()
        {
            return Interlocked.Read(ref m_value);
        }
    }
}
