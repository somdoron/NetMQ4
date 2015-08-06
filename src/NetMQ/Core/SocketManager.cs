using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ;

namespace NetMQ.Core
{
    public class SocketManager
    {
        public const int IOThreadSlotId = 0;
        const int MaximumSockets = 1023;

        private static bool s_active;
        private static object s_slotSync;
        private static IMailbox[] s_slots;
        private static Queue<int> s_emptySlots;
        private static IOThread s_ioThread;        

        static SocketManager()
        {
            s_active = false;
            s_slotSync = new object();
            s_emptySlots = new Queue<int>();
            s_slots = new IMailbox[MaximumSockets + 1];

            for (int i = 1; i < s_slots.Length; i++)
            {
                s_emptySlots.Enqueue(i);
            }           
        }

        

        static private void Activate()
        {
            s_active = true;

            //  Create I/O thread object and launch it
            s_ioThread = new IOThread(0);
            s_slots[0] = s_ioThread.Mailbox;
            s_ioThread.Start();
        }

        static private void Deactivate()
        {
            s_active = false;

            s_ioThread.Dispose();
            s_ioThread = null;
        }

        internal static int AddMailbox(IMailbox mailbox)
        {
            lock (s_slotSync)
            {
                if (!s_active)
                {
                    Activate();
                }

                //  If max_sockets limit was reached, throw exception.
                if (s_emptySlots.Count == 0)
                {
                    throw new NetMQException(ErrorCode.TooManyOpenSockets);
                }

                //  Choose a slot for the socket.
                int slot = s_emptySlots.Dequeue();
                s_slots[slot] = mailbox;

                return slot;
            }
        }

        internal static void RemoveMailbox(int slotId)
        {
            lock (s_slotSync)
            {
                s_emptySlots.Enqueue(slotId);
                s_slots[slotId] = null;

                // all sockets are disposed, lets kill the io thread
                if (s_emptySlots.Count == MaximumSockets)
                {
                    Deactivate();
                }
            }
        }

        internal static void SendCommand(int slotId, Command command)
        {
            s_slots[slotId].Send(command);
        }


    }
}
