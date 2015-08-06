using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Core
{
    public class Uri
    {
        public Uri(string protocol, string address)
        {
            Protocol = protocol;
            Address = address;
        }

        public static bool TryParse(string uri, out Uri result)
        {
            int index = uri.IndexOf("://");

            if (index == -1)
            {
                result = null;
                return false;
            }

            string protocol = uri.Substring(0, index).ToLower();
            string address = uri.Substring(index + 3);

            result = new Uri(protocol, address);

            return true;
        }

        public string Protocol { get; private set; }
        public string Address { get; private set; }

        public string Host { get; private set; }
        public int Port { get; private set; }

        public bool TryResolve()
        {
            Host = Address;
            Port = 0;
            int index = Address.LastIndexOf(":");

            if (index != -1)
            {
                Host = Address.Substring(0, index);

                if (Host.Length > 2 && Host[0] == '[' && Host[Host.Length - 1] == ']')
                    Host = Host.Substring(1, Host.Length - 2);

                int port;

                if (int.TryParse(Address.Substring(index + 1), out port))
                {
                    Port = port;
                    return true;
                }
            }

            Host = string.Empty;
            Port = 0;

            return false;
        }

        protected bool Equals(Uri other)
        {
            return string.Equals(Protocol, other.Protocol) && string.Equals(Address, other.Address);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Uri)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Protocol != null ? Protocol.GetHashCode() : 0) * 397) ^ (Address != null ? Address.GetHashCode() : 0);
            }
        }
    }
}
