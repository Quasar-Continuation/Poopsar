using Pulsar.Server.Utilities;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WFMouseButtons = System.Windows.Forms.MouseButtons;
using WFMouseEventArgs = System.Windows.Forms.MouseEventArgs;

namespace Pulsar.Server.Controls
{
    /// <summary>
    /// Hosts a WPF image surface backed by a WriteableBitmap to render rapid remote desktop frames inside WinForms.
    /// </summary>
    public sealed class RemoteDesktopElementHost : ElementHost, IRapidPictureBox
    {
        private readonly System.Windows.Controls.Image _imageControl;
        private readonly object _bitmapLock = new object();

        private WriteableBitmap _writeableBitmap;
        private FrameCounter _frameCounter = new FrameCounter();
        private Stopwatch _stopwatch;
        private bool _inputHandlersAttached;

        /// <summary>
        /// Provides access to the current WriteableBitmap for diagnostic purposes.
        /// </summary>
        public WriteableBitmap CurrentBitmap => _writeableBitmap;

        /// <inheritdoc />
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool Running { get; set; }

        /// <inheritdoc />
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public System.Drawing.Image GetImageSafe { get; set; }

        /// <summary>
        /// Returns the width of the most recent frame.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int ScreenWidth { get; private set; }

        /// <summary>
        /// Returns the height of the most recent frame.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public int ScreenHeight { get; private set; }

        /// <summary>
        /// True when at least one frame has been rendered.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [Browsable(false)]
        public bool HasFrame => _writeableBitmap != null;

        public RemoteDesktopElementHost()
        {
            try
            {
                BackColor = System.Drawing.Color.Black;

                _imageControl = new System.Windows.Controls.Image
                {
                    Stretch = Stretch.Fill,
                    SnapsToDevicePixels = true,
                    Focusable = true,
                    IsHitTestVisible = true
                };

                RenderOptions.SetBitmapScalingMode(_imageControl, BitmapScalingMode.HighQuality);
                RenderOptions.SetEdgeMode(_imageControl, EdgeMode.Unspecified);

                Child = _imageControl;

                TabStop = true;
                AttachInputForwarders();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RemoteDesktopElementHost ctor failed: {ex}");
                System.Windows.Forms.MessageBox.Show(
                    $"RemoteDesktopElementHost failed to initialize.\n\n{ex}",
                    "Remote Desktop Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                throw;
            }
        }

        /// <inheritdoc />
        public void Start()
        {
            _frameCounter = new FrameCounter();
            _stopwatch = Stopwatch.StartNew();
            Running = true;
        }

        /// <inheritdoc />
        public void Stop()
        {
            _stopwatch?.Stop();
            Running = false;
        }

        /// <inheritdoc />
        public void UpdateImage(Bitmap bitmap, bool cloneBitmap = false)
        {
            if (bitmap == null)
            {
                return;
            }

            if (!_imageControl.Dispatcher.CheckAccess())
            {
                Bitmap transfer = cloneBitmap ? bitmap : (Bitmap)bitmap.Clone();
                _imageControl.Dispatcher.BeginInvoke(new Action(() => UpdateImage(transfer, true)));

                if (!cloneBitmap)
                {
                    bitmap.Dispose();
                }
                return;
            }

            try
            {
                CountFps();

                var source = PrepareBitmapForTransfer(bitmap, cloneBitmap, out bool createdCopy);

                try
                {
                    EnsureWriteableBitmap(source);

                    var rect = new System.Drawing.Rectangle(0, 0, source.Width, source.Height);
                    var lockFormat = source.PixelFormat;

                    var bitmapData = source.LockBits(rect, ImageLockMode.ReadOnly, lockFormat);
                    try
                    {
                        var updateRect = new Int32Rect(0, 0, source.Width, source.Height);
                        var bufferSize = Math.Abs(bitmapData.Stride) * bitmapData.Height;

                        lock (_bitmapLock)
                        {
                            _writeableBitmap.Lock();
                            _writeableBitmap.WritePixels(updateRect, bitmapData.Scan0, bufferSize, bitmapData.Stride);
                            _writeableBitmap.Unlock();
                        }
                    }
                    finally
                    {
                        source.UnlockBits(bitmapData);
                    }

                    UpdateCachedImage(source);
                }
                finally
                {
                    if (!cloneBitmap && !ReferenceEquals(source, bitmap))
                    {
                        bitmap.Dispose();
                    }

                    if (cloneBitmap && createdCopy && !ReferenceEquals(source, bitmap))
                    {
                        bitmap.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RemoteDesktopElementHost.UpdateImage failed: {ex}");
            }
        }

        private void UpdateCachedImage(Bitmap latest)
        {
            lock (_bitmapLock)
            {
                if (ReferenceEquals(GetImageSafe, latest))
                {
                    return;
                }

                var oldImage = GetImageSafe;
                GetImageSafe = latest;
                oldImage?.Dispose();
            }
        }

        private Bitmap PrepareBitmapForTransfer(Bitmap input, bool cloneBitmap, out bool disposeAfterUse)
        {
            disposeAfterUse = false;

            if (IsSupportedPixelFormat(input.PixelFormat))
            {
                if (cloneBitmap)
                {
                    disposeAfterUse = true;
                    return (Bitmap)input.Clone();
                }

                return input;
            }

            var converted = new Bitmap(input.Width, input.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(converted))
            {
                g.DrawImage(input, new Rectangle(0, 0, converted.Width, converted.Height));
            }

            disposeAfterUse = true;
            return converted;
        }

        private bool IsSupportedPixelFormat(System.Drawing.Imaging.PixelFormat format)
        {
            return format == System.Drawing.Imaging.PixelFormat.Format32bppPArgb
                || format == System.Drawing.Imaging.PixelFormat.Format32bppArgb
                || format == System.Drawing.Imaging.PixelFormat.Format32bppRgb
                || format == System.Drawing.Imaging.PixelFormat.Format24bppRgb;
        }

        private void EnsureWriteableBitmap(Bitmap source)
        {
            var wpfFormat = source.PixelFormat switch
            {
                System.Drawing.Imaging.PixelFormat.Format24bppRgb => PixelFormats.Bgr24,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb => PixelFormats.Bgra32,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb => PixelFormats.Bgra32,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb => PixelFormats.Pbgra32,
                _ => PixelFormats.Bgra32
            };

            lock (_bitmapLock)
            {
                if (_writeableBitmap == null
                    || _writeableBitmap.PixelWidth != source.Width
                    || _writeableBitmap.PixelHeight != source.Height
                    || _writeableBitmap.Format != wpfFormat)
                {
                    _writeableBitmap = new WriteableBitmap(source.Width, source.Height, 96, 96, wpfFormat, null);
                    _imageControl.Source = _writeableBitmap;

                    ScreenWidth = source.Width;
                    ScreenHeight = source.Height;
                }
            }
        }

        private void CountFps()
        {
            if (_stopwatch == null)
            {
                _stopwatch = Stopwatch.StartNew();
                return;
            }

            float deltaTime = (float)_stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();

            _frameCounter.Update(deltaTime);
        }

        public void SetFrameUpdatedEvent(FrameUpdatedEventHandler handler)
        {
            _frameCounter.FrameUpdated += handler;
        }

        public void UnsetFrameUpdatedEvent(FrameUpdatedEventHandler handler)
        {
            _frameCounter.FrameUpdated -= handler;
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Keyboard.Focus(_imageControl);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DetachInputForwarders();
                lock (_bitmapLock)
                {
                    _imageControl.Source = null;
                    _writeableBitmap = null;
                    GetImageSafe?.Dispose();
                    GetImageSafe = null;
                }
            }

            base.Dispose(disposing);
        }

        private void AttachInputForwarders()
        {
            if (_inputHandlersAttached)
            {
                return;
            }

            _imageControl.MouseDown += ImageControl_MouseDown;
            _imageControl.MouseUp += ImageControl_MouseUp;
            _imageControl.MouseMove += ImageControl_MouseMove;
            _imageControl.MouseWheel += ImageControl_MouseWheel;
            _imageControl.PreviewKeyDown += ImageControl_PreviewKeyDown;
            _imageControl.PreviewKeyUp += ImageControl_PreviewKeyUp;

            _inputHandlersAttached = true;
        }

        private void DetachInputForwarders()
        {
            if (!_inputHandlersAttached)
            {
                return;
            }

            _imageControl.MouseDown -= ImageControl_MouseDown;
            _imageControl.MouseUp -= ImageControl_MouseUp;
            _imageControl.MouseMove -= ImageControl_MouseMove;
            _imageControl.MouseWheel -= ImageControl_MouseWheel;
            _imageControl.PreviewKeyDown -= ImageControl_PreviewKeyDown;
            _imageControl.PreviewKeyUp -= ImageControl_PreviewKeyUp;

            _inputHandlersAttached = false;
        }

        private void ImageControl_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Focus();
            var args = ConvertMouseEventArgs(e);
            base.OnMouseDown(args);
            e.Handled = true;
        }

        private void ImageControl_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var args = ConvertMouseEventArgs(e);
            base.OnMouseUp(args);
            e.Handled = true;
        }

        private void ImageControl_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            var args = ConvertMouseEventArgs(e);
            base.OnMouseMove(args);
        }

        private void ImageControl_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            var args = ConvertMouseEventArgs(e, WFMouseButtons.None, 0);
            base.OnMouseWheel(args);
            e.Handled = true;
        }

        private void ImageControl_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var args = ConvertKeyEventArgs(e);
            base.OnKeyDown(args);
            e.Handled = args.Handled;
        }

        private void ImageControl_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var args = ConvertKeyEventArgs(e);
            base.OnKeyUp(args);
            e.Handled = args.Handled;
        }

        private WFMouseEventArgs ConvertMouseEventArgs(System.Windows.Input.MouseEventArgs e, WFMouseButtons? overrideButton = null, int? overrideClickCount = null)
        {
            var position = e.GetPosition(_imageControl);
            var winPoint = TransformToWinForms(position);
            var button = overrideButton.HasValue ? overrideButton.Value : ConvertButton(e);
            var clickCount = overrideClickCount ?? (e is System.Windows.Input.MouseButtonEventArgs mbe ? mbe.ClickCount : 0);
            var delta = e is System.Windows.Input.MouseWheelEventArgs wheel ? wheel.Delta : 0;
            return new WFMouseEventArgs(button, clickCount, winPoint.X, winPoint.Y, delta);
        }

        private WFMouseButtons ConvertButton(System.Windows.Input.MouseEventArgs e)
        {
            if (e is System.Windows.Input.MouseButtonEventArgs mbe)
            {
                return mbe.ChangedButton switch
                {
                    MouseButton.Left => WFMouseButtons.Left,
                    MouseButton.Right => WFMouseButtons.Right,
                    MouseButton.Middle => WFMouseButtons.Middle,
                    MouseButton.XButton1 => WFMouseButtons.XButton1,
                    MouseButton.XButton2 => WFMouseButtons.XButton2,
                    _ => WFMouseButtons.None
                };
            }

            if (e.LeftButton == MouseButtonState.Pressed) return WFMouseButtons.Left;
            if (e.RightButton == MouseButtonState.Pressed) return WFMouseButtons.Right;
            if (e.MiddleButton == MouseButtonState.Pressed) return WFMouseButtons.Middle;
            return WFMouseButtons.None;
        }

        private System.Drawing.Point TransformToWinForms(System.Windows.Point point)
        {
            var presentationSource = PresentationSource.FromVisual(_imageControl);
            if (presentationSource != null)
            {
                var m = presentationSource.CompositionTarget.TransformToDevice;
                var x = (int)Math.Round(point.X * m.M11);
                var y = (int)Math.Round(point.Y * m.M22);
                return new System.Drawing.Point(x, y);
            }

            return new System.Drawing.Point((int)Math.Round(point.X), (int)Math.Round(point.Y));
        }

        private System.Windows.Forms.KeyEventArgs ConvertKeyEventArgs(System.Windows.Input.KeyEventArgs e)
        {
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            System.Windows.Input.ModifierKeys modifiers = Keyboard.Modifiers;

            var keyData = (System.Windows.Forms.Keys)virtualKey;

            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control))
            {
                keyData |= System.Windows.Forms.Keys.Control;
            }

            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
            {
                keyData |= System.Windows.Forms.Keys.Shift;
            }

            if (modifiers.HasFlag(System.Windows.Input.ModifierKeys.Alt))
            {
                keyData |= System.Windows.Forms.Keys.Alt;
            }

            return new System.Windows.Forms.KeyEventArgs(keyData);
        }
    }
}