using Pulsar.Server.Controls;

namespace Pulsar.Server.Forms
{
    partial class FrmRemoteWebcam
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRemoteWebcam));
            btnStart = new System.Windows.Forms.Button();
            btnStop = new System.Windows.Forms.Button();
            barQuality = new System.Windows.Forms.TrackBar();
            lblQuality = new System.Windows.Forms.Label();
            lblQualityShow = new System.Windows.Forms.Label();
            panelTop = new System.Windows.Forms.Panel();
            sizeLabelCounter = new System.Windows.Forms.Label();
            cbMonitors = new System.Windows.Forms.ComboBox();
            btnHide = new System.Windows.Forms.Button();
            btnShow = new System.Windows.Forms.Button();
            toolTipButtons = new System.Windows.Forms.ToolTip(components);
            picWebcam = new RemoteDesktopElementHost();
            ((System.ComponentModel.ISupportInitialize)barQuality).BeginInit();
            panelTop.SuspendLayout();
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
            barQuality.Location = new System.Drawing.Point(700, 1);
            barQuality.Maximum = 100;
            barQuality.Minimum = 1;
            barQuality.Name = "barQuality";
            barQuality.Size = new System.Drawing.Size(109, 45);
            barQuality.TabIndex = 3;
            barQuality.TabStop = false;
            barQuality.Value = 100;
            barQuality.Scroll += barQuality_Scroll;
            // 
            // lblQuality
            // 
            lblQuality.AutoSize = true;
            lblQuality.Location = new System.Drawing.Point(648, 3);
            lblQuality.Name = "lblQuality";
            lblQuality.Size = new System.Drawing.Size(46, 13);
            lblQuality.TabIndex = 4;
            lblQuality.Text = "Quality:";
            // 
            // lblQualityShow
            // 
            lblQualityShow.AutoSize = true;
            lblQualityShow.Location = new System.Drawing.Point(648, 16);
            lblQualityShow.Name = "lblQualityShow";
            lblQualityShow.Size = new System.Drawing.Size(56, 13);
            lblQualityShow.TabIndex = 5;
            lblQualityShow.Text = "100 (best)";
            // 
            // panelTop
            // 
            panelTop.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            panelTop.Controls.Add(sizeLabelCounter);
            panelTop.Controls.Add(cbMonitors);
            panelTop.Controls.Add(btnHide);
            panelTop.Controls.Add(lblQualityShow);
            panelTop.Controls.Add(btnStart);
            panelTop.Controls.Add(btnStop);
            panelTop.Controls.Add(lblQuality);
            panelTop.Controls.Add(barQuality);
            panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            panelTop.Location = new System.Drawing.Point(0, 0);
            panelTop.Name = "panelTop";
            panelTop.Size = new System.Drawing.Size(882, 37);
            panelTop.TabIndex = 7;
            // 
            // sizeLabelCounter
            // 
            sizeLabelCounter.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            sizeLabelCounter.Location = new System.Drawing.Point(805, 8);
            sizeLabelCounter.Name = "sizeLabelCounter";
            sizeLabelCounter.Size = new System.Drawing.Size(64, 18);
            sizeLabelCounter.TabIndex = 12;
            sizeLabelCounter.Text = "Size:";
            sizeLabelCounter.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbMonitors
            // 
            cbMonitors.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbMonitors.FormattingEnabled = true;
            cbMonitors.Location = new System.Drawing.Point(159, 5);
            cbMonitors.Name = "cbMonitors";
            cbMonitors.Size = new System.Drawing.Size(423, 21);
            cbMonitors.TabIndex = 8;
            cbMonitors.TabStop = false;
            // 
            // btnHide
            // 
            btnHide.Location = new System.Drawing.Point(588, 3);
            btnHide.Name = "btnHide";
            btnHide.Size = new System.Drawing.Size(54, 28);
            btnHide.TabIndex = 7;
            btnHide.TabStop = false;
            btnHide.Text = "Hide";
            btnHide.UseVisualStyleBackColor = true;
            btnHide.Click += btnHide_Click;
            // 
            // btnShow
            // 
            btnShow.Location = new System.Drawing.Point(828, 534);
            btnShow.Name = "btnShow";
            btnShow.Size = new System.Drawing.Size(54, 28);
            btnShow.TabIndex = 8;
            btnShow.TabStop = false;
            btnShow.Text = "Show";
            btnShow.UseVisualStyleBackColor = true;
            btnShow.Visible = false;
            btnShow.Click += btnShow_Click;
            // 
            // picWebcam
            // 
            picWebcam.BackColor = System.Drawing.Color.Black;
            picWebcam.Dock = System.Windows.Forms.DockStyle.Fill;
            picWebcam.Location = new System.Drawing.Point(0, 37);
            picWebcam.Margin = new System.Windows.Forms.Padding(0);
            picWebcam.Name = "picWebcam";
            picWebcam.Size = new System.Drawing.Size(882, 525);
            picWebcam.TabIndex = 0;
            picWebcam.TabStop = false;
            // 
            // FrmRemoteWebcam
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(882, 562);
            Controls.Add(btnShow);
            Controls.Add(picWebcam);
            Controls.Add(panelTop);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MinimumSize = new System.Drawing.Size(640, 480);
            Name = "FrmRemoteWebcam";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Remote Webcam []";
            FormClosing += FrmRemoteWebcam_FormClosing;
            Load += FrmRemoteWebcam_Load;
            Resize += FrmRemoteWebcam_Resize;
            ((System.ComponentModel.ISupportInitialize)barQuality).EndInit();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnStop;
        private System.Windows.Forms.TrackBar barQuality;
        private System.Windows.Forms.Label lblQuality;
        private System.Windows.Forms.Label lblQualityShow;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Button btnHide;
        private System.Windows.Forms.Button btnShow;
        private System.Windows.Forms.ComboBox cbMonitors;
        private System.Windows.Forms.ToolTip toolTipButtons;
        private Controls.RemoteDesktopElementHost picWebcam;
        private System.Windows.Forms.Label sizeLabelCounter;
    }
}