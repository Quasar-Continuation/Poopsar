using Pulsar.Server.Utilities;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Pulsar.Server.Controls
{
    public interface IRapidPictureBox
    {
        bool Running { get; set; }
        Image GetImageSafe { get; set; }

        void Start();
        void Stop();
        void UpdateImage(Bitmap bmp, bool cloneBitmap = false);
    }

    /// <summary>
    /// Custom PictureBox Control designed for rapidly-changing images.
    /// </summary>
    public class RapidPictureBox : PictureBox, IRapidPictureBox
    {
        /// <summary>
        /// True if the PictureBox is currently streaming images, else False.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool Running { get; set; }

    /// <summary>
    /// Returns the width of the original screen.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int ScreenWidth { get; private set; }

    /// <summary>
    /// Returns the height of the original screen.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public int ScreenHeight { get; private set; }

        /// <summary>
        /// Provides thread-safe access to the Image of this Picturebox.
        /// </summary>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Image GetImageSafe
        {
            get
            {
                lock (_imageLock)
                {
                    return _frame;
                }
            }
            set
            {
                lock (_imageLock)
                {
                    var old = _frame;
                    _frame = value as Bitmap;
                    old?.Dispose();
                }

                RequestRepaint();
            }
        }

        /// <summary>
        /// The lock object for the Picturebox's image.
        /// </summary>
    private readonly object _imageLock = new object();

    /// <summary>
    /// Latest frame to draw. We avoid assigning to PictureBox.Image to prevent cross-thread UI access and allocations.
    /// </summary>
    private Bitmap _frame;

    /// <summary>
    /// Small placeholder assigned to base.Image so existing code paths that check Image != null keep working.
    /// </summary>
    private Bitmap _placeholder;

    /// <summary>
    /// Prevent flooding the message queue; coalesce multiple UpdateImage calls into one repaint.
    /// </summary>
    private bool _repaintPending;

        /// <summary>
        /// The Stopwatch for internal FPS measuring.
        /// </summary>
        private Stopwatch _sWatch;

        /// <summary>
        /// The internal class for FPS measuring.
        /// </summary>
        private FrameCounter _frameCounter;

        /// <summary>
        /// Subscribes an Eventhandler to the FrameUpdated event.
        /// </summary>
        /// <param name="e">The Eventhandler to set.</param>
        public void SetFrameUpdatedEvent(FrameUpdatedEventHandler e)
        {
            _frameCounter.FrameUpdated += e;
        }

        /// <summary>
        /// Unsubscribes an Eventhandler from the FrameUpdated event.
        /// </summary>
        /// <param name="e">The Eventhandler to remove.</param>
        public void UnsetFrameUpdatedEvent(FrameUpdatedEventHandler e)
        {
            _frameCounter.FrameUpdated -= e;
        }

        /// <summary>
        /// Starts the internal FPS measuring.
        /// </summary>
        public void Start()
        {
            _frameCounter = new FrameCounter();

            _sWatch = Stopwatch.StartNew();

            Running = true;
        }

        /// <summary>
        /// Stops the internal FPS measuring.
        /// </summary>
        public void Stop()
        {
            _sWatch?.Stop();

            Running = false;
        }

        /// <summary>
        /// Updates the Image of this Picturebox.
        /// </summary>
        /// <param name="bmp">The new bitmap to use.</param>
        /// <param name="cloneBitmap">If True the bitmap will be cloned, else it uses the original bitmap.</param>
        public void UpdateImage(Bitmap bmp, bool cloneBitmap)
        {
            try
            {
                CountFps();

                if ((ScreenWidth != bmp.Width) || (ScreenHeight != bmp.Height))
                    UpdateScreenSize(bmp.Width, bmp.Height);

                // Swap the frame without resizing; scaling is handled in OnPaint for speed.
                lock (_imageLock)
                {
                    var old = _frame;
                    _frame = cloneBitmap ? (Bitmap)bmp.Clone() : bmp;
                    old?.Dispose();
                }

                RequestRepaint();
            }
            catch (InvalidOperationException)
            {
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Constructor, sets Picturebox double-buffered and initializes the Framecounter.
        /// </summary>
        public RapidPictureBox()
        {
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);

            _placeholder = new Bitmap(1, 1);
            _placeholder.SetPixel(0, 0, Color.Transparent);
            base.Image = _placeholder;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Bitmap localFrame = null;
            lock (_imageLock)
            {
                if (_frame == null)
                    return;
                localFrame = _frame;
            }

            var g = pe.Graphics;
            g.SmoothingMode = SmoothingMode.None;
            g.CompositingMode = CompositingMode.SourceCopy;
            g.CompositingQuality = CompositingQuality.HighSpeed;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            if (localFrame != null)
            {
                var cs = this.ClientSize;
                if (cs.Width <= 0 || cs.Height <= 0)
                    return;

                double sx = (double)cs.Width / localFrame.Width;
                double sy = (double)cs.Height / localFrame.Height;
                bool upscaling = sx >= 1.0 || sy >= 1.0;
                g.InterpolationMode = upscaling ? InterpolationMode.NearestNeighbor : InterpolationMode.Low;

                g.DrawImage(localFrame, this.ClientRectangle);
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pevent)
        {
            if (this.BackColor.A == 255)
            {
                using (var b = new SolidBrush(this.BackColor))
                {
                    pevent.Graphics.FillRectangle(b, this.ClientRectangle);
                }
            }
            else
            {
                base.OnPaintBackground(pevent);
            }
        }

        private void UpdateScreenSize(int newWidth, int newHeight)
        {
            ScreenWidth = newWidth;
            ScreenHeight = newHeight;
        }

        private void CountFps()
        {
            var deltaTime = (float)_sWatch.Elapsed.TotalSeconds;
            _sWatch = Stopwatch.StartNew();

            _frameCounter.Update(deltaTime);
        }

        private void RequestRepaint()
        {
            if (_repaintPending)
                return;

            _repaintPending = true;

            void doInvalidate()
            {
                if (!IsDisposed)
                {
                    Invalidate();
                    Update();
                }
                _repaintPending = false;
            }

            if (IsHandleCreated && InvokeRequired)
            {
                try { BeginInvoke((Action)(doInvalidate)); } catch { _repaintPending = false; }
            }
            else
            {
                doInvalidate();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_imageLock)
                {
                    _frame?.Dispose();
                    _frame = null;
                }
                try
                {
                    if (ReferenceEquals(base.Image, _placeholder))
                    {
                        base.Image = null;
                    }
                    _placeholder?.Dispose();
                }
                catch { }
                finally
                {
                    _placeholder = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}