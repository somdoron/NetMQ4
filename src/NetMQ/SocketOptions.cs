using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQ
{
    public class SocketOptions
    {
        public SocketOptions()
        {            
            ReconnenctInterval = TimeSpan.FromMilliseconds(100);
            ReconnectIntervalMaximum = TimeSpan.Zero;
            Linger = Timeout.InfiniteTimeSpan;
            Backlog = 100;
            SendHighWatermark = 1000;
            ReceiveHighwatermark = 1000;
            ReceiveIdentity = false;
        }
        
        public int Backlog { get; set; }

        public TimeSpan Linger { get; set; }

        public TimeSpan ReconnenctInterval { get; set; }

        public TimeSpan ReconnectIntervalMaximum { get; set; }

        public int SendHighWatermark { get; set; }

        public int ReceiveHighwatermark { get; set; }

        internal bool ReceiveIdentity { get; set; }
    }
}
