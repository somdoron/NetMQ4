using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetMQ.Core;
using Uri = NetMQ.Core.Uri;

namespace NetMQ.Transport
{
    public abstract class BaseTransport : Own
    {
        public BaseTransport(SocketOptions options)
            : base(options)
        {
        }        

        public abstract bool ValidateBindUri(Uri uri);

        public abstract void Bind(Uri uri);

        protected override void ProcessDisposed()
        {
            
        }
    }
}
