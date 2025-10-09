namespace Pulsar.Server.Forms
{
    partial class FrmSettings
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSettings));
            btnSave = new System.Windows.Forms.Button();
            lblPort = new System.Windows.Forms.Label();
            ncPort = new System.Windows.Forms.NumericUpDown();
            chkAutoListen = new System.Windows.Forms.CheckBox();
            chkPopup = new System.Windows.Forms.CheckBox();
            btnListen = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            chkUseUpnp = new System.Windows.Forms.CheckBox();
            chkShowTooltip = new System.Windows.Forms.CheckBox();
            chkIPv6Support = new System.Windows.Forms.CheckBox();
            chkDarkMode = new System.Windows.Forms.CheckBox();
            chkEventLog = new System.Windows.Forms.CheckBox();
            chkDiscordRPC = new System.Windows.Forms.CheckBox();
            chkTelegramNotis = new System.Windows.Forms.CheckBox();
            txtTelegramToken = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            txtTelegramChatID = new System.Windows.Forms.TextBox();
            label2 = new System.Windows.Forms.Label();
            button1 = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            BlockedRichTB = new System.Windows.Forms.RichTextBox();
            chkHideFromScreenCapture = new System.Windows.Forms.CheckBox();
            lblMultiPorts = new System.Windows.Forms.Label();
            txtMultiPorts = new System.Windows.Forms.TextBox();
            chkShowCountryGroups = new System.Windows.Forms.CheckBox();
            label3 = new System.Windows.Forms.Label();
            comboLanguages = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)ncPort).BeginInit();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // btnSave
            // 
            btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnSave.Location = new System.Drawing.Point(227, 583);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(121, 23);
            btnSave.TabIndex = 19;
            btnSave.Text = "&Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // lblPort
            // 
            lblPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lblPort.AutoSize = true;
            lblPort.Location = new System.Drawing.Point(12, 11);
            lblPort.Name = "lblPort";
            lblPort.Size = new System.Drawing.Size(78, 13);
            lblPort.TabIndex = 0;
            lblPort.Text = "Port to add:";
            lblPort.Visible = false;
            // 
            // ncPort
            // 
            ncPort.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            ncPort.Enabled = false;
            ncPort.Location = new System.Drawing.Point(111, 7);
            ncPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            ncPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            ncPort.Name = "ncPort";
            ncPort.Size = new System.Drawing.Size(75, 23);
            ncPort.TabIndex = 1;
            ncPort.Value = new decimal(new int[] { 1, 0, 0, 0 });
            ncPort.Visible = false;
            // 
            // chkAutoListen
            // 
            chkAutoListen.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkAutoListen.AutoSize = true;
            chkAutoListen.Location = new System.Drawing.Point(12, 145);
            chkAutoListen.Name = "chkAutoListen";
            chkAutoListen.Size = new System.Drawing.Size(222, 17);
            chkAutoListen.TabIndex = 6;
            chkAutoListen.Text = "Listen for new connections on startup";
            chkAutoListen.UseVisualStyleBackColor = true;
            chkAutoListen.CheckedChanged += chkAutoListen_CheckedChanged;
            // 
            // chkPopup
            // 
            chkPopup.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkPopup.AutoSize = true;
            chkPopup.Location = new System.Drawing.Point(12, 214);
            chkPopup.Name = "chkPopup";
            chkPopup.Size = new System.Drawing.Size(259, 17);
            chkPopup.TabIndex = 7;
            chkPopup.Text = "Show popup notification on new connection";
            chkPopup.UseVisualStyleBackColor = true;
            // 
            // btnListen
            // 
            btnListen.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnListen.Location = new System.Drawing.Point(12, 6);
            btnListen.Name = "btnListen";
            btnListen.Size = new System.Drawing.Size(336, 23);
            btnListen.TabIndex = 2;
            btnListen.Text = "Start listening";
            btnListen.UseVisualStyleBackColor = true;
            btnListen.Click += btnListen_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.Location = new System.Drawing.Point(12, 583);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(121, 23);
            btnCancel.TabIndex = 18;
            btnCancel.Text = "&Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // chkUseUpnp
            // 
            chkUseUpnp.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkUseUpnp.AutoSize = true;
            chkUseUpnp.Location = new System.Drawing.Point(12, 237);
            chkUseUpnp.Name = "chkUseUpnp";
            chkUseUpnp.Size = new System.Drawing.Size(249, 17);
            chkUseUpnp.TabIndex = 8;
            chkUseUpnp.Text = "Try to automatically forward the port (UPnP)";
            chkUseUpnp.UseVisualStyleBackColor = true;
            // 
            // chkShowTooltip
            // 
            chkShowTooltip.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkShowTooltip.AutoSize = true;
            chkShowTooltip.Location = new System.Drawing.Point(12, 260);
            chkShowTooltip.Name = "chkShowTooltip";
            chkShowTooltip.Size = new System.Drawing.Size(268, 17);
            chkShowTooltip.TabIndex = 9;
            chkShowTooltip.Text = "Show tooltip on client with system information";
            chkShowTooltip.UseVisualStyleBackColor = true;
            // 
            // chkIPv6Support
            // 
            chkIPv6Support.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkIPv6Support.AutoSize = true;
            chkIPv6Support.Location = new System.Drawing.Point(12, 168);
            chkIPv6Support.Name = "chkIPv6Support";
            chkIPv6Support.Size = new System.Drawing.Size(128, 17);
            chkIPv6Support.TabIndex = 5;
            chkIPv6Support.Text = "Enable IPv6 support";
            chkIPv6Support.UseVisualStyleBackColor = true;
            // 
            // chkDarkMode
            // 
            chkDarkMode.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkDarkMode.AutoSize = true;
            chkDarkMode.Checked = true;
            chkDarkMode.CheckState = System.Windows.Forms.CheckState.Checked;
            chkDarkMode.Location = new System.Drawing.Point(12, 99);
            chkDarkMode.Name = "chkDarkMode";
            chkDarkMode.Size = new System.Drawing.Size(83, 17);
            chkDarkMode.TabIndex = 20;
            chkDarkMode.Text = "Dark Mode";
            chkDarkMode.UseVisualStyleBackColor = true;
            chkDarkMode.CheckedChanged += chkDarkMode_CheckedChanged;
            // 
            // chkEventLog
            // 
            chkEventLog.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkEventLog.AutoSize = true;
            chkEventLog.Location = new System.Drawing.Point(12, 283);
            chkEventLog.Name = "chkEventLog";
            chkEventLog.Size = new System.Drawing.Size(186, 17);
            chkEventLog.TabIndex = 21;
            chkEventLog.Text = "Show event log and debug log";
            chkEventLog.UseVisualStyleBackColor = true;
            // 
            // chkDiscordRPC
            // 
            chkDiscordRPC.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkDiscordRPC.AutoSize = true;
            chkDiscordRPC.Checked = true;
            chkDiscordRPC.CheckState = System.Windows.Forms.CheckState.Checked;
            chkDiscordRPC.Location = new System.Drawing.Point(12, 122);
            chkDiscordRPC.Name = "chkDiscordRPC";
            chkDiscordRPC.Size = new System.Drawing.Size(88, 17);
            chkDiscordRPC.TabIndex = 22;
            chkDiscordRPC.Text = "Discord RPC";
            chkDiscordRPC.UseVisualStyleBackColor = true;
            chkDiscordRPC.CheckedChanged += chkDiscordRPC_CheckedChanged;
            // 
            // chkTelegramNotis
            // 
            chkTelegramNotis.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkTelegramNotis.AutoSize = true;
            chkTelegramNotis.Location = new System.Drawing.Point(12, 329);
            chkTelegramNotis.Name = "chkTelegramNotis";
            chkTelegramNotis.Size = new System.Drawing.Size(178, 17);
            chkTelegramNotis.TabIndex = 23;
            chkTelegramNotis.Text = "Enable Telegram Notifications";
            chkTelegramNotis.UseVisualStyleBackColor = true;
            chkTelegramNotis.CheckedChanged += chkTelegramNotis_CheckedChanged;
            // 
            // txtTelegramToken
            // 
            txtTelegramToken.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtTelegramToken.Enabled = false;
            txtTelegramToken.Location = new System.Drawing.Point(70, 352);
            txtTelegramToken.Name = "txtTelegramToken";
            txtTelegramToken.Size = new System.Drawing.Size(278, 22);
            txtTelegramToken.TabIndex = 24;
            txtTelegramToken.TextChanged += textBox1_TextChanged;
            // 
            // label1
            // 
            label1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label1.AutoSize = true;
            label1.Enabled = false;
            label1.Location = new System.Drawing.Point(20, 355);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(43, 13);
            label1.TabIndex = 25;
            label1.Text = "Token: ";
            // 
            // txtTelegramChatID
            // 
            txtTelegramChatID.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtTelegramChatID.Enabled = false;
            txtTelegramChatID.Location = new System.Drawing.Point(70, 380);
            txtTelegramChatID.Name = "txtTelegramChatID";
            txtTelegramChatID.Size = new System.Drawing.Size(278, 22);
            txtTelegramChatID.TabIndex = 26;
            // 
            // label2
            // 
            label2.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            label2.AutoSize = true;
            label2.Enabled = false;
            label2.Location = new System.Drawing.Point(19, 383);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(45, 13);
            label2.TabIndex = 27;
            label2.Text = "ChatID:";
            // 
            // button1
            // 
            button1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            button1.Location = new System.Drawing.Point(192, 408);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(156, 23);
            button1.TabIndex = 28;
            button1.Text = "Test";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
            groupBox1.Controls.Add(BlockedRichTB);
            groupBox1.Location = new System.Drawing.Point(39, 432);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(290, 137);
            groupBox1.TabIndex = 30;
            groupBox1.TabStop = false;
            groupBox1.Text = "Blocked IP's";
            // 
            // BlockedRichTB
            // 
            BlockedRichTB.Anchor = System.Windows.Forms.AnchorStyles.None;
            BlockedRichTB.BorderStyle = System.Windows.Forms.BorderStyle.None;
            BlockedRichTB.Location = new System.Drawing.Point(3, 18);
            BlockedRichTB.Name = "BlockedRichTB";
            BlockedRichTB.Size = new System.Drawing.Size(284, 116);
            BlockedRichTB.TabIndex = 0;
            BlockedRichTB.Text = "";
            // 
            // chkHideFromScreenCapture
            // 
            chkHideFromScreenCapture.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkHideFromScreenCapture.AutoSize = true;
            chkHideFromScreenCapture.Location = new System.Drawing.Point(12, 191);
            chkHideFromScreenCapture.Name = "chkHideFromScreenCapture";
            chkHideFromScreenCapture.Size = new System.Drawing.Size(248, 17);
            chkHideFromScreenCapture.TabIndex = 5;
            chkHideFromScreenCapture.Text = "Hide window contents from screen capture";
            chkHideFromScreenCapture.UseVisualStyleBackColor = true;
            chkHideFromScreenCapture.CheckedChanged += hideFromScreenCapture_CheckedChanged;
            // 
            // lblMultiPorts
            // 
            lblMultiPorts.AutoSize = true;
            lblMultiPorts.Location = new System.Drawing.Point(12, 39);
            lblMultiPorts.Name = "lblMultiPorts";
            lblMultiPorts.Size = new System.Drawing.Size(95, 13);
            lblMultiPorts.TabIndex = 20;
            lblMultiPorts.Text = "Ports to listen to:";
            // 
            // txtMultiPorts
            // 
            txtMultiPorts.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtMultiPorts.ForeColor = System.Drawing.Color.Gray;
            txtMultiPorts.Location = new System.Drawing.Point(111, 36);
            txtMultiPorts.Name = "txtMultiPorts";
            txtMultiPorts.Size = new System.Drawing.Size(237, 22);
            txtMultiPorts.TabIndex = 21;
            txtMultiPorts.Text = "port1 port2 etc..";
            txtMultiPorts.Enter += txtMultiPorts_Enter;
            txtMultiPorts.KeyPress += txtMultiPorts_KeyPress;
            txtMultiPorts.Leave += txtMultiPorts_Leave;
            // 
            // chkShowCountryGroups
            // 
            chkShowCountryGroups.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            chkShowCountryGroups.AutoSize = true;
            chkShowCountryGroups.Checked = true;
            chkShowCountryGroups.CheckState = System.Windows.Forms.CheckState.Checked;
            chkShowCountryGroups.Location = new System.Drawing.Point(12, 306);
            chkShowCountryGroups.Name = "chkShowCountryGroups";
            chkShowCountryGroups.Size = new System.Drawing.Size(152, 17);
            chkShowCountryGroups.TabIndex = 10;
            chkShowCountryGroups.Text = "Group clients by country";
            chkShowCountryGroups.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new System.Drawing.Point(12, 69);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(111, 13);
            label3.TabIndex = 31;
            label3.Text = "Language Selection:";
            // 
            // comboLanguages
            // 
            comboLanguages.FormattingEnabled = true;
            comboLanguages.Location = new System.Drawing.Point(127, 66);
            comboLanguages.Name = "comboLanguages";
            comboLanguages.Size = new System.Drawing.Size(221, 21);
            comboLanguages.TabIndex = 32;
            // 
            // FrmSettings
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(360, 610);
            Controls.Add(comboLanguages);
            Controls.Add(label3);
            Controls.Add(txtMultiPorts);
            Controls.Add(lblMultiPorts);
            Controls.Add(chkShowCountryGroups);
            Controls.Add(chkHideFromScreenCapture);
            Controls.Add(groupBox1);
            Controls.Add(button1);
            Controls.Add(label2);
            Controls.Add(txtTelegramChatID);
            Controls.Add(label1);
            Controls.Add(txtTelegramToken);
            Controls.Add(chkTelegramNotis);
            Controls.Add(chkDiscordRPC);
            Controls.Add(chkEventLog);
            Controls.Add(chkDarkMode);
            Controls.Add(chkIPv6Support);
            Controls.Add(chkShowTooltip);
            Controls.Add(chkUseUpnp);
            Controls.Add(btnCancel);
            Controls.Add(btnListen);
            Controls.Add(chkPopup);
            Controls.Add(chkAutoListen);
            Controls.Add(btnSave);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmSettings";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Settings";
            Load += FrmSettings_Load;
            ((System.ComponentModel.ISupportInitialize)ncPort).EndInit();
            groupBox1.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.NumericUpDown ncPort;
        private System.Windows.Forms.CheckBox chkAutoListen;
        private System.Windows.Forms.CheckBox chkPopup;
        private System.Windows.Forms.Button btnListen;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox chkUseUpnp;
        private System.Windows.Forms.CheckBox chkShowTooltip;
        private System.Windows.Forms.CheckBox chkIPv6Support;
        private System.Windows.Forms.CheckBox chkDarkMode;
        private System.Windows.Forms.CheckBox chkEventLog;
        private System.Windows.Forms.CheckBox chkDiscordRPC;
        private System.Windows.Forms.CheckBox chkTelegramNotis;
        private System.Windows.Forms.TextBox txtTelegramToken;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtTelegramChatID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RichTextBox BlockedRichTB;
        private System.Windows.Forms.CheckBox chkHideFromScreenCapture;
        private System.Windows.Forms.Label lblMultiPorts;
        private System.Windows.Forms.TextBox txtMultiPorts;
        private System.Windows.Forms.CheckBox chkShowCountryGroups;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox comboLanguages;
    }
}