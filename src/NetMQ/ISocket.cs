﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetMQ
{
    public interface ISocket : IDisposable
    {
        void Bind(string address);
        void Unbind(string address);

        void Connect(string address);
        void Disconnect(string address);              

        SocketOptions Options { get; }
    }
}
