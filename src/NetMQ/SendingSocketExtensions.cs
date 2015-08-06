using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQ
{
    public static class SendingSocketExtensions
    {
        /// <summary>
        /// Block until the frame is can be sent.
        /// </summary>
        /// <remarks>
        /// The call  blocks until the frame can be sent and cannot be interrupted. 
        /// Wether the frame can be sent depends on the socket type.
        /// </remarks>
        /// <param name="socket">The socket to send the message on.</param>
        /// <param name="frame">An object with message's data to send.</param>        
        public static void SendFrame(this ISend socket, ref Frame frame)
        {
            var result = socket.TrySendFrame(ref frame, Timeout.InfiniteTimeSpan);
            Debug.Assert(result);
        }

        #region Sending Byte Array

        #region Blocking

        /// <summary>
        /// Transmit a byte-array of data over this socket, block until frame is sent.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>
        /// <param name="data">the byte-array of data to send.</param>        
        /// <param name="more">set this flag to true to signal that you will be immediately sending another frame (optional: default is false)</param>
        public static void SendFrame(this ISend socket, byte[] data, bool more = false)
        {
            SendFrame(socket, data, data.Length, more);
        }

        /// <summary>
        /// Transmit a byte-array of data over this socket, block until frame is sent.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>
        /// <param name="data">the byte-array of data to send.</param>
        /// <param name="length">the number of bytes to send from <paramref name="data"/>.</param>
        /// <param name="more">set this flag to true to signal that you will be immediately sending another frame (optional: default is false)</param>
        public static void SendFrame(this ISend socket, byte[] data, int length, bool more = false)
        {
            var frame = new Frame(length);
            Buffer.BlockCopy(data, 0, frame.Data, 0, length);
            frame.More = more;
                     
            socket.SendFrame(ref frame);  
            frame.Close();
        }

        /// <summary>
        /// Transmit a byte-array of data over this socket, block until frame is sent.
        /// Send more frame, another frame must be sent after this frame. Use to chain Send methods.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>
        /// <param name="data">the byte-array of data to send.</param>                
        public static ISend SendMoreFrame(this ISend socket, byte[] data)
        {
            SendFrame(socket, data, true);

            return socket;
        }

        /// <summary>
        /// Transmit a byte-array of data over this socket, block until frame is sent.
        /// Send more frame, another frame must be sent after this frame. Use to chain Send methods.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>
        /// <param name="data">the byte-array of data to send.</param>        
        /// <param name="length">the number of bytes to send from <paramref name="data"/>.</param>        
        public static ISend SendMoreFrame(this ISend socket, byte[] data, int length)
        {
            SendFrame(socket, data, length, true);

            return socket;
        }

        #endregion

        #region Timeout

        /// <summary>
        /// Attempt to transmit a single frame on <paramref cref="socket"/>.
        /// If message cannot be sent within <paramref name="timeout"/>, return <c>false</c>.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>
        /// <param name="timeout">The maximum period of time to try to send a message.</param>
        /// <param name="data">the byte-array of data to send.</param>        
        /// <param name="length">the number of bytes to send from <paramref name="data"/>.</param>
        /// <param name="more">set this flag to true to signal that you will be immediately sending another frame (optional: default is false)</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TrySendFrame(this ISend socket, TimeSpan timeout, byte[] data, int length, bool more = false)
        {
            var frame = new Frame(length);
            Buffer.BlockCopy(data, 0, frame.Data, 0, length);
            frame.More = more;
            
            if (!socket.TrySendFrame(ref frame, timeout))
            {
                frame.Close();
                return false;
            }

            frame.Close();
            return true;
        }

        /// <summary>
        /// Attempt to transmit a single frame on <paramref cref="socket"/>.
        /// If message cannot be sent within <paramref name="timeout"/>, return <c>false</c>.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>
        /// <param name="timeout">The maximum period of time to try to send a message.</param>
        /// <param name="data">the byte-array of data to send.</param>                
        /// <param name="more">set this flag to true to signal that you will be immediately sending another frame (optional: default is false)</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TrySendFrame(this ISend socket, TimeSpan timeout, byte[] data, bool more = false)
        {
            return TrySendFrame(socket, timeout, data, data.Length, more);
        }

        #endregion

        #region Immediate

        /// <summary>
        /// Attempt to transmit a single frame on <paramref cref="socket"/>.
        /// If message cannot be sent immediately, return <c>false</c>.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>        
        /// <param name="data">the byte-array of data to send.</param>                
        /// <param name="more">set this flag to true to signal that you will be immediately sending another frame (optional: default is false)</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TrySendFrame(this ISend socket, byte[] data, bool more = false)
        {
            return TrySendFrame(socket, TimeSpan.Zero, data, more);
        }

        /// <summary>
        /// Attempt to transmit a single frame on <paramref cref="socket"/>.
        /// If message cannot be sent immediately, return <c>false</c>.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>        
        /// <param name="data">the byte-array of data to send.</param>            
        /// <param name="length">the number of bytes to send from <paramref name="data"/>.</param>    
        /// <param name="more">set this flag to true to signal that you will be immediately sending another frame (optional: default is false)</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TrySendFrame(this ISend socket, byte[] data, int length, bool more = false)
        {
            return TrySendFrame(socket, TimeSpan.Zero, data, length, more);
        }

        #endregion

        #endregion

        #region Sending Strings

        #region Blocking

        /// <summary>
        /// Transmit a string over this socket, block until frame is sent.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>
        /// <param name="message">the string to send</param>        
        /// <param name="more">set this flag to true to signal that you will be immediately sending another frame (optional: default is false)</param>
        public static void SendFrame(this ISend socket, string message, bool more = false)
        {
            // Count the number of bytes required to encode the string.
            // Note that non-ASCII strings may not have an equal number of characters
            // and bytes. The encoding must be queried for this answer.
            // With this number, request a buffer from the pool.
            var frame = new Frame(SendReceiveConstants.DefaultEncoding.GetByteCount(message));
            frame.More = more;

            // Encode the string into the buffer
            SendReceiveConstants.DefaultEncoding.GetBytes(message, 0, message.Length, frame.Data, 0);

            socket.SendFrame(ref frame);
            frame.Close();
        }

        /// <summary>
        /// Transmit a string over this socket, block until frame is sent.
        /// Send more frame, another frame must be sent after this frame. Use to chain Send methods.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>
        /// <param name="message">the string to send</param>           
        public static ISend SendMoreFrame(this ISend socket, string message)
        {
            SendFrame(socket, message, true);

            return socket;
        }

        #endregion

        #region Timeout

        /// <summary>
        /// Attempt to transmit a single string frame on <paramref cref="socket"/>.
        /// If message cannot be sent within <paramref name="timeout"/>, return <c>false</c>.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>
        /// <param name="timeout">The maximum period of time to try to send a message.</param>
        /// <param name="message">the string to send</param>                
        /// <param name="more">set this flag to true to signal that you will be immediately sending another frame (optional: default is false)</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TrySendFrame(this ISend socket, TimeSpan timeout, string message, bool more = false)
        {
            // Count the number of bytes required to encode the string.
            // Note that non-ASCII strings may not have an equal number of characters
            // and bytes. The encoding must be queried for this answer.
            // With this number, request a buffer from the pool.
            var frame = new Frame(SendReceiveConstants.DefaultEncoding.GetByteCount(message));
            frame.More = more;

            // Encode the string into the buffer
            SendReceiveConstants.DefaultEncoding.GetBytes(message, 0, message.Length, frame.Data, 0);

            if (!socket.TrySendFrame(ref frame, timeout))
            {
                frame.Close();
                return false;
            }

            frame.Close();
            return true;
        }

        #endregion

        #region Immediate

        /// <summary>
        /// Attempt to transmit a single string frame on <paramref cref="socket"/>.
        // If message cannot be sent immediately, return <c>false</c>.
        /// </summary>
        /// <param name="socket">the ISend to transmit on</param>        
        /// <param name="message">the string to send</param>                
        /// <param name="more">set this flag to true to signal that you will be immediately sending another frame (optional: default is false)</param>
        /// <returns><c>true</c> if a message was available, otherwise <c>false</c>.</returns>
        public static bool TrySendFrame(this ISend socket, string message, bool more = false)
        {
            return TrySendFrame(socket, TimeSpan.Zero, message, more);
        }

        #endregion

        #endregion


        
    }
}
