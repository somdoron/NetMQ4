using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using AsyncIO;
using NetMQ.Core;
using Uri = NetMQ.Core.Uri;

namespace NetMQ.Transport.Tcp
{
    class TcpListener : Own
    {
        private const SocketOptionName IPv6Only = (SocketOptionName)27;
        
        private IPEndPoint m_endPoint;
        private AsyncSocket m_handle;

        public TcpListener(SocketOptions options)
            : base(options)
        {         
        }

        public void SetAddress(Uri uri)
        {
            IPAddress ipAddress;

            if (uri.Host == "*")
                ipAddress = IPAddress.IPv6Any;
            else if (!IPAddress.TryParse(uri.Host, out ipAddress))
            {
                var availableAddresses = Dns.GetHostEntry(uri.Host).AddressList;

                // higher priority to IPv6
                ipAddress = availableAddresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetworkV6);

                if (ipAddress == null)
                    ipAddress = availableAddresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);

                if (ipAddress == null)
                    throw new NetMQException(ErrorCode.Invalid, string.Format("unable to find an IP address for {0}", uri.Host));
            }

            m_endPoint = new IPEndPoint(ipAddress, uri.Port);

            try
            {
                m_handle = AsyncSocket.Create(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                if (ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    try
                    {
                        // This is not supported on old windows operation system and might throw exception
                        m_handle.SetSocketOption(SocketOptionLevel.IPv6, IPv6Only, 0);
                    }
                    catch
                    {

                    }
                }

                m_handle.Bind(m_endPoint);
                m_handle.Listen(Options.Backlog);
            }
            catch (SocketException ex)
            {
                if (m_handle != null)
                {
                    m_handle.Dispose();
                    m_handle = null;
                }

                throw NetMQException.FromSocketError(ex.SocketErrorCode);
            }
        }

        internal override void Process(PlugCommand command)
        {
            base.Process(command);
        }

        internal override void Process(DisposeCommand command)
        {
            base.Process(command);
        }

        protected override void ProcessDisposed()
        {
            
        }
    }
}
