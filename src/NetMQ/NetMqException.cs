using System;
using System.Net.Sockets;

namespace NetMQ
{
    public class NetMQException : Exception
    {
        public ErrorCode ErrorCode { get; private set; }

        public NetMQException(ErrorCode errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public NetMQException(ErrorCode errorCode)
            : base("")
        {
            ErrorCode = errorCode;
        }

        public static NetMQException FromSocketError(SocketError socketError)
        {
            ErrorCode errorCode = ErrorCode.Unknown;

            switch (socketError)
            {
                case SocketError.InvalidArgument:
                    errorCode = ErrorCode.Invalid;
                    break;
                case SocketError.TooManyOpenSockets:
                    errorCode = ErrorCode.TooManyOpenSockets;
                    break;
                case SocketError.AddressAlreadyInUse:
                    errorCode = ErrorCode.AddressAlreadyInUse;
                    break;
            }

            return new NetMQException(errorCode);
        }

    }
}