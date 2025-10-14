using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Pulsar.Server.Plugins;

namespace Pulsar.Server.Forms
{
    public partial class FrmPluginManager : Form
    {
        private readonly PluginManager _pluginManager;
        private readonly string _pluginsDirectory;

        public FrmPluginManager(PluginManager pluginManager)
        {
            _pluginManager = pluginManager;
            _pluginsDirectory = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "Plugins");
            InitializeComponent();
            LoadPluginsToList();
            _pluginManager.PluginsChanged += PluginManager_PluginsChanged;
        }

        private void InitializeComponent()
        {
            this.Text = "Plugin Manager";
            this.Size = new System.Drawing.Size(600, 400);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var listView = new ListView
            {
                Name = "lstPlugins",
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            listView.Columns.Add("Name", 150);
            listView.Columns.Add("Version", 80);
            listView.Columns.Add("Type", 100);
            listView.Columns.Add("Description", 200);
            listView.Columns.Add("Status", 80);

            var btnReload = new Button
            {
                Text = "Reload Plugins",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            btnReload.Click += BtnReload_Click;

            var btnOpenFolder = new Button
            {
                Text = "Open Plugins Folder",
                Dock = DockStyle.Bottom,
                Height = 30
            };
            btnOpenFolder.Click += BtnOpenFolder_Click;

            this.Controls.Add(listView);
            this.Controls.Add(btnReload);
            this.Controls.Add(btnOpenFolder);
        }

        private void PluginManager_PluginsChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(LoadPluginsToList));
            }
            else
            {
                LoadPluginsToList();
            }
        }

        private void LoadPluginsToList()
        {
            var listView = this.Controls.OfType<ListView>().FirstOrDefault(lv => lv.Name == "lstPlugins");
            if (listView == null) return;

            listView.Items.Clear();
            foreach (var plugin in _pluginManager.Plugins)
            {
                var item = new ListViewItem(plugin.Name);
                item.SubItems.Add(plugin.Version.ToString());
                item.SubItems.Add(plugin.Type);
                item.SubItems.Add(plugin.Description);
                item.SubItems.Add("Loaded");
                listView.Items.Add(item);
            }

            // Also list disabled plugins
            if (Directory.Exists(_pluginsDirectory))
            {
                var loadedPluginNames = _pluginManager.Plugins.Select(p => p.Name).ToHashSet();
                foreach (var file in Directory.GetFiles(_pluginsDirectory, "*.dll.disabled"))
                {
                    var pluginName = Path.GetFileNameWithoutExtension(file).Replace(".dll", "");
                    if (!loadedPluginNames.Contains(pluginName))
                    {
                        var item = new ListViewItem(pluginName);
                        item.SubItems.Add("N/A");
                        item.SubItems.Add("N/A");
                        item.SubItems.Add("Disabled");
                        item.SubItems.Add("Disabled");
                        listView.Items.Add(item);
                    }
                }
            }
        }

        private void BtnReload_Click(object sender, EventArgs e)
        {
            _pluginManager.LoadFrom(_pluginsDirectory);
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            try
            {
                if (Directory.Exists(_pluginsDirectory))
                {
                    System.Diagnostics.Process.Start("explorer.exe", _pluginsDirectory);
                }
                else
                {
                    MessageBox.Show("Plugins directory not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening folder: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _pluginManager.PluginsChanged -= PluginManager_PluginsChanged;
            base.OnFormClosed(e);
        }
    }
}