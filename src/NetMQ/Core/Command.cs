using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Core
{    
    abstract class Command
    {
        protected Command(BaseObject destination)
        {
            Destination = destination;
        }

        public BaseObject Destination { get; private set; } 
    }

    class PlugCommand : Command
    {
        public PlugCommand(BaseObject destination)
            : base(destination)
        {
        }        
    }

    class OwnCommand : Command
    {
        public OwnCommand(BaseObject destination, Own child)
            : base(destination)
        {
            Child = child;
        }
        
        public Own Child { get; private set; }
    }

    class AttachCommand : Command
    {
        public AttachCommand(BaseObject destination, IEngine engine)
            : base(destination)
        {
            Engine = engine;
        }

        public IEngine Engine { get; private set; }
    }

    class BindCommand : Command
    {
        public BindCommand(BaseObject destination, Pipe pipe)
            : base(destination)
        {
            Pipe = pipe;
        }

        public Pipe Pipe { get; private set; }
    }

    class ActivateReadCommand : Command
    {
        public ActivateReadCommand(BaseObject destination)
            : base(destination)
        {
        }
    }

    class ActivateWriteCommand : Command
    {
        public ActivateWriteCommand(BaseObject destination, long messagesRead)
            : base(destination)
        {
            MessagesRead = messagesRead;
        }        

        public long MessagesRead { get; private set; }
    }

    class HiccupCommand : Command
    {
        public HiccupCommand(BaseObject destination, YPipe<Frame> pipe)
            : base(destination)
        {
            Pipe = pipe;
        }

        public YPipe<Frame>  Pipe { get; private set; }
    }

    class ClosePipeCommand : Command
    {
        public ClosePipeCommand(BaseObject destination)
            : base(destination)
        {
        }        
    }

    class CloseRequestCommand : Command
    {
        public CloseRequestCommand(BaseObject destination, Own child)
            : base(destination)
        {
            Child = child;            
        }

        public Own Child { get; private set; }
    }

    class CloseCommand : Command
    {
        public CloseCommand(BaseObject destination, TimeSpan linger)
            : base(destination)
        {
            Linger = linger;
        }

        public TimeSpan Linger { get; private set; }
    }

    class CloseAckCommand : Command
    {
        public CloseAckCommand(BaseObject destination)
            : base(destination)
        {
        }        
    }

    class DoneCommand : Command
    {
        public DoneCommand(BaseObject destination)
            : base(destination)
        {
        }        
    }

    class InProcConnectedCommand : Command
    {
        public InProcConnectedCommand(BaseObject destination) : base(destination)
        {
        }
    }
}
