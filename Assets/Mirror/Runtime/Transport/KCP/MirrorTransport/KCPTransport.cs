using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace Mirror.KCP
{
    public class KcpTransport : Transport
    {
        // common
        [Header("Transport Configuration")]
        public ushort Port = 7777;
        readonly byte[] buffer = new byte[1500];

        // server
        Socket serverSocket;
        EndPoint serverNewClientEP = new IPEndPoint(IPAddress.IPv6Any, 0);

        // client
        Socket clientSocket;
        EndPoint clientNewClientEP = new IPEndPoint(IPAddress.IPv6Any, 0);
        KcpClientConnection clientConnection;

        void Awake()
        {
            Debug.Log("KcpTransport initialized!");
        }

        // all except WebGL
        public override bool Available() =>
            Application.platform != RuntimePlatform.WebGLPlayer;

        // client
        public override bool ClientConnected() => clientSocket != null; // TODO
        public override void ClientConnect(string address)
        {
            if (clientConnection != null)
            {
                Debug.LogWarning("KCP: client already connected!");
                return;
            }

            clientConnection = new KcpClientConnection();
            clientConnection.Connect(address, Port);
        }
        public override bool ClientSend(int channelId, ArraySegment<byte> segment)
        {
            throw new NotImplementedException();
        }

        public override void ClientDisconnect()
        {
            clientSocket?.Close();
            clientSocket = null;
        }


        // IMPORTANT: set script execution order to >1000 to call Transport's
        //            LateUpdate after all others. Fixes race condition where
        //            e.g. in uSurvival Transport would apply Cmds before
        //            ShoulderRotation.LateUpdate, resulting in projectile
        //            spawns at the point before shoulder rotation.
        public void LateUpdate()
        {
            // note: we need to check enabled in case we set it to false
            // when LateUpdate already started.
            // (https://github.com/vis2k/Mirror/pull/379)
            if (!enabled)
                return;

            // update server
            while (serverSocket != null && serverSocket.Poll(0, SelectMode.SelectRead))
            {
                int msgLength = serverSocket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref serverNewClientEP);

                // is this a new connection?
                Debug.LogWarning($"KCP: server raw recv {msgLength}");
                /*if (!connectedClients.TryGetValue(endpoint, out KcpServerConnection connection))
                {
                    // add it to a queue
                    connection = new KcpServerConnection(socket, endpoint);
                    acceptedConnections.Writer.TryWrite(connection);
                    connectedClients.Add(endpoint, connection);
                    connection.Disconnected += () =>
                    {
                        connectedClients.Remove(endpoint);
                    };
                }

                connection.RawInput(data, msgLength);*/
            }

            // update client
            while (clientSocket != null && clientSocket.Poll(0, SelectMode.SelectRead))
            {
                int msgLength = clientSocket.ReceiveFrom(buffer, 0, buffer.Length, SocketFlags.None, ref clientNewClientEP);
                Debug.LogWarning($"KCP: client raw recv {msgLength}");

                // is this a new connection?
                /*if (!connectedClients.TryGetValue(endpoint, out KcpServerConnection connection))
                {
                    // add it to a queue
                    connection = new KcpServerConnection(socket, endpoint);
                    acceptedConnections.Writer.TryWrite(connection);
                    connectedClients.Add(endpoint, connection);
                    connection.Disconnected += () =>
                    {
                        connectedClients.Remove(endpoint);
                    };
                }

                connection.RawInput(data, msgLength);*/
            }
        }

        // server
        public override bool ServerActive() => serverSocket != null;
        public override void ServerStart()
        {
            // only start once
            if (serverSocket != null)
            {
                Debug.LogWarning("KCP: server already started!");
            }

            // listen
            serverSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
            serverSocket.DualMode = true;
            serverSocket.Bind(new IPEndPoint(IPAddress.IPv6Any, Port));

            // TODO accept
            //KcpServerConnection connection = await acceptedConnections.Reader.ReadAsync();
            //await connection.Handshake();
            //return connection;
        }
        public override bool ServerSend(int connectionId, int channelId, ArraySegment<byte> segment)
        {
            throw new NotImplementedException();
        }
        public override bool ServerDisconnect(int connectionId) => throw new NotImplementedException();
        public override string ServerGetClientAddress(int connectionId)
        {
            throw new NotImplementedException();
        }
        public override void ServerStop()
        {
            serverSocket?.Close();
            serverSocket = null;
        }

        // common
        public override void Shutdown()
        {
            throw new NotImplementedException();
        }

        // MTU
        public override ushort GetMaxPacketSize() => Kcp.MTU_DEF;

        public override string ToString()
        {
            return "KCP";
        }
    }
}
