﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Core
{
    /// <summary>
    /// Base class for all objects that participate in inter-thread
    //  communication.
    /// </summary>
    public class BaseObject
    {
        public BaseObject()
        {
            
        }

        internal BaseObject(int slotId)
        {
            SlotId = slotId;
        }

        internal BaseObject(BaseObject parent)
        {
            SlotId = parent.SlotId;
        }

        internal protected int SlotId { get; internal set; }
        
        internal virtual void Process(PlugCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(OwnCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(AttachCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(BindCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(ActivateReadCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(ActivateWriteCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(HiccupCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(ClosePipeCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(CloseRequestCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(CloseCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(CloseAckCommand command)
        {
            throw new NotImplementedException();
        }

        internal virtual void Process(InProcConnectedCommand command)
        {
            throw new NotImplementedException();
        }        
    }
}
