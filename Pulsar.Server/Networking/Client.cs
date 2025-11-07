using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using Pulsar.Server.Forms;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

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
    private Stream _stream;
    private readonly X509Certificate2 _serverCertificate;
    private readonly bool _encryptTraffic;

    /// <summary>
    /// The queue which holds messages to send.
    /// </summary>
    private readonly ConcurrentQueue<IMessage> _sendBuffers = new ConcurrentQueue<IMessage>();

    /// <summary>
    /// Coordinates cancellation across asynchronous read/write operations.
    /// </summary>
    private readonly CancellationTokenSource _cancellationSource = new CancellationTokenSource();

    /// <summary>
    /// Ensures only one writer accesses the stream at a time.
    /// </summary>
    private readonly SemaphoreSlim _sendSemaphore = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Background receive loop tracking task.
    /// </summary>
    private Task _receiveTask;

    /// <summary>
    /// Determines if the client is currently sending messages.
    /// </summary>
    private int _sendingMessagesFlag;

    /// <summary>
    /// Prevents multiple disconnect executions.
    /// </summary>
    private int _disconnectInvoked;

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

        public Client(Stream stream, IPEndPoint endPoint, X509Certificate2 serverCertificate)
        {
            try
            {
                Identified = false;
                Value = new UserState();
                EndPoint = endPoint;
                ConnectedTime = DateTime.UtcNow;
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
                _serverCertificate = serverCertificate ?? throw new ArgumentNullException(nameof(serverCertificate));

#if DEBUG
                var certificateUsable = SecureMessageEnvelopeHelper.CanUse(_serverCertificate);
                var enforceEncryptionFlag = Environment.GetEnvironmentVariable("PULSAR_DEBUG_ENFORCE_ENCRYPTION");
                var enforceEncryption = !string.IsNullOrWhiteSpace(enforceEncryptionFlag)
                    && (enforceEncryptionFlag.Equals("1", StringComparison.OrdinalIgnoreCase)
                        || enforceEncryptionFlag.Equals("true", StringComparison.OrdinalIgnoreCase)
                        || enforceEncryptionFlag.Equals("yes", StringComparison.OrdinalIgnoreCase));

                if (enforceEncryption && certificateUsable)
                {
                    _encryptTraffic = true;
                    Debug.WriteLine("[SERVER] Debug build: encryption enforced via PULSAR_DEBUG_ENFORCE_ENCRYPTION.");
                }
                else
                {
                    _encryptTraffic = false;
                    if (!certificateUsable)
                    {
                        Debug.WriteLine("[SERVER] Debug build: server certificate unavailable, running without encryption.");
                    }
                    else
                    {
                        var logMessage = enforceEncryption
                            ? "[SERVER] Debug build: encryption enforcement requested but certificate cannot be used; continuing without encryption."
                            : "[SERVER] Debug build: encryption disabled by default for development.";
                        Debug.WriteLine(logMessage);
                    }
                }
#else
                _encryptTraffic = SecureMessageEnvelopeHelper.CanUse(_serverCertificate);
                if (!_encryptTraffic)
                {
                    throw new InvalidOperationException("A valid server certificate is required for secure communication.");
                }
#endif

                _receiveTask = ReceiveLoopAsync(_cancellationSource.Token);
                OnClientState(true);
            }
            catch (Exception ex)
            {
                Disconnect();
                OnClientFail(ex);
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
        {
            var headerBuffer = new byte[HEADER_SIZE];

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await ReadExactAsync(headerBuffer, cancellationToken).ConfigureAwait(false);
                    var length = BitConverter.ToInt32(headerBuffer, 0);
                    if (length <= 0)
                    {
                        throw new InvalidDataException("Invalid message length.");
                    }

                    var payload = new byte[length];
                    await ReadExactAsync(payload, cancellationToken).ConfigureAwait(false);

                    var message = PulsarMessagePackSerializer.Deserialize(payload);
                    message = ProcessIncomingMessage(message);
                    OnClientRead(message, length);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Disconnect();
                OnClientFail(ex);
            }
        }

        private async Task ReadExactAsync(byte[] buffer, CancellationToken cancellationToken)
        {
            var remaining = buffer.Length;
            var offset = 0;

            while (remaining > 0)
            {
                var stream = _stream;
                if (stream == null)
                {
                    throw new IOException("Network stream is unavailable.");
                }

                var bytesRead = await stream.ReadAsync(buffer.AsMemory(offset, remaining), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    throw new IOException("Remote endpoint closed the connection.");
                }

                offset += bytesRead;
                remaining -= bytesRead;
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
            if (Interlocked.CompareExchange(ref _sendingMessagesFlag, 1, 0) == 0)
            {
                _ = Task.Run(ProcessSendBuffersAsync);
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

            SafeSendMessageAsync(message).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Safely sends a message and prevents multiple simultaneous
        /// write operations on the <see cref="_stream"/>.
        /// </summary>
        /// <param name="message">The message to send.</param>
        private async Task SafeSendMessageAsync(IMessage message)
        {
            if (message == null)
            {
                return;
            }

            var acquired = false;
            try
            {
                await _sendSemaphore.WaitAsync(_cancellationSource.Token).ConfigureAwait(false);
                acquired = true;

                var stream = _stream;
                if (stream == null)
                {
                    return;
                }

                var prepared = PrepareMessageForSend(message);
                if (prepared == null)
                {
                    return;
                }

                var payload = PulsarMessagePackSerializer.Serialize(prepared);
                var totalLength = HEADER_SIZE + payload.Length;
                var buffer = ArrayPool<byte>.Shared.Rent(totalLength);
                try
                {
                    var span = buffer.AsSpan(0, totalLength);
                    BinaryPrimitives.WriteInt32LittleEndian(span.Slice(0, HEADER_SIZE), payload.Length);
                    payload.AsSpan().CopyTo(span.Slice(HEADER_SIZE));
                    await stream.WriteAsync(buffer.AsMemory(0, totalLength), _cancellationSource.Token).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer, clearArray: false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                Disconnect();
            }
            finally
            {
                if (acquired)
                {
                    _sendSemaphore.Release();
                }
            }
        }

        private async Task ProcessSendBuffersAsync()
        {
            try
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

                    await SafeSendMessageAsync(message).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                SendCleanup(true);
            }
            catch
            {
                SendCleanup(true);
                Disconnect();
            }
        }

        private void SendCleanup(bool clear = false)
        {
            Interlocked.Exchange(ref _sendingMessagesFlag, 0);

            if (clear)
            {
                while (_sendBuffers.TryDequeue(out _)) { }
                return;
            }

            if (!_sendBuffers.IsEmpty && Interlocked.CompareExchange(ref _sendingMessagesFlag, 1, 0) == 0)
            {
                _ = Task.Run(ProcessSendBuffersAsync);
            }
        }

        private IMessage PrepareMessageForSend(IMessage message)
        {
            if (message == null)
            {
                return null;
            }

            if (message is SecureMessageEnvelope)
            {
                return message;
            }

            if (!_encryptTraffic)
            {
                return message;
            }

            if (!SecureMessageEnvelopeHelper.CanUse(_serverCertificate))
            {
                throw new InvalidOperationException("Secure transport is enabled but the server certificate is unavailable.");
            }

            return SecureMessageEnvelopeHelper.Wrap(message, _serverCertificate);
        }

        private IMessage ProcessIncomingMessage(IMessage message)
        {
            if (message is SecureMessageEnvelope secureEnvelope)
            {
                if (!SecureMessageEnvelopeHelper.CanUse(_serverCertificate))
                {
                    throw new InvalidOperationException("Received a secure envelope but the server certificate is unavailable for decryption.");
                }

                return SecureMessageEnvelopeHelper.Unwrap(secureEnvelope, _serverCertificate);
            }

            if (_encryptTraffic)
            {
                throw new InvalidOperationException($"Received unexpected plaintext message of type {message?.GetType().Name} while encryption is enforced.");
            }

            return message;
        }

        /// <summary>
        /// Disconnect the client from the server and dispose of
        /// resources associated with the client.
        /// </summary>
        public void Disconnect()
        {
            if (Interlocked.Exchange(ref _disconnectInvoked, 1) == 1)
            {
                return;
            }

            try
            {
                _cancellationSource.Cancel();
            }
            catch
            {
            }

            var stream = Interlocked.Exchange(ref _stream, null);
            if (stream != null)
            {
                try
                {
                    stream.Dispose();
                }
                catch
                {
                }
            }

            SendCleanup(true);
            OnClientState(false);
        }
    }
}
