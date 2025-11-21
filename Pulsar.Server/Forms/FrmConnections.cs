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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmConnections : Form
    {
        private readonly object _updateGate = new();
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

        // ------------------------ RedrawScope Fix ------------------------
        internal readonly struct RedrawScope : IDisposable
        {
            private readonly Control _ctl;
            private readonly IntPtr _handle;

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

            private const int WM_SETREDRAW = 0x0B;

            public RedrawScope(Control c)
            {
                _ctl = c;
                _handle = c.IsHandleCreated ? c.Handle : IntPtr.Zero;

                if (_handle != IntPtr.Zero)
                    SendMessage(_handle, WM_SETREDRAW, 0, 0); // STOP drawing
            }

            public void Dispose()
            {
                if (_handle != IntPtr.Zero)
                {
                    SendMessage(_handle, WM_SETREDRAW, 1, 0); // RESUME drawing
                    _ctl.Invalidate();
                }
            }
        }
        // -----------------------------------------------------------------

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

            if (client.EndPoint is IPEndPoint ep)
            {
                _clientAddress = ep.Address.ToString();
                _clientPort = ep.Port;
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

            EnhanceListViewStability(lstConnections);

            _refreshTimer = new Timer { Interval = 3000 };
            _refreshTimer.Tick += RefreshTick;
            _refreshTimer.Start();

            autorefreshToolStripMenuItem.Checked = _autoRefreshEnabled;
        }

        private void EnhanceListViewStability(ListView lv)
        {
            try
            {
                var prop = lv.GetType().GetProperty("DoubleBuffered",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                prop?.SetValue(lv, true, null);
            }
            catch { }

            lv.Resize += (s, e) =>
            {
                try { lv.BeginUpdate(); lv.EndUpdate(); } catch { }
            };
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
                this.Invoke((System.Windows.Forms.MethodInvoker)this.Close);
        }

        private void RefreshTick(object sender, EventArgs e)
        {
            if (!_autoRefreshEnabled || _isRefreshing)
                return;

            _isRefreshing = true;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Yield(); // prevent synchronous deadlock
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

            if (lstConnections.IsDisposed)
                return;

            int? topIndexBefore = null;
            try
            {
                if (lstConnections.TopItem != null)
                    topIndexBefore = lstConnections.TopItem.Index;
            }
            catch { }

            lock (lstConnections)
            {
                // 🔥 FIX: Prevents header-drop glitch + blank space bug
                using (new RedrawScope(lstConnections))
                {
                    lstConnections.BeginUpdate();
                    try
                    {
                        UpdateListView(connections, topIndexBefore);
                    }
                    finally
                    {
                        lstConnections.EndUpdate();
                    }
                }
            }
        }

        private void UpdateListView(TcpConnection[] connections, int? topIndexBefore)
        {
            var items = lstConnections.Items.Cast<ListViewItem>().ToList();
            var existing = items.ToDictionary(GetKey, x => x);
            var seenNow = new HashSet<string>();

            if (_initialLoad)
            {
                foreach (var c in connections)
                    _initialKeys.Add(GetKey(c));
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

            foreach (var item in items.Where(i => !seenNow.Contains(GetKey(i))).ToList())
                RemoveListViewItemDelayed(item);

            // restore scroll position
            if (topIndexBefore.HasValue && lstConnections.Items.Count > 0)
            {
                int idx = Math.Max(0, Math.Min(topIndexBefore.Value, lstConnections.Items.Count - 1));
                try { lstConnections.TopItem = lstConnections.Items[idx]; } catch { }
            }
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

            Task.Delay(800).ContinueWith(t =>
            {
                if (lstConnections.IsDisposed || item.ListView == null)
                    return;

                try
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (!lstConnections.IsDisposed && item.ListView != null)
                        {
                            lstConnections.Items.Remove(item);
                        }
                    }));
                }
                catch { }
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
