using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ.Core
{
    class Uri
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
    }
}
