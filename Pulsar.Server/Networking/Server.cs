using Pulsar.Common.Extensions;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using Pulsar.Server.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Networking
{
    public class Server
    {
        /// <summary>
        /// Occurs when the state of the server changes.
        /// </summary>
        public event ServerStateEventHandler ServerState;

        /// <summary>
        /// Represents a method that will handle a change in the server's state.
        /// </summary>
        /// <param name="s">The server which changed its state.</param>
        /// <param name="listening">The new listening state of the server.</param>
        /// <param name="port">The port the server is listening on, if listening is True.</param>
        public delegate void ServerStateEventHandler(Server s, bool listening, ushort port);

        /// <summary>
        /// Fires an event that informs subscribers that the server has changed it's state.
        /// </summary>
        /// <param name="listening">The new listening state of the server.</param>
        private void OnServerState(bool listening)
        {
            if (Listening == listening) return;

            Listening = listening;

            var handler = ServerState;
            handler?.Invoke(this, listening, Port);
        }

        /// <summary>
        /// Occurs when the state of a client changes.
        /// </summary>
        public event ClientStateEventHandler ClientState;

        /// <summary>
        /// Represents a method that will handle a change in a client's state.
        /// </summary>
        /// <param name="s">The server, the client is connected to.</param>
        /// <param name="c">The client which changed its state.</param>
        /// <param name="connected">The new connection state of the client.</param>
        public delegate void ClientStateEventHandler(Server s, Client c, bool connected);

        /// <summary>
        /// Fires an event that informs subscribers that a client has changed its state.
        /// </summary>
        /// <param name="c">The client which changed its state.</param>
        /// <param name="connected">The new connection state of the client.</param>
        private void OnClientState(Client c, bool connected)
        {
            if (!connected)
                RemoveClient(c);

            var handler = ClientState;
            handler?.Invoke(this, c, connected);
        }

        /// <summary>
        /// Occurs when a message is received by a client.
        /// </summary>
        public event ClientReadEventHandler ClientRead;

        /// <summary>
        /// Represents a method that will handle a message received from a client.
        /// </summary>
        /// <param name="s">The server, the client is connected to.</param>
        /// <param name="c">The client that has received the message.</param>
        /// <param name="message">The message that received by the client.</param>
        public delegate void ClientReadEventHandler(Server s, Client c, IMessage message);

        /// <summary>
        /// Fires an event that informs subscribers that a message has been
        /// received from the client.
        /// </summary>
        /// <param name="c">The client that has received the message.</param>
        /// <param name="message">The message that received by the client.</param>
        /// <param name="messageLength">The length of the message.</param>
        private void OnClientRead(Client c, IMessage message, int messageLength)
        {
            BytesReceived += messageLength;
            var handler = ClientRead;
            handler?.Invoke(this, c, message);
        }

        /// <summary>
        /// Occurs when a message is sent by a client.
        /// </summary>
        public event ClientWriteEventHandler ClientWrite;

        /// <summary>
        /// Represents the method that will handle the sent message by a client.
        /// </summary>
        /// <param name="s">The server, the client is connected to.</param>
        /// <param name="c">The client that has sent the message.</param>
        /// <param name="message">The message that has been sent by the client.</param>
        public delegate void ClientWriteEventHandler(Server s, Client c, IMessage message);

        /// <summary>
        /// Fires an event that informs subscribers that the client has sent a message.
        /// </summary>
        /// <param name="c">The client that has sent the message.</param>
        /// <param name="message">The message that has been sent by the client.</param>
        /// <param name="messageLength">The length of the message.</param>
        private void OnClientWrite(Client c, IMessage message, int messageLength)
        {
            BytesSent += messageLength;
            var handler = ClientWrite;
            handler?.Invoke(this, c, message);
        }

        /// <summary>
        /// The port on which the server is listening.
        /// For multi-port scenarios, this is the last port that was started.
        /// </summary>
        public ushort Port { get; private set; }

        /// <summary>
        /// The total amount of received bytes.
        /// </summary>
        public long BytesReceived { get; set; }

        /// <summary>
        /// The total amount of sent bytes.
        /// </summary>
        public long BytesSent { get; set; }

        /// <summary>
        /// The keep-alive time in ms.
        /// </summary>
        private const uint KeepAliveTime = 25000; // 25 s

        /// <summary>
        /// The keep-alive interval in ms.
        /// </summary>
        private const uint KeepAliveInterval = 25000; // 25 s        


        /// <summary>
        /// The listening state of the server. True if listening, else False.
        /// </summary>
        public bool Listening { get; private set; }

        /// <summary>
        /// Gets the clients currently connected to the server.
        /// </summary>
        protected Client[] Clients
        {
            get
            {
                lock (_clientsLock)
                {
                    return _clients.ToArray();
                }
            }
        }

        /// <summary>
        /// Gets the number of clients currently connected to the server without array allocation.
        /// </summary>
        public int ClientCount
        {
            get
            {
                lock (_clientsLock)
                {
                    return _clients.Count;
                }
            }
        }

        /// <summary>
        /// Handle(s) of the Server Socket(s).
        /// </summary>
        private readonly List<Socket> _handles = new List<Socket>();
        private readonly object _handlesLock = new object();

    /// <summary>
    /// Tracks the accept loop tasks for each listening socket.
    /// </summary>
    private readonly Dictionary<Socket, Task> _acceptLoops = new Dictionary<Socket, Task>();

    /// <summary>
    /// Cancellation token source controlling the active accept loops.
    /// </summary>
    private CancellationTokenSource _listenCancellation;

        /// <summary>
        /// The server certificate.
        /// </summary>
        protected readonly X509Certificate2 ServerCertificate;

        /// <summary>
        /// List of the clients connected to the server.
        /// </summary>
        private readonly List<Client> _clients = new List<Client>();

        /// <summary>
        /// The UPnP service used to create port mappings per port.
        /// </summary>
        private readonly Dictionary<ushort, UPnPService> _upnpByPort = new Dictionary<ushort, UPnPService>();

        /// <summary>
        /// Lock object for the list of clients.
        /// </summary>
        private readonly object _clientsLock = new object();

        /// <summary>
        /// Determines if the server is currently processing Disconnect method. 
        /// </summary>
        protected bool ProcessingDisconnect { get; set; }

        /// <summary>
        /// Constructor of the server.
        /// </summary>
        /// <param name="serverCertificate">The server certificate.</param>
        protected Server(X509Certificate2 serverCertificate)
        {
            ServerCertificate = serverCertificate;
        }

        /// <summary>
        /// Updates the status strip icon for the server listening state.
        /// </summary>
        /// <param name="isListening">True if server is listening, false otherwise.</param>
        private void UpdateServerStatusIcon(bool isListening)
        {
            var mainForm = GetMainFormSafe();
            if (mainForm == null) return;

            var iconResource = isListening
                ? Properties.Resources.bullet_green
                : Properties.Resources.bullet_red;

            try
            {
                if (mainForm.InvokeRequired)
                {
                    mainForm.BeginInvoke(new Action(() => SetStatusStripIcon(mainForm, iconResource)));
                }
                else
                {
                    SetStatusStripIcon(mainForm, iconResource);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Safely gets the main form instance if it exists and is valid.
        /// </summary>
        /// <returns>The main form instance or null if not available.</returns>
        private static FrmMain GetMainFormSafe()
        {
            var mainForm = Application.OpenForms.OfType<FrmMain>().FirstOrDefault();
            return (mainForm != null && !mainForm.IsDisposed && !mainForm.Disposing) ? mainForm : null;
        }

        /// <summary>
        /// Sets the status strip icon if the control is valid.
        /// </summary>
        /// <param name="mainForm">The main form instance.</param>
        /// <param name="icon">The icon to set.</param>
        private static void SetStatusStripIcon(FrmMain mainForm, System.Drawing.Image icon)
        {
            if (mainForm.statusStrip?.IsDisposed == false &&
                mainForm.statusStrip.Items.ContainsKey("listenToolStripStatusLabel"))
            {
                mainForm.statusStrip.Items["listenToolStripStatusLabel"].Image = icon;
            }
        }

        /// <summary>
        /// Begins listening for clients on a single port.
        /// </summary>
        public void Listen(ushort port, bool ipv6, bool enableUPnP)
        {
            ListenMany(new[] { port }, ipv6, enableUPnP);
        }

        /// <summary>
        /// Begins listening for clients on multiple ports.
        /// </summary>
        /// <param name="ports">Ports to listen on.</param>
        /// <param name="ipv6">If set to true, use a dual-stack socket to allow IPv4/6 connections. Otherwise use IPv4-only socket.</param>
        /// <param name="enableUPnP">Enables the automatic UPnP port forwarding for each port.</param>
        public void ListenMany(IEnumerable<ushort> ports, bool ipv6, bool enableUPnP)
        {
            var startNow = !Listening;
            var token = EnsureListeningToken();

            foreach (var port in ports.Distinct())
            {
                bool alreadyListening;
                lock (_handlesLock)
                {
                    alreadyListening = _handles.Any(h => (h.LocalEndPoint as IPEndPoint)?.Port == port);
                }

                if (alreadyListening)
                {
                    continue;
                }

                Socket handle;
                if (Socket.OSSupportsIPv6 && ipv6)
                {
                    handle = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                    handle.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, 0);
                    handle.Bind(new IPEndPoint(IPAddress.IPv6Any, port));
                }
                else
                {
                    handle = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    handle.Bind(new IPEndPoint(IPAddress.Any, port));
                }

                handle.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                handle.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                handle.Listen(1000);

                lock (_handlesLock)
                {
                    _handles.Add(handle);
                    _acceptLoops[handle] = AcceptClientsAsync(handle, token);
                }

                if (enableUPnP)
                {
                    var upnp = new UPnPService();
                    _upnpByPort[port] = upnp;
                    upnp.CreatePortMapAsync(port);
                }

                Port = port; // keep last started port for compatibility

                var mainForm = GetMainFormSafe();
                if (mainForm != null)
                {
                    try
                    {
                        if (mainForm.InvokeRequired)
                        {
                            mainForm.BeginInvoke(new Action(() =>
                            {
                                try
                                {
                                    mainForm.EventLog($"Started listening for connections on port: {port}", "info");
                                    UpdateServerStatusIcon(true);
                                }
                                catch (Exception)
                                {
                                }
                            }));
                        }
                        else
                        {
                            mainForm.EventLog($"Started listening for connections on port: {port}", "info");
                            UpdateServerStatusIcon(true);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            lock (_handlesLock)
            {
                if (startNow && _handles.Count > 0)
                {
                    OnServerState(true);
                }
            }
        }

        private CancellationToken EnsureListeningToken()
        {
            lock (_handlesLock)
            {
                if (_listenCancellation == null || _listenCancellation.IsCancellationRequested)
                {
                    _listenCancellation?.Dispose();
                    _listenCancellation = new CancellationTokenSource();
                }

                return _listenCancellation.Token;
            }
        }

        private async Task AcceptClientsAsync(Socket listenSocket, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    Socket clientSocket;
                    try
                    {
                        clientSocket = await listenSocket.AcceptAsync(cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (ObjectDisposedException)
                    {
                        break;
                    }
                    catch (SocketException ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Debug.WriteLine($"[SERVER] AcceptAsync failed: {ex.SocketErrorCode}");
                            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
                        }
                        continue;
                    }
                    catch (Exception ex)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            Debug.WriteLine($"[SERVER] AcceptAsync encountered an unexpected error: {ex.Message}");
                            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken).ConfigureAwait(false);
                        }
                        continue;
                    }

                    if (clientSocket == null)
                    {
                        continue;
                    }

                    try
                    {
                        clientSocket.SetKeepAliveEx(KeepAliveInterval, KeepAliveTime);
                        clientSocket.NoDelay = true;

                        NetworkStream networkStream = null;
                        try
                        {
                            networkStream = new NetworkStream(clientSocket, ownsSocket: true);
                            var client = new Client(networkStream, (IPEndPoint)clientSocket.RemoteEndPoint, ServerCertificate);
                            networkStream = null; // ownership transferred to Client
                            AddClient(client);
                            OnClientState(client, true);
                        }
                        finally
                        {
                            networkStream?.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[SERVER] Failed to initialize client connection: {ex.Message}");
                        try
                        {
                            clientSocket.Dispose();
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                lock (_handlesLock)
                {
                    _acceptLoops.Remove(listenSocket);
                }
            }
        }

        /// <summary>
        /// Adds a connected client to the list of clients,
        /// subscribes to the client's events.
        /// </summary>
        /// <param name="client">The client to add.</param>
        private void AddClient(Client client)
        {
            lock (_clientsLock)
            {
                client.ClientState += OnClientState;
                client.ClientRead += OnClientRead;

                _clients.Add(client);
            }
        }

        /// <summary>
        /// Removes a disconnected client from the list of clients,
        /// unsubscribes from the client's events.
        /// </summary>
        /// <param name="client">The client to remove.</param>
        private void RemoveClient(Client client)
        {
            if (ProcessingDisconnect) return;

            lock (_clientsLock)
            {
                client.ClientState -= OnClientState;
                client.ClientRead -= OnClientRead;

                _clients.Remove(client);
            }
        }

        /// <summary>
        /// Disconnect the server from all of the clients and discontinue
        /// listening (placing the server in an "off" state).
        /// </summary>
        public void Disconnect()
        {
            if (ProcessingDisconnect) return;
            ProcessingDisconnect = true;

            StopAcceptLoops();

            List<Socket> toClose;
            lock (_handlesLock)
            {
                toClose = _handles.ToList();
                _handles.Clear();
            }
            foreach (var handle in toClose)
            {
                try { handle.Close(); } catch { }
            }

            foreach (var upnpKvp in _upnpByPort.ToList())
            {
                try { upnpKvp.Value.DeletePortMapAsync(upnpKvp.Key); } catch { }
            }
            _upnpByPort.Clear();

            lock (_clientsLock)
            {
                var clientsToDisconnect = _clients.ToList();
                _clients.Clear();

                foreach (var client in clientsToDisconnect)
                {
                    try
                    {
                        client.Disconnect();
                        client.ClientState -= OnClientState;
                        client.ClientRead -= OnClientRead;
                    }
                    catch
                    {
                    }
                }
            }

            ProcessingDisconnect = false;
            OnServerState(false);
            UpdateServerStatusIcon(false);
        }

        private void StopAcceptLoops()
        {
            CancellationTokenSource cancellation = null;
            Task[] acceptTasks = Array.Empty<Task>();

            lock (_handlesLock)
            {
                if (_listenCancellation != null)
                {
                    cancellation = _listenCancellation;
                    _listenCancellation = null;
                }

                if (_acceptLoops.Count > 0)
                {
                    acceptTasks = _acceptLoops.Values.Where(t => t != null).ToArray();
                    _acceptLoops.Clear();
                }
            }

            if (cancellation != null)
            {
                try
                {
                    cancellation.Cancel();
                }
                catch
                {
                }
                finally
                {
                    cancellation.Dispose();
                }
            }

            if (acceptTasks.Length > 0)
            {
                try
                {
                    Task.WaitAll(acceptTasks, TimeSpan.FromSeconds(2));
                }
                catch (AggregateException)
                {
                }
                catch (OperationCanceledException)
                {
                }
            }
        }

        /// <summary>
        /// Gets the ports the server is currently listening on.
        /// </summary>
        public ushort[] GetListeningPorts()
        {
            lock (_handlesLock)
            {
                return _handles.Select(h => (ushort)((IPEndPoint)h.LocalEndPoint).Port).ToArray();
            }
        }
    }
}

