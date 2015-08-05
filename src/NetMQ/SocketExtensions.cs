using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ
{
    public static class SocketExtensions
    {
        public static void Connect(this ISocket socket, string address, params object[] args)
        {
            socket.Connect(string.Format(address, args));
        }

        public static void Bind(this ISocket socket, string address, params object[] args)
        {
            socket.Bind(string.Format(address, args));
        }

        public static void TcpConnect(this ISocket socket, string host, int port)
        {
            socket.Connect("tcp://{0}:{1}", host, port);
        }

        public static void TcpBind(this ISocket socket, string host, int port)
        {
            socket.Bind("tcp://{0}:{1}", host, port);
        }        
    }
}
