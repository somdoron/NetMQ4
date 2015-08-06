using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ.Transport.Tcp;

namespace NetMQ.Transport
{
    static public class TransportManager
    {
        public static bool TryCreateTransport(Socket socket,string protocol, out BaseTransport transport)
        {
            if (protocol == "tcp")
            {
                transport = new TcpTransport(socket.Options);
                return true;
            }

            transport = null;
            return false;            
        }     
    }
}
