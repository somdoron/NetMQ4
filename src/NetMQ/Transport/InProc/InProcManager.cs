using System;
using System.Collections.Generic;
using NetMQ.Core;

namespace NetMQ.Transport.InProc
{
    static class InProcManager
    {
        class PendingConnection
        {
            public PendingConnection(Socket socket, Pipe connectPipe, Pipe bindPipe)
            {
                Socket = socket;
                ConnectPipe = connectPipe;
                BindPipe = bindPipe;
            }

            public Socket Socket { get; private set; }
            public Pipe ConnectPipe { get; set; }
            public Pipe BindPipe { get; set; }
        }

        private static Dictionary<string, Socket> s_endpoints;
        private static Dictionary<string, List<PendingConnection>> s_pendingConnections;

        private static object s_endpointsSync;

        static InProcManager()
        {
            s_endpoints = new Dictionary<string, Socket>();
            s_pendingConnections = new Dictionary<string, List<PendingConnection>>();
            s_endpointsSync = new object();
        }

        public static bool TryRegisterEndpoint(Socket socket, string address)
        {
            lock (s_endpointsSync)
            {
                if (!s_endpoints.ContainsKey(address))
                {
                    s_endpoints.Add(address, socket);

                    List<PendingConnection> connections;

                    // try to connect pending connection
                    if (s_pendingConnections.TryGetValue(address, out connections))
                    {
                        foreach (var pendingConnection in connections)
                        {
                            ConnectInprocSockets(socket, pendingConnection);
                        }

                        connections.Clear();
                    }

                    return true;
                }

                return false;
            }
        }

        public static bool TryUnregisterEndpoint(Socket socket, string address)
        {
            lock (s_endpointsSync)
            {
                Socket temp;

                if (s_endpoints.TryGetValue(address, out temp) && Object.ReferenceEquals(temp, socket))
                {
                    s_endpoints.Remove(address);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static void UnregisterEndpoints(Socket socket)
        {
            lock (s_endpointsSync)
            {
                List<string> deleteList = new List<string>();

                foreach (var endpoint in s_endpoints)
                {
                    if (endpoint.Value == socket)
                        deleteList.Add(endpoint.Key);
                }


                foreach (var address in deleteList)
                {
                    s_endpoints.Remove(address);
                }
            }
        }

        internal static Pipe ConnectEndpoint(Socket connectSocket, string address)
        {
            lock (s_endpointsSync)
            {
                Socket boundSocket;

                int connectHighWatermark = connectSocket.Options.SendHighWatermark;
                int bindHighWatermark = connectSocket.Options.ReceiveHighwatermark;

                if (s_endpoints.TryGetValue(address, out boundSocket))
                {
                    //  Increment the command sequence number of the peer so that it won't
                    //  get deallocated until "bind" command is issued by the caller.
                    //  The subsequent 'bind' has to be called with inc_seqnum parameter
                    //  set to false, so that the seqnum isn't incremented twice.
                    boundSocket.IncreaseSequenceNumber();

                    if (connectHighWatermark != 0 && boundSocket.Options.ReceiveHighwatermark != 0)
                        connectHighWatermark += boundSocket.Options.ReceiveHighwatermark;

                    if (bindHighWatermark != 0 && boundSocket.Options.SendHighWatermark != 0)
                        bindHighWatermark += boundSocket.Options.SendHighWatermark;
                }

                Pipe connectPipe;
                Pipe bindPipe;

                Pipe.CreatePair(connectSocket, boundSocket ?? connectSocket, 
                    bindHighWatermark, connectHighWatermark, out connectPipe, out bindPipe);

                if (boundSocket == null)
                {
                    //  The peer doesn't exist yet so we don't know whether
                    //  to send the identity message or not. To resolve this,
                    //  we always send our identity and drop it later if
                    //  the peer doesn't expect it.
                    Frame identity = new Frame(0);
                    identity.Identity = true;
                    connectPipe.TryWrite(ref identity);
                    connectPipe.Flush();

                    connectSocket.IncreaseSequenceNumber();
                    AddPendingConnection(connectSocket, address, connectPipe, bindPipe);
                }
                else
                {
                    //  If required, send the identity of the local socket to the peer.
                    if (boundSocket.Options.ReceiveIdentity)
                    {
                        Frame identity = new Frame(0);
                        identity.Identity = true;
                        connectPipe.TryWrite(ref identity);
                        connectPipe.Flush();
                    }

                    //  If required, send the identity of the peer to the local socket.
                    if (connectSocket.Options.ReceiveIdentity)
                    {
                        Frame identity = new Frame(0);
                        identity.Identity = true;
                        bindPipe.TryWrite(ref identity);
                        bindPipe.Flush();
                    }

                    //  Attach remote end of the pipe to the peer socket. Note that peer's
                    //  sequence nubmer was incremented in find endpoint function. We don't need it
                    //  increased here.
                    CommandDispatcher.SendBind(boundSocket, bindPipe, false);
                }

                return connectPipe;
            }            
        }

        private static void AddPendingConnection(Socket socket, string address, Pipe connectPipe, Pipe bindPipe)
        {                        
            List<PendingConnection> pendingConnections;

            if (!s_pendingConnections.TryGetValue(address, out pendingConnections))
            {
                pendingConnections = new List<PendingConnection>();
                s_pendingConnections.Add(address, pendingConnections);
            }

            pendingConnections.Add(new PendingConnection(socket, connectPipe, bindPipe));
        }

        private static void ConnectInprocSockets(Socket boundSocket, PendingConnection connection)
        {
            boundSocket.IncreaseSequenceNumber();
            connection.BindPipe.SlotId = boundSocket.SlotId;

            // If bound socket doesn't receive identity we need to read from the pipe as we already 
            // insert the message while creating the pipe pair
            if (!boundSocket.Options.ReceiveIdentity)
            {
                Frame frame;
                connection.BindPipe.TryRead(out frame);
                frame.Close();
            }

            int inHighWatermark = 0;
            int outHighWatermark = 0;

            if (boundSocket.Options.SendHighWatermark != 0 &&
                connection.Socket.Options.ReceiveHighwatermark != 0)            
                inHighWatermark = 
                    boundSocket.Options.SendHighWatermark + connection.Socket.Options.ReceiveHighwatermark;


            if (boundSocket.Options.ReceiveHighwatermark != 0 &&
                connection.Socket.Options.SendHighWatermark != 0)
                outHighWatermark =
                    boundSocket.Options.ReceiveHighwatermark + connection.Socket.Options.SendHighWatermark;
            
            // Set the highwatermarks now as before the bound highwater marks was missing
            connection.ConnectPipe.SetHighWatermarks(inHighWatermark,outHighWatermark);
            connection.BindPipe.SetHighWatermarks(outHighWatermark, inHighWatermark);

            BindCommand bindCommand = new BindCommand(boundSocket, connection.BindPipe);
            boundSocket.Process(bindCommand);

            CommandDispatcher.SendInprocConnected(connection.Socket);

            if (connection.Socket.Options.ReceiveIdentity)
            {
                Frame identity = new Frame();
                identity.Identity = true;
                connection.BindPipe.TryWrite(ref identity);
                connection.BindPipe.Flush();
            }
        }
    }
}
