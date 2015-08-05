using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Core
{
    class IOThread : BaseObject, IDisposable
    {
        public IOThread(int slotId)
            : base(slotId)
        {

        }

        public IMailbox Mailbox { get; set; }

        public void Start()
        {

        }

        public void Dispose()
        {
            
        }
    }
}
