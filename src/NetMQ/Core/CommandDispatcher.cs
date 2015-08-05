using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Core
{
    internal class CommandDispatcher
    {
        private static void SendCommand(Command command)
        {
            SocketManager.SendCommand(command.Destination.SlotId, command);
        }

        public static void SendPlug(Own destination, bool increaseSequenceNumber = true)
        {
            if (increaseSequenceNumber)
                destination.IncreaseSequenceNumber();

            var command = new PlugCommand(destination);
            SendCommand(command);
        }

        public static void SendOwn(Own destination, Own child)
        {
            destination.IncreaseSequenceNumber();

            var command = new OwnCommand(destination, child);
            SendCommand(command);
        }

        public static void SendDispose(Own destination, TimeSpan linger)
        {
            var command = new DisposeCommand(destination, linger);
            SendCommand(command);
        }

        public static void SendDisposeRequest(Own destination, Own child)
        {
            var command = new DisposeRequestCommand(destination, child);
            SendCommand(command);
        }

        public static void SendDisposeAck(Own destination)
        {
            var command = new DisposeAckCommand(destination);
            SendCommand(command);
        }

        public static void SendBind(Socket destination, Pipe pipe, bool increaseSequenceNumber)
        {
            if(increaseSequenceNumber)
                destination.IncreaseSequenceNumber();

            var command = new BindCommand(destination, pipe);
            SendCommand(command);
        }

        public static void SendInprocConnected(Socket destination)
        {
            var command = new InProcConnectedCommand(destination);
            SendCommand(command);
        }

        public static void SendActivateWrite(Pipe destination, long messagesRead)
        {
            var command = new ActivateWriteCommand(destination, messagesRead);
            SendCommand(command);
        }

        public static void SendActivateRead(Pipe destination)
        {
            var command = new ActivateReadCommand(destination);
            SendCommand(command);
        }

        public static void SendPipeDisposeAck(Pipe destination)
        {
            var command = new PipeDisposeAckCommand(destination);
            SendCommand(command);
        }

        public static void SendPipeDispose(Pipe destination)
        {
            var command = new PipeDisposeCommand(destination);
            SendCommand(command);
        }

        public static void SendHiccup(Pipe destination, YPipe<Frame> pipe)
        {
            var command = new HiccupCommand(destination, pipe);
            SendCommand(command);
        }
    }
}
