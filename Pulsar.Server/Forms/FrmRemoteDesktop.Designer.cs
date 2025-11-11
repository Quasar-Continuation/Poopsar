using Pulsar.Server.Controls;
using Pulsar.Server.Images.Helpers;

namespace Pulsar.Server.Forms
{
    partial class FrmRemoteDesktop
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRemoteDesktop));
            btnStart = new System.Windows.Forms.Button();
            btnStop = new System.Windows.Forms.Button();
            barQuality = new System.Windows.Forms.TrackBar();
            lblQuality = new System.Windows.Forms.Label();
            lblQualityShow = new System.Windows.Forms.Label();
            btnMouse = new System.Windows.Forms.Button();
            panelTop = new System.Windows.Forms.Panel();
            btnBiDirectionalClipboard = new System.Windows.Forms.Button();
            btnStartProgramOnDisplay = new System.Windows.Forms.Button();
            btnShowDrawingTools = new System.Windows.Forms.Button();
            sizeLabelCounter = new System.Windows.Forms.Label();
            enableGPU = new System.Windows.Forms.Button();
            btnKeyboard = new System.Windows.Forms.Button();
            cbMonitors = new System.Windows.Forms.ComboBox();
            btnHide = new System.Windows.Forms.Button();
            panelDrawingTools = new System.Windows.Forms.Panel();
            colorPicker = new System.Windows.Forms.Button();
            strokeWidthTrackBar = new System.Windows.Forms.TrackBar();
            btnDrawing = new System.Windows.Forms.Button();
            btnEraser = new System.Windows.Forms.Button();
            btnClearDrawing = new System.Windows.Forms.Button();
            btnShow = new System.Windows.Forms.Button();
            toolTipButtons = new System.Windows.Forms.ToolTip(components);
            picDesktop = new RemoteDesktopElementHost();
            ((System.ComponentModel.ISupportInitialize)barQuality).BeginInit();
            panelTop.SuspendLayout();
            panelDrawingTools.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)strokeWidthTrackBar).BeginInit();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.Location = new System.Drawing.Point(11, 3);
            btnStart.Name = "btnStart";
            btnStart.Size = new System.Drawing.Size(68, 28);
            btnStart.TabIndex = 1;
            btnStart.TabStop = false;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnStop
            // 
            btnStop.Enabled = false;
            btnStop.Location = new System.Drawing.Point(85, 3);
            btnStop.Name = "btnStop";
            btnStop.Size = new System.Drawing.Size(68, 28);
            btnStop.TabIndex = 2;
            btnStop.TabStop = false;
            btnStop.Text = "Stop";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;
            // 
            // barQuality
            // 
            barQuality.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            barQuality.Location = new System.Drawing.Point(515, 3);
            barQuality.Maximum = 100;
            barQuality.Minimum = 1;
            barQuality.Name = "barQuality";
            barQuality.Size = new System.Drawing.Size(94, 45);
            barQuality.TabIndex = 3;
            barQuality.TabStop = false;
            barQuality.Value = 100;
            barQuality.Scroll += barQuality_Scroll;
            // 
            // lblQuality
            // 
            lblQuality.AutoSize = true;
            lblQuality.Location = new System.Drawing.Point(463, 5);
            lblQuality.Name = "lblQuality";
            lblQuality.Size = new System.Drawing.Size(46, 13);
            lblQuality.TabIndex = 4;
            lblQuality.Text = "Quality:";
            // 
            // lblQualityShow
            // 
            lblQualityShow.AutoSize = true;
            lblQualityShow.Location = new System.Drawing.Point(463, 18);
            lblQualityShow.Name = "lblQualityShow";
            lblQualityShow.Size = new System.Drawing.Size(56, 13);
            lblQualityShow.TabIndex = 5;
            lblQualityShow.Text = "100 (best)";
            // 
            // btnMouse
            // 
            btnMouse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnMouse.Image = Properties.Resources.mouse_delete;
            btnMouse.Location = new System.Drawing.Point(678, 3);
            btnMouse.Name = "btnMouse";
            btnMouse.Size = new System.Drawing.Size(28, 28);
            btnMouse.TabIndex = 6;
            btnMouse.TabStop = false;
            toolTipButtons.SetToolTip(btnMouse, "Enable mouse input.");
            btnMouse.UseVisualStyleBackColor = true;
            btnMouse.Click += btnMouse_Click;
            // 
            // panelTop
            // 
            panelTop.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            panelTop.Controls.Add(btnBiDirectionalClipboard);
            panelTop.Controls.Add(btnStartProgramOnDisplay);
            panelTop.Controls.Add(btnShowDrawingTools);
            panelTop.Controls.Add(sizeLabelCounter);
            panelTop.Controls.Add(enableGPU);
            panelTop.Controls.Add(btnKeyboard);
            panelTop.Controls.Add(cbMonitors);
            panelTop.Controls.Add(btnHide);
            panelTop.Controls.Add(lblQualityShow);
            panelTop.Controls.Add(btnMouse);
            panelTop.Controls.Add(btnStart);
            panelTop.Controls.Add(btnStop);
            panelTop.Controls.Add(lblQuality);
            panelTop.Controls.Add(barQuality);
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.Location = new System.Drawing.Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new System.Drawing.Size(848, 36);
            panelTop.TabIndex = 7;
            // 
            // btnBiDirectionalClipboard
            // 
            btnBiDirectionalClipboard.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnBiDirectionalClipboard.Image = (System.Drawing.Image)resources.GetObject("btnBiDirectionalClipboard.Image");
            btnBiDirectionalClipboard.Location = new System.Drawing.Point(610, 3);
            btnBiDirectionalClipboard.Name = "btnBiDirectionalClipboard";
            btnBiDirectionalClipboard.Size = new System.Drawing.Size(28, 28);
            btnBiDirectionalClipboard.TabIndex = 19;
            btnBiDirectionalClipboard.TabStop = false;
            toolTipButtons.SetToolTip(btnBiDirectionalClipboard, "Enable mouse input.");
            btnBiDirectionalClipboard.UseVisualStyleBackColor = true;
            btnBiDirectionalClipboard.Click += btnBiDirectionalClipboard_Click;
            // 
            // btnStartProgramOnDisplay
            // 
            btnStartProgramOnDisplay.Image = Properties.Resources.application_add;
            btnStartProgramOnDisplay.Location = new System.Drawing.Point(350, 3);
            btnStartProgramOnDisplay.Name = "btnStartProgramOnDisplay";
            btnStartProgramOnDisplay.Size = new System.Drawing.Size(47, 28);
            btnStartProgramOnDisplay.TabIndex = 18;
            btnStartProgramOnDisplay.UseVisualStyleBackColor = true;
            btnStartProgramOnDisplay.Click += btnStartProgramOnDisplay_Click;
            // 
            // btnShowDrawingTools
            // 
            btnShowDrawingTools.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnShowDrawingTools.Image = Properties.Resources.arrow_up;
            btnShowDrawingTools.Location = new System.Drawing.Point(746, 3);
            btnShowDrawingTools.Name = "btnShowDrawingTools";
            btnShowDrawingTools.Size = new System.Drawing.Size(28, 28);
            btnShowDrawingTools.TabIndex = 17;
            btnShowDrawingTools.TabStop = false;
            toolTipButtons.SetToolTip(btnShowDrawingTools, "Show drawing tools");
            btnShowDrawingTools.UseVisualStyleBackColor = true;
            btnShowDrawingTools.Click += btnShowDrawingTools_Click;
            // 
            // sizeLabelCounter
            // 
            sizeLabelCounter.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            sizeLabelCounter.Location = new System.Drawing.Point(780, 8);
            sizeLabelCounter.Name = "sizeLabelCounter";
            sizeLabelCounter.Size = new System.Drawing.Size(55, 23);
            sizeLabelCounter.TabIndex = 11;
            sizeLabelCounter.Text = "Size: ";
            // 
            // enableGPU
            // 
            enableGPU.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            enableGPU.Image = Properties.Resources.computer_error;
            enableGPU.Location = new System.Drawing.Point(644, 3);
            enableGPU.Name = "enableGPU";
            enableGPU.Size = new System.Drawing.Size(28, 28);
            enableGPU.TabIndex = 10;
            enableGPU.TabStop = false;
            toolTipButtons.SetToolTip(enableGPU, "Enable mouse input.");
            enableGPU.UseVisualStyleBackColor = true;
            enableGPU.Click += enableGPU_Click;
            // 
            // btnKeyboard
            // 
            btnKeyboard.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnKeyboard.Image = Properties.Resources.keyboard_delete;
            btnKeyboard.Location = new System.Drawing.Point(712, 3);
            btnKeyboard.Name = "btnKeyboard";
            btnKeyboard.Size = new System.Drawing.Size(28, 28);
            btnKeyboard.TabIndex = 9;
            btnKeyboard.TabStop = false;
            toolTipButtons.SetToolTip(btnKeyboard, "Enable keyboard input.");
            btnKeyboard.UseVisualStyleBackColor = true;
            btnKeyboard.Click += btnKeyboard_Click;
            // 
            // cbMonitors
            // 
            cbMonitors.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbMonitors.FormattingEnabled = true;
            cbMonitors.Location = new System.Drawing.Point(159, 5);
            cbMonitors.Name = "cbMonitors";
            cbMonitors.Size = new System.Drawing.Size(185, 21);
            cbMonitors.TabIndex = 8;
            cbMonitors.TabStop = false;
            // 
            // btnHide
            // 
            btnHide.Location = new System.Drawing.Point(403, 3);
            btnHide.Name = "btnHide";
            btnHide.Size = new System.Drawing.Size(54, 28);
            btnHide.TabIndex = 7;
            btnHide.TabStop = false;
            btnHide.Text = "Hide";
            btnHide.UseVisualStyleBackColor = true;
            btnHide.Click += btnHide_Click;
            // 
            // panelDrawingTools
            // 
            panelDrawingTools.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            panelDrawingTools.Controls.Add(colorPicker);
            panelDrawingTools.Controls.Add(strokeWidthTrackBar);
            panelDrawingTools.Controls.Add(btnDrawing);
            panelDrawingTools.Controls.Add(btnEraser);
            panelDrawingTools.Controls.Add(btnClearDrawing);
            panelDrawingTools.Dock = System.Windows.Forms.DockStyle.Top;
            panelDrawingTools.Location = new System.Drawing.Point(0, 36);
            panelDrawingTools.Name = "panelDrawingTools";
            panelDrawingTools.Size = new System.Drawing.Size(848, 36);
            panelDrawingTools.TabIndex = 8;
            panelDrawingTools.Visible = false;
            // 
            // colorPicker
            // 
            colorPicker.Location = new System.Drawing.Point(11, 3);
            colorPicker.Name = "colorPicker";
            colorPicker.Size = new System.Drawing.Size(60, 28);
            colorPicker.TabIndex = 12;
            colorPicker.TabStop = false;
            colorPicker.Text = "Color";
            toolTipButtons.SetToolTip(colorPicker, "Select drawing color");
            colorPicker.UseVisualStyleBackColor = true;
            // 
            // strokeWidthTrackBar
            // 
            strokeWidthTrackBar.Location = new System.Drawing.Point(275, 3);
            strokeWidthTrackBar.Minimum = 1;
            strokeWidthTrackBar.Name = "strokeWidthTrackBar";
            strokeWidthTrackBar.Size = new System.Drawing.Size(100, 45);
            strokeWidthTrackBar.TabIndex = 13;
            strokeWidthTrackBar.TabStop = false;
            toolTipButtons.SetToolTip(strokeWidthTrackBar, "Adjust stroke width");
            strokeWidthTrackBar.Value = 5;
            // 
            // btnDrawing
            // 
            btnDrawing.BackgroundImage = Properties.Resources.pencil;
            btnDrawing.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            btnDrawing.Location = new System.Drawing.Point(77, 3);
            btnDrawing.Name = "btnDrawing";
            btnDrawing.Size = new System.Drawing.Size(60, 28);
            btnDrawing.TabIndex = 14;
            btnDrawing.TabStop = false;
            toolTipButtons.SetToolTip(btnDrawing, "Enable drawing");
            btnDrawing.UseVisualStyleBackColor = false;
            btnDrawing.Click += btnDrawing_Click;
            // 
            // btnEraser
            // 
            btnEraser.BackgroundImage = Properties.Resources.eraser;
            btnEraser.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            btnEraser.Location = new System.Drawing.Point(143, 3);
            btnEraser.Name = "btnEraser";
            btnEraser.Size = new System.Drawing.Size(60, 28);
            btnEraser.TabIndex = 15;
            btnEraser.TabStop = false;
            toolTipButtons.SetToolTip(btnEraser, "Enable eraser");
            btnEraser.UseVisualStyleBackColor = false;
            btnEraser.Click += btnEraser_Click;
            // 
            // btnClearDrawing
            // 
            btnClearDrawing.BackgroundImage = Properties.Resources.clear;
            btnClearDrawing.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            btnClearDrawing.Location = new System.Drawing.Point(209, 3);
            btnClearDrawing.Name = "btnClearDrawing";
            btnClearDrawing.Size = new System.Drawing.Size(60, 28);
            btnClearDrawing.TabIndex = 16;
            btnClearDrawing.TabStop = false;
            toolTipButtons.SetToolTip(btnClearDrawing, "Clear drawing");
            btnClearDrawing.UseVisualStyleBackColor = false;
            btnClearDrawing.Click += btnClearDrawing_Click;
            // 
            // btnShow
            // 
            btnShow.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnShow.Location = new System.Drawing.Point(794, 534);
            btnShow.Name = "btnShow";
            btnShow.Size = new System.Drawing.Size(54, 28);
            btnShow.TabIndex = 8;
            btnShow.TabStop = false;
            btnShow.Text = "Show";
            btnShow.UseVisualStyleBackColor = true;
            btnShow.Visible = false;
            btnShow.Click += btnShow_Click;
            // 
            // picDesktop
            // 
            picDesktop.BackColor = System.Drawing.Color.Black;
            picDesktop.Dock = System.Windows.Forms.DockStyle.Fill;
            picDesktop.Location = new System.Drawing.Point(0, 0);
            picDesktop.Margin = new System.Windows.Forms.Padding(0);
            picDesktop.Name = "picDesktop";
            picDesktop.Size = new System.Drawing.Size(848, 562);
            picDesktop.TabIndex = 0;
            picDesktop.TabStop = false;
            picDesktop.MouseDown += picDesktop_MouseDown;
            picDesktop.MouseMove += picDesktop_MouseMove;
            picDesktop.MouseUp += picDesktop_MouseUp;
            // 
            // FrmRemoteDesktop
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(848, 562);
            Controls.Add(btnShow);
            Controls.Add(panelDrawingTools);
            Controls.Add(panelTop);
            Controls.Add(picDesktop);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MinimumSize = new System.Drawing.Size(640, 480);
            Name = "FrmRemoteDesktop";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Remote Desktop []";
            FormClosing += FrmRemoteDesktop_FormClosing;
            Load += FrmRemoteDesktop_Load;
            Resize += FrmRemoteDesktop_Resize;
            ((System.ComponentModel.ISupportInitialize)barQuality).EndInit();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            panelDrawingTools.ResumeLayout(false);
            panelDrawingTools.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)strokeWidthTrackBar).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.TrackBar barQuality;
        private System.Windows.Forms.Label lblQuality;
        private System.Windows.Forms.Label lblQualityShow;
        private System.Windows.Forms.Button btnMouse;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnHide;
        private System.Windows.Forms.Button btnShow;
        private System.Windows.Forms.ComboBox cbMonitors;
        private System.Windows.Forms.Button btnKeyboard;
        private System.Windows.Forms.ToolTip toolTipButtons;
        private Controls.RemoteDesktopElementHost picDesktop;
        private System.Windows.Forms.Button enableGPU;
        private System.Windows.Forms.Label sizeLabelCounter;
        private System.Windows.Forms.Button colorPicker;
        private System.Windows.Forms.TrackBar strokeWidthTrackBar;
        private System.Windows.Forms.Button btnDrawing;
        private System.Windows.Forms.Button btnEraser;
        private System.Windows.Forms.Button btnClearDrawing;
        private System.Windows.Forms.Button btnShowDrawingTools;
        private System.Windows.Forms.Panel panelDrawingTools;
        private System.Windows.Forms.Button btnStartProgramOnDisplay;
        private System.Windows.Forms.Button btnBiDirectionalClipboard;
    }
}