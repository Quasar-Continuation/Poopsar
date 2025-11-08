using System;
using System.Windows.Forms;

namespace Pulsar.Client.Helper.HVNC
{
    internal partial class CloneProgressForm : Form
    {
        private const int CloseDelayMilliseconds = 450;
        private bool _isCompleting;
        private bool _cancelRaised;

        public CloneProgressForm()
        {
            InitializeComponent();
        }

        public event EventHandler UserRequestedCancel;

        public void Initialize(string browserName)
        {
            lblTitle.Text = string.IsNullOrWhiteSpace(browserName)
                ? "Cloning browser profile..."
                : $"Cloning {browserName} profile...";
            lblDetail.Text = "Preparing...";
            progressBar.Style = ProgressBarStyle.Marquee;
        }

        public void ShowPreparing()
        {
            progressBar.Style = ProgressBarStyle.Marquee;
            lblDetail.Text = "Preparing...";
        }

        public void UpdateProgress(BrowserCloneProgress progress)
        {
            if (IsDisposed)
            {
                return;
            }

            if (progress.IsIndeterminate || progress.TotalFiles <= 0)
            {
                progressBar.Style = ProgressBarStyle.Marquee;
                lblDetail.Text = "Preparing...";
                return;
            }

            if (progressBar.Style != ProgressBarStyle.Continuous)
            {
                progressBar.Style = ProgressBarStyle.Continuous;
            }

            int maximum = Math.Max(1, progress.TotalFiles);
            if (progressBar.Maximum != maximum)
            {
                progressBar.Maximum = maximum;
            }

            int value = Math.Min(progress.FilesCopied, progressBar.Maximum);
            progressBar.Value = Math.Max(0, value);

            string currentFile = progress.CurrentItem;
            if (!string.IsNullOrEmpty(currentFile) && currentFile.Length > 50)
            {
                currentFile = "..." + currentFile.Substring(currentFile.Length - 50);
            }

            lblDetail.Text = string.IsNullOrEmpty(currentFile)
                ? $"Cloned {progress.FilesCopied} of {progress.TotalFiles} files"
                : $"{progress.FilesCopied}/{progress.TotalFiles}: {currentFile}";
        }

        public void BeginCompleteAnimation(bool wasSuccessful, Action onClosed)
        {
            _isCompleting = true;
            btnCancel.Enabled = false;

            if (IsDisposed)
            {
                onClosed?.Invoke();
                return;
            }

            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Maximum = 100;
            progressBar.Value = 100;

            lblDetail.Text = wasSuccessful ? "Profile cloned successfully" : "Profile clone failed";

            var closeTimer = new Timer
            {
                Interval = CloseDelayMilliseconds
            };

            closeTimer.Tick += (sender, args) =>
            {
                closeTimer.Stop();
                closeTimer.Dispose();
                onClosed?.Invoke();
                if (!IsDisposed)
                {
                    Close();
                }
            };

            closeTimer.Start();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            RaiseCancelRequested();
            if (!IsDisposed)
            {
                Close();
            }
        }

        private void CloneProgressForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isCompleting)
            {
                return;
            }

            if (e.CloseReason == CloseReason.UserClosing || e.CloseReason == CloseReason.TaskManagerClosing)
            {
                RaiseCancelRequested();
            }
        }

        private void RaiseCancelRequested()
        {
            if (_cancelRaised)
            {
                return;
            }

            _cancelRaised = true;
            UserRequestedCancel?.Invoke(this, EventArgs.Empty);
        }
    }
}
