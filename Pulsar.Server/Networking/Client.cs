using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using Pulsar.Server.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Threading;

namespace Pulsar.Server.Networking
{
    public class Client : IEquatable<Client>, ISender
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
        /// Occurs when the state of the client changes.
        /// </summary>
        public event ClientStateEventHandler ClientState;

        /// <summary>
        /// Represents the method that will handle a change in a client's state.
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

            if (connected)
            {

            }
        }

        /// <summary>
        /// Occurs when a message is received from the client.
        /// </summary>
        public event ClientReadEventHandler ClientRead;

        /// <summary>
        /// Represents the method that will handle a message received from the client.
        /// </summary>
        /// <param name="s">The client that has received the message.</param>
        /// <param name="message">The message that received by the client.</param>
        /// <param name="messageLength">The length of the message.</param>
        public delegate void ClientReadEventHandler(Client s, IMessage message, int messageLength);

        /// <summary>
        /// Fires an event that informs subscribers that a message has been
        /// received from the client.
        /// </summary>
        /// <param name="message">The message that received by the client.</param>
        /// <param name="messageLength">The length of the message.</param>
        private void OnClientRead(IMessage message, int messageLength)
        {
            Debug.WriteLine($"[SERVER] Received packet: {message.GetType().Name} (Length: {messageLength} bytes) from {EndPoint}");
            var handler = ClientRead;
            handler?.Invoke(this, message, messageLength);
        }

        public static bool operator ==(Client c1, Client c2)
        {
            if (ReferenceEquals(c1, null))
                return ReferenceEquals(c2, null);

            return c1.Equals(c2);
        }

        public static bool operator !=(Client c1, Client c2)
        {
            return !(c1 == c2);
        }

        /// <summary>
        /// Checks whether the clients are equal.
        /// </summary>
        /// <param name="other">Client to compare with.</param>
        /// <returns>True if equal, else False.</returns>
        public bool Equals(Client other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            try
            {
                // the port is always unique for each client
                return this.EndPoint.Port.Equals(other.EndPoint.Port);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as Client);
        }

        /// <summary>
        /// Returns the hashcode for this instance.
        /// </summary>
        /// <returns>A hash code for the current instance.</returns>
        public override int GetHashCode()
        {
            return this.EndPoint.GetHashCode();
        }

        /// <summary>
        /// The stream used for communication.
        /// </summary>
        private SslStream _stream;

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
        /// The time when the client connected.
        /// </summary>
        public DateTime ConnectedTime { get; }

        /// <summary>
        /// The connection state of the client.
        /// </summary>
        public bool Connected { get; private set; }

        /// <summary>
        /// Determines if the client is identified.
        /// </summary>
        public bool Identified { get; set; }

        /// <summary>
        /// Stores values of the user.
        /// </summary>
        public UserState Value { get; set; }

        /// <summary>
        /// The Endpoint which the client is connected to.
        /// </summary>
        public IPEndPoint EndPoint { get; }

        /// <summary>
        /// The header size in bytes.
        /// </summary>
        private const int HEADER_SIZE = 4;  // 4 B

        public Client(SslStream stream, IPEndPoint endPoint)
        {
            try
            {
                Identified = false;
                Value = new UserState();
                EndPoint = endPoint;
                ConnectedTime = DateTime.UtcNow;
                _stream = stream;

                _readBuffer = new byte[HEADER_SIZE];
                _readOffset = 0;
                _readLength = HEADER_SIZE;
                _stream.BeginRead(_readBuffer, _readOffset, _readBuffer.Length, AsyncReceive, null);
                OnClientState(true);
            }
            catch (Exception ex)
            {
                Disconnect();
                OnClientFail(ex);
            }
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
        /// Sends a message to the connected client.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to be sent.</param>
        public void Send<T>(T message) where T : IMessage
        {
            if (!Connected || message == null) return;

            _sendBuffers.Enqueue(message);
            if (Interlocked.Exchange(ref _sendingMessagesFlag, 1) == 0)
            {
                ThreadPool.QueueUserWorkItem(ProcessSendBuffers);
            }
        }

        /// <summary>
        /// Sends a message to the connected client.
        /// Blocks the thread until the message has been sent.
        /// </summary>
        /// <typeparam name="T">The type of the message.</typeparam>
        /// <param name="message">The message to be sent.</param>
        public void SendBlocking<T>(T message) where T : IMessage
        {
            if (!Connected || message == null) return;

            SafeSendMessage(message);
        }

        /// <summary>
        /// Safely sends a message and prevents multiple simultaneous
        /// write operations on the <see cref="_stream"/>.
        /// </summary>
        /// <param name="message">The message to send.</param>
        private void SafeSendMessage(IMessage message)
        {
            try
            {
                lock (_sendMessageLock)
                {
                    var payload = PulsarMessagePackSerializer.Serialize(message);
                    _stream.Write(BitConverter.GetBytes(payload.Length), 0, HEADER_SIZE);
                    _stream.Write(payload, 0, payload.Length);
                    _stream.Flush();
                }
            }
            catch
            {
                Disconnect();
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
        /// Disconnect the client from the server and dispose of
        /// resources associated with the client.
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

            SendCleanup(true);
            OnClientState(false);
        }
    }
}
