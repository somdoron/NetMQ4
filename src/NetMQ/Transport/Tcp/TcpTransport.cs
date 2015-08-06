using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ.Core;
using Uri = NetMQ.Core.Uri;

namespace NetMQ.Transport.Tcp
{
    // TODO: should this be subclass of Own and launch it's own chidls?
    class TcpTransport : BaseTransport
    {
        

        private IDictionary<Uri, TcpListener> m_endpoints;

        public TcpTransport(SocketOptions options)
            : base(options)
        {
            m_endpoints = new Dictionary<Uri, TcpListener>();            
        }

        public override bool ValidateBindUri(Uri uri)
        {
            return uri.TryResolve();
        }

        public override void Bind(Uri uri)
        {
            TcpListener tcpListener = new TcpListener(Options);
            tcpListener.SetAddress(uri);

            LaunchChild(tcpListener);
            m_endpoints.Add(uri, tcpListener);
        }
    }
}
