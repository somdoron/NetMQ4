using System;

namespace NetMQ
{
    public class NetMQException : Exception
    {
        public ErrorCode ErrorCode { get; private set; }

        public NetMQException(ErrorCode errorCode)
            : base("")
        {
            ErrorCode = errorCode;
        }
    }
}