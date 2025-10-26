using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Pulsar.Server.Controls
{
    /// <summary>
    /// A custom control that visualizes audio levels with smooth animations in dark mode.
    /// </summary>
    public class AudioVisualizer : Control
    {
        private const int BAR_SPACING = 2;
        private int _barCount = 40;
        private const float DECAY_RATE = 0.92f;
        private const float SMOOTHING = 0.3f;

        private float[] _barHeights;
        private float[] _targetHeights;
        private float _currentLevel = 0f;
        private float _targetLevel = 0f;

        private Timer _animationTimer;
        private Random _random = new Random();

        // Dark mode colors
        private readonly Color _backgroundColor = Color.FromArgb(28, 28, 28);
        private readonly Color _barColorLow = Color.FromArgb(0, 150, 255);
        private readonly Color _barColorMid = Color.FromArgb(0, 200, 100);
        private readonly Color _barColorHigh = Color.FromArgb(255, 100, 0);

        public AudioVisualizer()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            BackColor = _backgroundColor;

            _animationTimer = new Timer();
            _animationTimer.Interval = 33; // ~30 FPS
            _animationTimer.Tick += AnimationTimer_Tick;
            _animationTimer.Start();

            CalculateBarCount();
            InitializeBars();
        }

        /// <summary>
        /// Calculates the optimal number of bars to fill the control width.
        /// </summary>
        private void CalculateBarCount()
        {
            if (Width > 0)
            {
                // Calculate how many bars can fit (minimum 3 pixels per bar + spacing)
                _barCount = Math.Max(20, Width / 5);
            }
            else
            {
                _barCount = 40; // Default
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            CalculateBarCount();
            InitializeBars();
        }

        private void InitializeBars()
        {
            _barHeights = new float[_barCount];
            _targetHeights = new float[_barCount];

            for (int i = 0; i < _barCount; i++)
            {
                _barHeights[i] = 0f;
                _targetHeights[i] = 0f;
            }
        }

        /// <summary>
        /// Updates the audio level for visualization.
        /// </summary>
        /// <param name="level">Audio level between 0.0 and 1.0</param>
        public void UpdateLevel(float level)
        {
            _targetLevel = Math.Max(0f, Math.Min(1f, level));
        }

        /// <summary>
        /// Resets the visualizer to zero.
        /// </summary>
        public void Reset()
        {
            _targetLevel = 0f;
            _currentLevel = 0f;
            if (_barHeights != null)
            {
                for (int i = 0; i < _barCount; i++)
                {
                    _barHeights[i] = 0f;
                    _targetHeights[i] = 0f;
                }
            }
            Invalidate();
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            if (_barHeights == null || _targetHeights == null)
                return;

            _currentLevel += (_targetLevel - _currentLevel) * SMOOTHING;

            for (int i = 0; i < _barCount; i++)
            {
                float baseHeight = _currentLevel;

                if (_currentLevel > 0.01f)
                {
                    float variation = (float)_random.NextDouble() * 0.3f - 0.15f;
                    _targetHeights[i] = Math.Max(0f, Math.Min(1f, baseHeight + variation));
                }
                else
                {
                    _targetHeights[i] = 0f;
                }

                if (_barHeights[i] < _targetHeights[i])
                {
                    _barHeights[i] += (_targetHeights[i] - _barHeights[i]) * 0.4f;
                }
                else
                {
                    _barHeights[i] *= DECAY_RATE;
                }
            }

            _targetLevel *= 0.85f;

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_barHeights == null || _targetHeights == null)
                return;

            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int barWidth = (Width - (_barCount - 1) * BAR_SPACING) / _barCount;
            if (barWidth < 1) barWidth = 1;

            for (int i = 0; i < _barCount; i++)
            {
                int x = i * (barWidth + BAR_SPACING);
                int barHeight = (int)(_barHeights[i] * Height);
                int y = Height - barHeight;

                if (barHeight > 0)
                {
                    Color barColor;
                    if (_barHeights[i] < 0.5f)
                    {
                        barColor = _barColorLow;
                    }
                    else if (_barHeights[i] < 0.8f)
                    {
                        barColor = _barColorMid;
                    }
                    else
                    {
                        barColor = _barColorHigh;
                    }

                    using (SolidBrush brush = new SolidBrush(barColor))
                    {
                        g.FillRectangle(brush, x, y, barWidth, barHeight);
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Stop();
                _animationTimer?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}