using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Server.Extensions;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using Pulsar.Server.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmSystemInformation : Form
    {
        // -------------------------------------------------------------
        // FIXED: StableListView now inherits AeroListView
        // -------------------------------------------------------------
        private class StableListView : AeroListView
        {
            public StableListView()
            {
                this.DoubleBuffered = true;
                this.ResizeRedraw = false;

                this.SetStyle(ControlStyles.OptimizedDoubleBuffer |
                              ControlStyles.AllPaintingInWmPaint, true);

                this.UpdateStyles();
            }

            protected override void OnResize(EventArgs e)
            {
                this.BeginUpdate();
                base.OnResize(e);
                this.EndUpdate();
            }

            protected override void OnColumnWidthChanged(ColumnWidthChangedEventArgs e)
            {
                this.BeginUpdate();
                base.OnColumnWidthChanged(e);
                this.EndUpdate();
            }
        }

        // -------------------------------------------------------------
        // Fields
        // -------------------------------------------------------------
        private readonly Client _connectClient;
        private readonly SystemInformationHandler _sysInfoHandler;
        private static readonly Dictionary<Client, FrmSystemInformation> OpenedForms = new();

        public static FrmSystemInformation CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
                return OpenedForms[client];

            var f = new FrmSystemInformation(client);
            f.Disposed += (s, a) => OpenedForms.Remove(client);
            OpenedForms[client] = f;

            return f;
        }

        public FrmSystemInformation(Client client)
        {
            _connectClient = client;
            _sysInfoHandler = new SystemInformationHandler(client);

            RegisterMessageHandler();
            InitializeComponent();

            ConvertAllListViewsToStable();

            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
        }

        // -------------------------------------------------------------
        // Replace ALL AeroListView controls with StableListView
        // -------------------------------------------------------------
        private void ConvertAllListViewsToStable()
        {
            foreach (Control ctrl in GetAllControls(this))
            {
                if (ctrl is AeroListView oldLv)
                {
                    var stable = new StableListView
                    {
                        Name = oldLv.Name,
                        Location = oldLv.Location,
                        Size = oldLv.Size,
                        Anchor = oldLv.Anchor,
                        Dock = oldLv.Dock,
                        FullRowSelect = oldLv.FullRowSelect,
                        GridLines = oldLv.GridLines,
                        HeaderStyle = oldLv.HeaderStyle,
                        HideSelection = oldLv.HideSelection,
                        MultiSelect = oldLv.MultiSelect,
                        View = oldLv.View,
                        BorderStyle = oldLv.BorderStyle
                    };

                    foreach (ColumnHeader col in oldLv.Columns)
                        stable.Columns.Add((ColumnHeader)col.Clone());

                    var parent = oldLv.Parent;
                    int index = parent.Controls.IndexOf(oldLv);

                    parent.Controls.Remove(oldLv);
                    parent.Controls.Add(stable);
                    parent.Controls.SetChildIndex(stable, index);

                    if (oldLv.Name == "lstSystem")
                        lstSystem = stable;
                }
            }
        }

        private List<Control> GetAllControls(Control parent)
        {
            List<Control> list = new();
            foreach (Control child in parent.Controls)
            {
                list.Add(child);
                list.AddRange(GetAllControls(child));
            }
            return list;
        }

        // -------------------------------------------------------------
        // Handlers
        // -------------------------------------------------------------
        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _sysInfoHandler.ProgressChanged += SystemInformationChanged;
            MessageHandler.Register(_sysInfoHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_sysInfoHandler);
            _sysInfoHandler.ProgressChanged -= SystemInformationChanged;
            _connectClient.ClientState -= ClientDisconnected;
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
                this.Invoke((MethodInvoker)this.Close);
        }

        private void FrmSystemInformation_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("System Information", _connectClient);

            _sysInfoHandler.RefreshSystemInformation();
            AddBasicSystemInformation();
        }

        private void FrmSystemInformation_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
        }

        private void SystemInformationChanged(object sender, List<Tuple<string, string>> infos)
        {
            lstSystem.Items.RemoveAt(2);

            foreach (var info in infos)
            {
                lstSystem.Items.Add(new ListViewItem(new[] { info.Item1, info.Item2 }));
            }

            lstSystem.BeginUpdate();
            lstSystem.AutosizeColumns();
            lstSystem.EndUpdate();
        }

        private void AddBasicSystemInformation()
        {
            lstSystem.Items.Add(new ListViewItem(new[]
            {
                "Operating System",
                _connectClient.Value.OperatingSystem
            }));

            lstSystem.Items.Add(new ListViewItem(new[]
            {
                "Architecture",
                _connectClient.Value.OperatingSystem.Contains("32 Bit")
                    ? "x86 (32 Bit)"
                    : "x64 (64 Bit)"
            }));

            lstSystem.Items.Add(new ListViewItem(new[]
            {
                "",
                "Getting more information..."
            }));
        }

        private void copyAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstSystem.Items.Count == 0) return;

            string output = string.Join("\r\n",
                lstSystem.Items.Cast<ListViewItem>()
                               .Select(item =>
                                   string.Join(" : ",
                                       item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                                                    .Select(s => s.Text))));

            ClipboardHelper.SetClipboardTextSafe(output);
        }

        private void copySelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstSystem.SelectedItems.Count == 0) return;

            string output = string.Join("\r\n",
                lstSystem.SelectedItems.Cast<ListViewItem>()
                                       .Select(item =>
                                           string.Join(" : ",
                                               item.SubItems.Cast<ListViewItem.ListViewSubItem>()
                                                            .Select(s => s.Text))));

            ClipboardHelper.SetClipboardTextSafe(output);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lstSystem.Items.Clear();
            _sysInfoHandler.RefreshSystemInformation();
            AddBasicSystemInformation();
        }
    }
}
