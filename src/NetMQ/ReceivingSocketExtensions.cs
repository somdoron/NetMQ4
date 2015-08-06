using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQ
{
    public static class ReceivingSocketExtensions
    {
        /// <summary>
        /// Block until the next message arrives, then make the message's data available via <paramref name="frame"/>.
        /// </summary>
        /// <remarks>
        /// The call  blocks until the next message arrives, and cannot be interrupted. This a convenient and safe when
        /// you know a message is available.
        /// </remarks>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="frame">An object to receive the frame's data into.</param>
        public static void ReceiveFrame(this IReceive socket, ref Frame frame)
        {
            var result = socket.TryReceiveFrame(ref frame, Timeout.InfiniteTimeSpan);
            Debug.Assert(result);
        }

        #region Receiving a frame as a byte array

        #region Blocking

        /// <summary>
        /// Receive a single frame from <paramref cref="socket"/>, blocking until one arrives.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <returns>The content of the received message frame.</returns>        
        public static byte[] ReceiveFrameBytes(this IReceive socket)
        {
            bool more;
            return socket.ReceiveFrameBytes(out more);
        }

        /// <summary>
        /// Receive a single frame from <paramref cref="socket"/>, blocking until one arrives.
        /// Indicate whether further frames exist via <paramref name="more"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        /// <returns>The content of the received message frame.</returns>        
        public static byte[] ReceiveFrameBytes(this IReceive socket, out bool more)
        {
            var frame = new Frame();            

            socket.ReceiveFrame(ref frame);

            var data = frame.CloneData();

            more = frame.More;

            frame.Close();
            return data;
        }

        /// <summary>
        /// Receive a single frame from <paramref cref="socket"/>, blocking until one arrives.
        /// Indicate whether further frames exist via <paramref name="more"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>        
        /// <param name="bytes">Array to populate with the frame data. If pre-allocated and large enough the allocated array will be used. Otherwise new array will be allocated.</param>                
        /// <param name="length">The length of the frame receieved.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        public static void ReceiveFrameBytes(this IReceive socket, ref byte[] bytes, out int length, out bool more)
        {
            var frame = new Frame();
            socket.ReceiveFrame(ref frame);

            if (bytes != null && bytes.Length >= frame.Length)
                frame.CopyDataTo(bytes);
            else
                bytes = frame.CloneData();

            length = frame.Length;
            more = frame.More;

            frame.Close();            
        }

        #endregion

        #region Immediate

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>.
        /// If no message is immediately available, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="bytes">The content of the received message frame, or <c>null</c> if no message was available.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameBytes(this IReceive socket, out byte[] bytes)
        {
            bool more;
            return socket.TryReceiveFrameBytes(out bytes, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>.
        /// If no message is immediately available, return <c>false</c>.
        /// Indicate whether further frames exist via <paramref name="more"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        /// <param name="bytes">The content of the received message frame, or <c>null</c> if no message was available.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameBytes(this IReceive socket, out byte[] bytes, out bool more)
        {
            return socket.TryReceiveFrameBytes(TimeSpan.Zero, out bytes, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>. Reusing existing bytes-array if large enough.
        /// If no message is immediately available, return <c>false</c>.
        /// Indicate whether further frames exist via <paramref name="more"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="bytes">The content of the received message frame. If pre-allocated and large enough the allocated array will be used. Otherwise new array will be allocated.</param>
        /// <param name="length">The length of the frame receieved.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>        
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameBytes(this IReceive socket, ref byte[] bytes, out int length, out bool more)
        {
            return socket.TryReceiveFrameBytes(TimeSpan.Zero, ref bytes, out length, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>. Reusing existing bytes-array if large enough.
        /// If no message is immediately available, return <c>false</c>.        
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="bytes">The content of the received message frame. If pre-allocated and large enough the allocated array will be used. Otherwise new array will be allocated.</param>
        /// <param name="length">The length of the frame receieved.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameBytes(this IReceive socket, ref byte[] bytes, out int length)
        {
            return socket.TryReceiveFrameBytes(TimeSpan.Zero, ref bytes, out length);
        }

        #endregion

        #region Timeout

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>.
        /// If no message is available within <paramref name="timeout"/>, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="timeout">The maximum period of time to wait for a message to become available.</param>
        /// <param name="bytes">The content of the received message frame, or <c>null</c> if no message was available.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameBytes(this IReceive socket, TimeSpan timeout, out byte[] bytes)
        {
            bool more;
            return socket.TryReceiveFrameBytes(timeout, out bytes, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>.
        /// If no message is available within <paramref name="timeout"/>, return <c>false</c>.
        /// Indicate whether further frames exist via <paramref name="more"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        /// <param name="timeout">The maximum period of time to wait for a message to become available.</param>
        /// <param name="bytes">The content of the received message frame, or <c>null</c> if no message was available.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameBytes(this IReceive socket, TimeSpan timeout, out byte[] bytes, out bool more)
        {
            var frame = new Frame();           

            if (!socket.TryReceiveFrame(ref frame, timeout))
            {
                frame.Close();
                bytes = null;
                more = false;
                return false;
            }

            bytes = frame.CloneData();
            more = frame.More;

            frame.Close();
            return true;
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>.  Reusing existing bytes-array if large enough.
        /// If no message is available within <paramref name="timeout"/>, return <c>false</c>.
        /// Indicate whether further frames exist via <paramref name="more"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="timeout">The maximum period of time to wait for a message to become available.</param>
        /// <param name="bytes">The content of the received message frame. If pre-allocated and large enough the allocated array will be used. Otherwise new array will be allocated.</param>
        /// <param name="length">The length of the frame receieved.</param>        
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>               
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameBytes(this IReceive socket, TimeSpan timeout, ref byte[] bytes, out int length, out bool more)
        {
            var frame = new Frame();

            if (!socket.TryReceiveFrame(ref frame, timeout))
            {
                frame.Close();
                bytes = null;
                more = false;
                length = 0;
                return false;
            }

            if (bytes != null && bytes.Length >= frame.Length)
                frame.CopyDataTo(bytes);
            else
                bytes = frame.CloneData();

            length = frame.Length;            
            more = frame.More;

            frame.Close();
            return true;
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>.  Reusing existing bytes-array if large enough.
        /// If no message is available within <paramref name="timeout"/>, return <c>false</c>.        
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="timeout">The maximum period of time to wait for a message to become available.</param>
        /// <param name="bytes">The content of the received message frame. If pre-allocated and large enough the allocated array will be used. Otherwise new array will be allocated.</param>
        /// <param name="length">The length of the frame receieved.</param>                
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameBytes(this IReceive socket, TimeSpan timeout, ref byte[] bytes, out int length)
        {
            bool more;
            return socket.TryReceiveFrameBytes(timeout, ref bytes, out length, out more);
        }

        #endregion

        #endregion

        #region Receiving a frame as a string

        #region Blocking

        /// <summary>
        /// Receive a single frame from <paramref cref="socket"/>, blocking until one arrives, and decode as a string using <see cref="SendReceiveConstants.DefaultEncoding"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <returns>The content of the received message frame as a string.</returns>
        public static string ReceiveFrameString(this IReceive socket)
        {
            bool more;
            return socket.ReceiveFrameString(SendReceiveConstants.DefaultEncoding, out more);
        }

        /// <summary>
        /// Receive a single frame from <paramref cref="socket"/>, blocking until one arrives, and decode as a string using <see cref="SendReceiveConstants.DefaultEncoding"/>.
        /// Indicate whether further frames exist via <paramref name="more"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        /// <returns>The content of the received message frame.</returns>        
        public static string ReceiveFrameString(this IReceive socket, out bool more)
        {
            return socket.ReceiveFrameString(SendReceiveConstants.DefaultEncoding, out more);
        }

        /// <summary>
        /// Receive a single frame from <paramref cref="socket"/>, blocking until one arrives, and decode as a string using <paramref name="encoding"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="encoding">The encoding used to convert the frame's data to a string.</param>
        /// <returns>The content of the received message frame as a string.</returns>        
        public static string ReceiveFrameString(this IReceive socket, Encoding encoding)
        {
            bool more;
            return socket.ReceiveFrameString(encoding, out more);
        }

        /// <summary>
        /// Receive a single frame from <paramref cref="socket"/>, blocking until one arrives, and decode as a string using <paramref name="encoding"/>.
        /// Indicate whether further frames exist via <paramref name="more"/>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="encoding">The encoding used to convert the frame's data to a string.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        /// <returns>The content of the received message frame as a string.</returns>        
        public static string ReceiveFrameString(this IReceive socket, Encoding encoding, out bool more)
        {
            var frame = new Frame();

            socket.ReceiveFrame(ref frame);

            more = frame.More;

            var str = frame.Length > 0
                ? encoding.GetString(frame.Data, 0, frame.Length)
                : string.Empty;

            frame.Close();
            return str;
        }

        #endregion

        #region Immediate

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>, and decode as a string using <see cref="SendReceiveConstants.DefaultEncoding"/>.
        /// If no message is immediately available, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="frameString">The content of the received message frame as a string, or <c>null</c> if no message was available.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameString(this IReceive socket, out string frameString)
        {
            bool more;
            return socket.TryReceiveFrameString(TimeSpan.Zero, SendReceiveConstants.DefaultEncoding, out frameString, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>, and decode as a string using <see cref="SendReceiveConstants.DefaultEncoding"/>.
        /// If no message is immediately available, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="frameString">The content of the received message frame as a string, or <c>null</c> if no message was available.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameString(this IReceive socket, out string frameString, out bool more)
        {
            return socket.TryReceiveFrameString(TimeSpan.Zero, SendReceiveConstants.DefaultEncoding, out frameString, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>, and decode as a string using <paramref name="encoding"/>.
        /// If no message is immediately available, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="encoding">The encoding used to convert the frame's data to a string.</param>
        /// <param name="frameString">The content of the received message frame as a string, or <c>null</c> if no message was available.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameString(this IReceive socket, Encoding encoding, out string frameString)
        {
            bool more;
            return socket.TryReceiveFrameString(TimeSpan.Zero, encoding, out frameString, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>, and decode as a string using <paramref name="encoding"/>.
        /// If no message is immediately available, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="encoding">The encoding used to convert the frame's data to a string.</param>
        /// <param name="frameString">The content of the received message frame as a string, or <c>null</c> if no message was available.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameString(this IReceive socket, Encoding encoding, out string frameString, out bool more)
        {
            return socket.TryReceiveFrameString(TimeSpan.Zero, encoding, out frameString, out more);
        }

        #endregion

        #region Timeout

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>, and decode as a string using <see cref="SendReceiveConstants.DefaultEncoding"/>.
        /// If no message is available within <paramref name="timeout"/>, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="timeout">The maximum period of time to wait for a message to become available.</param>
        /// <param name="frameString">The content of the received message frame as a string, or <c>null</c> if no message was available.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameString(this IReceive socket, TimeSpan timeout, out string frameString)
        {
            bool more;
            return socket.TryReceiveFrameString(timeout, SendReceiveConstants.DefaultEncoding, out frameString, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>, and decode as a string using <see cref="SendReceiveConstants.DefaultEncoding"/>.
        /// If no message is available within <paramref name="timeout"/>, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="timeout">The maximum period of time to wait for a message to become available.</param>
        /// <param name="frameString">The content of the received message frame as a string, or <c>null</c> if no message was available.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameString(this IReceive socket, TimeSpan timeout, out string frameString, out bool more)
        {
            return socket.TryReceiveFrameString(timeout, SendReceiveConstants.DefaultEncoding, out frameString, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>, and decode as a string using <paramref name="encoding"/>.
        /// If no message is available within <paramref name="timeout"/>, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="timeout">The maximum period of time to wait for a message to become available.</param>
        /// <param name="encoding">The encoding used to convert the frame's data to a string.</param>
        /// <param name="frameString">The content of the received message frame as a string, or <c>null</c> if no message was available.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameString(this IReceive socket, TimeSpan timeout, Encoding encoding, out string frameString)
        {
            bool more;
            return socket.TryReceiveFrameString(timeout, encoding, out frameString, out more);
        }

        /// <summary>
        /// Attempt to receive a single frame from <paramref cref="socket"/>, and decode as a string using <paramref name="encoding"/>.
        /// If no message is available within <paramref name="timeout"/>, return <c>false</c>.
        /// </summary>
        /// <param name="socket">The socket to receive from.</param>
        /// <param name="timeout">The maximum period of time to wait for a message to become available.</param>
        /// <param name="encoding">The encoding used to convert the frame's data to a string.</param>
        /// <param name="frameString">The content of the received message frame as a string, or <c>null</c> if no message was available.</param>
        /// <param name="more"><c>true</c> if another frame of the same message follows, otherwise <c>false</c>.</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TryReceiveFrameString(this IReceive socket, TimeSpan timeout, Encoding encoding, out string frameString, out bool more)
        {
            var frame = new Frame();

            if (socket.TryReceiveFrame(ref frame, timeout))
            {
                more = frame.More;

                frameString = frame.Length > 0
                    ? encoding.GetString(frame.Data, 0, frame.Length)
                    : string.Empty;

                frame.Close();
                return true;
            }

            frameString = null;
            more = false;
            frame.Close();
            return false;
        }

        #endregion

        #endregion

    }
}
