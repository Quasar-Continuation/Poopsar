using Pulsar.Server.Controls;

namespace Pulsar.Server.Forms
{
    partial class FrmBuilder
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

                if (portNotificationTimer != null)
                {
                    portNotificationTimer.Stop();
                    portNotificationTimer.Dispose();
                }

                if (portSetDelayTimer != null)
                {
                    portSetDelayTimer.Stop();
                    portSetDelayTimer.Dispose();
                }

                if (iconPreview?.Image != null)
                {
                    iconPreview.Image.Dispose();
                    iconPreview.Image = null;
                }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmBuilder));
            btnBuild = new System.Windows.Forms.Button();
            tooltip = new System.Windows.Forms.ToolTip(components);
            picUAC2 = new System.Windows.Forms.PictureBox();
            picUAC1 = new System.Windows.Forms.PictureBox();
            rbSystem = new System.Windows.Forms.RadioButton();
            rbProgramFiles = new System.Windows.Forms.RadioButton();
            contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            removeHostToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            builderTabs = new DotNetBarTabControl();
            generalPage = new System.Windows.Forms.TabPage();
            groupBox1 = new System.Windows.Forms.GroupBox();
            checkBox2 = new System.Windows.Forms.CheckBox();
            comboBox1 = new System.Windows.Forms.ComboBox();
            label15 = new System.Windows.Forms.Label();
            chkCryptable = new System.Windows.Forms.CheckBox();
            chkCriticalProcess = new System.Windows.Forms.CheckBox();
            chkUACBypass = new System.Windows.Forms.CheckBox();
            chkAntiDebug = new System.Windows.Forms.CheckBox();
            chkVM = new System.Windows.Forms.CheckBox();
            label2 = new System.Windows.Forms.Label();
            line2 = new Line();
            label3 = new System.Windows.Forms.Label();
            label9 = new System.Windows.Forms.Label();
            line6 = new Line();
            label8 = new System.Windows.Forms.Label();
            txtTag = new System.Windows.Forms.TextBox();
            label7 = new System.Windows.Forms.Label();
            lblTag = new System.Windows.Forms.Label();
            txtMutex = new System.Windows.Forms.TextBox();
            btnMutex = new System.Windows.Forms.Button();
            line5 = new Line();
            lblMutex = new System.Windows.Forms.Label();
            label6 = new System.Windows.Forms.Label();
            connectionPage = new System.Windows.Forms.TabPage();
            line11 = new Line();
            checkBox1 = new System.Windows.Forms.CheckBox();
            label13 = new System.Windows.Forms.Label();
            txtPastebin = new System.Windows.Forms.TextBox();
            numericUpDownPort = new System.Windows.Forms.NumericUpDown();
            numericUpDownDelay = new System.Windows.Forms.NumericUpDown();
            line3 = new Line();
            label4 = new System.Windows.Forms.Label();
            line1 = new Line();
            label1 = new System.Windows.Forms.Label();
            lstHosts = new System.Windows.Forms.ListBox();
            btnAddHost = new System.Windows.Forms.Button();
            lblMS = new System.Windows.Forms.Label();
            lblHost = new System.Windows.Forms.Label();
            txtHost = new System.Windows.Forms.TextBox();
            lblDelay = new System.Windows.Forms.Label();
            lblPort = new System.Windows.Forms.Label();
            installationPage = new System.Windows.Forms.TabPage();
            chkHideSubDirectory = new System.Windows.Forms.CheckBox();
            line7 = new Line();
            label10 = new System.Windows.Forms.Label();
            line4 = new Line();
            label5 = new System.Windows.Forms.Label();
            chkInstall = new System.Windows.Forms.CheckBox();
            lblInstallName = new System.Windows.Forms.Label();
            txtInstallName = new System.Windows.Forms.TextBox();
            txtRegistryKeyName = new System.Windows.Forms.TextBox();
            lblExtension = new System.Windows.Forms.Label();
            lblRegistryKeyName = new System.Windows.Forms.Label();
            chkStartup = new System.Windows.Forms.CheckBox();
            rbAppdata = new System.Windows.Forms.RadioButton();
            chkHide = new System.Windows.Forms.CheckBox();
            lblInstallDirectory = new System.Windows.Forms.Label();
            lblInstallSubDirectory = new System.Windows.Forms.Label();
            lblPreviewPath = new System.Windows.Forms.Label();
            txtInstallSubDirectory = new System.Windows.Forms.TextBox();
            txtPreviewPath = new System.Windows.Forms.TextBox();
            assemblyPage = new System.Windows.Forms.TabPage();
            btnClone = new System.Windows.Forms.Button();
            iconPreview = new System.Windows.Forms.PictureBox();
            btnBrowseIcon = new System.Windows.Forms.Button();
            txtIconPath = new System.Windows.Forms.TextBox();
            line8 = new Line();
            label11 = new System.Windows.Forms.Label();
            chkChangeAsmInfo = new System.Windows.Forms.CheckBox();
            txtFileVersion = new System.Windows.Forms.TextBox();
            line9 = new Line();
            lblProductName = new System.Windows.Forms.Label();
            label12 = new System.Windows.Forms.Label();
            chkChangeIcon = new System.Windows.Forms.CheckBox();
            lblFileVersion = new System.Windows.Forms.Label();
            txtProductName = new System.Windows.Forms.TextBox();
            txtProductVersion = new System.Windows.Forms.TextBox();
            lblDescription = new System.Windows.Forms.Label();
            lblProductVersion = new System.Windows.Forms.Label();
            txtDescription = new System.Windows.Forms.TextBox();
            txtOriginalFilename = new System.Windows.Forms.TextBox();
            lblCompanyName = new System.Windows.Forms.Label();
            lblOriginalFilename = new System.Windows.Forms.Label();
            txtCompanyName = new System.Windows.Forms.TextBox();
            txtTrademarks = new System.Windows.Forms.TextBox();
            lblCopyright = new System.Windows.Forms.Label();
            lblTrademarks = new System.Windows.Forms.Label();
            txtCopyright = new System.Windows.Forms.TextBox();
            monitoringTab = new System.Windows.Forms.TabPage();
            chkHideLogDirectory = new System.Windows.Forms.CheckBox();
            txtLogDirectoryName = new System.Windows.Forms.TextBox();
            lblLogDirectory = new System.Windows.Forms.Label();
            line10 = new Line();
            label14 = new System.Windows.Forms.Label();
            chkKeylogger = new System.Windows.Forms.CheckBox();
            btnShellcode = new System.Windows.Forms.Button();
            button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)picUAC2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picUAC1).BeginInit();
            contextMenuStrip.SuspendLayout();
            builderTabs.SuspendLayout();
            generalPage.SuspendLayout();
            groupBox1.SuspendLayout();
            connectionPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownPort).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownDelay).BeginInit();
            installationPage.SuspendLayout();
            assemblyPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)iconPreview).BeginInit();
            monitoringTab.SuspendLayout();
            SuspendLayout();
            // 
            // btnBuild
            // 
            btnBuild.Location = new System.Drawing.Point(402, 390);
            btnBuild.Name = "btnBuild";
            btnBuild.Size = new System.Drawing.Size(121, 23);
            btnBuild.TabIndex = 1;
            btnBuild.Text = "Build Client";
            btnBuild.UseVisualStyleBackColor = true;
            btnBuild.Click += btnBuild_Click;
            // 
            // picUAC2
            // 
            picUAC2.Image = Properties.Resources.uac_shield;
            picUAC2.Location = new System.Drawing.Point(363, 88);
            picUAC2.Name = "picUAC2";
            picUAC2.Size = new System.Drawing.Size(16, 20);
            picUAC2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            picUAC2.TabIndex = 32;
            picUAC2.TabStop = false;
            tooltip.SetToolTip(picUAC2, "Administrator Privileges are required to install the client in System.");
            // 
            // picUAC1
            // 
            picUAC1.Image = Properties.Resources.uac_shield;
            picUAC1.Location = new System.Drawing.Point(363, 68);
            picUAC1.Name = "picUAC1";
            picUAC1.Size = new System.Drawing.Size(16, 20);
            picUAC1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            picUAC1.TabIndex = 31;
            picUAC1.TabStop = false;
            tooltip.SetToolTip(picUAC1, "Administrator Privileges are required to install the client in Program Files.");
            // 
            // rbSystem
            // 
            rbSystem.AutoSize = true;
            rbSystem.Location = new System.Drawing.Point(241, 91);
            rbSystem.Name = "rbSystem";
            rbSystem.Size = new System.Drawing.Size(60, 17);
            rbSystem.TabIndex = 5;
            rbSystem.TabStop = true;
            rbSystem.Text = "System";
            tooltip.SetToolTip(rbSystem, "Administrator Privileges are required to install the client in System.");
            rbSystem.UseVisualStyleBackColor = true;
            rbSystem.CheckedChanged += HasChangedSettingAndFilePath;
            // 
            // rbProgramFiles
            // 
            rbProgramFiles.AutoSize = true;
            rbProgramFiles.Location = new System.Drawing.Point(241, 68);
            rbProgramFiles.Name = "rbProgramFiles";
            rbProgramFiles.Size = new System.Drawing.Size(94, 17);
            rbProgramFiles.TabIndex = 4;
            rbProgramFiles.TabStop = true;
            rbProgramFiles.Text = "Program Files";
            tooltip.SetToolTip(rbProgramFiles, "Administrator Privileges are required to install the client in Program Files.");
            rbProgramFiles.UseVisualStyleBackColor = true;
            rbProgramFiles.CheckedChanged += HasChangedSettingAndFilePath;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { removeHostToolStripMenuItem, clearToolStripMenuItem });
            contextMenuStrip.Name = "ctxtMenuHosts";
            contextMenuStrip.Size = new System.Drawing.Size(144, 48);
            // 
            // removeHostToolStripMenuItem
            // 
            removeHostToolStripMenuItem.Image = Properties.Resources.delete;
            removeHostToolStripMenuItem.Name = "removeHostToolStripMenuItem";
            removeHostToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            removeHostToolStripMenuItem.Text = "Remove host";
            removeHostToolStripMenuItem.Click += removeHostToolStripMenuItem_Click;
            // 
            // clearToolStripMenuItem
            // 
            clearToolStripMenuItem.Image = Properties.Resources.broom;
            clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            clearToolStripMenuItem.Size = new System.Drawing.Size(143, 22);
            clearToolStripMenuItem.Text = "Clear all";
            clearToolStripMenuItem.Click += clearToolStripMenuItem_Click;
            // 
            // builderTabs
            // 
            builderTabs.Alignment = System.Windows.Forms.TabAlignment.Left;
            builderTabs.Controls.Add(generalPage);
            builderTabs.Controls.Add(connectionPage);
            builderTabs.Controls.Add(installationPage);
            builderTabs.Controls.Add(assemblyPage);
            builderTabs.Controls.Add(monitoringTab);
            builderTabs.Dock = System.Windows.Forms.DockStyle.Top;
            builderTabs.ItemSize = new System.Drawing.Size(44, 136);
            builderTabs.Location = new System.Drawing.Point(0, 0);
            builderTabs.Multiline = true;
            builderTabs.Name = "builderTabs";
            builderTabs.SelectedIndex = 0;
            builderTabs.Size = new System.Drawing.Size(535, 384);
            builderTabs.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            builderTabs.TabIndex = 0;
            builderTabs.SelectedIndexChanged += builderTabs_SelectedIndexChanged;
            // 
            // generalPage
            // 
            generalPage.BackColor = System.Drawing.SystemColors.Control;
            generalPage.Controls.Add(groupBox1);
            generalPage.Controls.Add(chkCryptable);
            generalPage.Controls.Add(chkCriticalProcess);
            generalPage.Controls.Add(chkUACBypass);
            generalPage.Controls.Add(chkAntiDebug);
            generalPage.Controls.Add(chkVM);
            generalPage.Controls.Add(label2);
            generalPage.Controls.Add(line2);
            generalPage.Controls.Add(label3);
            generalPage.Controls.Add(label9);
            generalPage.Controls.Add(line6);
            generalPage.Controls.Add(label8);
            generalPage.Controls.Add(txtTag);
            generalPage.Controls.Add(label7);
            generalPage.Controls.Add(lblTag);
            generalPage.Controls.Add(txtMutex);
            generalPage.Controls.Add(btnMutex);
            generalPage.Controls.Add(line5);
            generalPage.Controls.Add(lblMutex);
            generalPage.Controls.Add(label6);
            generalPage.Location = new System.Drawing.Point(140, 4);
            generalPage.Name = "generalPage";
            generalPage.Padding = new System.Windows.Forms.Padding(3);
            generalPage.Size = new System.Drawing.Size(391, 376);
            generalPage.TabIndex = 4;
            generalPage.Text = "Basic Settings";
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(checkBox2);
            groupBox1.Controls.Add(comboBox1);
            groupBox1.Controls.Add(label15);
            groupBox1.Location = new System.Drawing.Point(6, 321);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(379, 49);
            groupBox1.TabIndex = 31;
            groupBox1.TabStop = false;
            groupBox1.Text = "Donut Shellcode Settings";
            groupBox1.Visible = false;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new System.Drawing.Point(269, 23);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new System.Drawing.Size(90, 17);
            checkBox2.TabIndex = 2;
            checkBox2.Text = "AMSI Bypass";
            checkBox2.UseVisualStyleBackColor = true;
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Items.AddRange(new object[] { "None", "aPLib", "LZNT1", "Xpress" });
            comboBox1.Location = new System.Drawing.Point(93, 21);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new System.Drawing.Size(170, 21);
            comboBox1.TabIndex = 1;
            // 
            // label15
            // 
            label15.AutoSize = true;
            label15.Location = new System.Drawing.Point(10, 24);
            label15.Name = "label15";
            label15.Size = new System.Drawing.Size(77, 13);
            label15.TabIndex = 0;
            label15.Text = "Compression:";
            // 
            // chkCryptable
            // 
            chkCryptable.AutoSize = true;
            chkCryptable.Location = new System.Drawing.Point(20, 246);
            chkCryptable.Name = "chkCryptable";
            chkCryptable.Size = new System.Drawing.Size(207, 17);
            chkCryptable.TabIndex = 30;
            chkCryptable.Text = "Cryptable / Ready to run in memory";
            chkCryptable.UseVisualStyleBackColor = true;
            // 
            // chkCriticalProcess
            // 
            chkCriticalProcess.AutoSize = true;
            chkCriticalProcess.Location = new System.Drawing.Point(271, 224);
            chkCriticalProcess.Name = "chkCriticalProcess";
            chkCriticalProcess.Size = new System.Drawing.Size(102, 17);
            chkCriticalProcess.TabIndex = 29;
            chkCriticalProcess.Text = "Critical Process";
            chkCriticalProcess.UseVisualStyleBackColor = true;
            chkCriticalProcess.CheckedChanged += chkCriticalProcess_CheckedChanged;
            // 
            // chkUACBypass
            // 
            chkUACBypass.AutoSize = true;
            chkUACBypass.Location = new System.Drawing.Point(183, 223);
            chkUACBypass.Name = "chkUACBypass";
            chkUACBypass.Size = new System.Drawing.Size(86, 17);
            chkUACBypass.TabIndex = 28;
            chkUACBypass.Text = "UAC Bypass";
            chkUACBypass.UseVisualStyleBackColor = true;
            chkUACBypass.CheckedChanged += chkUACBypass_CheckedChanged;
            // 
            // chkAntiDebug
            // 
            chkAntiDebug.AutoSize = true;
            chkAntiDebug.Location = new System.Drawing.Point(93, 222);
            chkAntiDebug.Name = "chkAntiDebug";
            chkAntiDebug.Size = new System.Drawing.Size(85, 17);
            chkAntiDebug.TabIndex = 25;
            chkAntiDebug.Text = "Anti Debug";
            chkAntiDebug.UseVisualStyleBackColor = true;
            chkAntiDebug.CheckedChanged += chkAntiDebug_CheckedChanged;
            // 
            // chkVM
            // 
            chkVM.AutoSize = true;
            chkVM.Location = new System.Drawing.Point(20, 222);
            chkVM.Name = "chkVM";
            chkVM.Size = new System.Drawing.Size(67, 17);
            chkVM.TabIndex = 24;
            chkVM.Text = "Anti VM";
            chkVM.UseVisualStyleBackColor = true;
            chkVM.CheckedChanged += chkUACBypass_CheckedChanged;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.BackColor = System.Drawing.Color.Transparent;
            label2.Location = new System.Drawing.Point(17, 203);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(299, 26);
            label2.TabIndex = 22;
            label2.Text = "Checks you want to run when something runs the client.\r\n\r\n";
            // 
            // line2
            // 
            line2.BackColor = System.Drawing.SystemColors.Control;
            line2.Location = new System.Drawing.Point(85, 187);
            line2.Name = "line2";
            line2.Size = new System.Drawing.Size(300, 13);
            line2.TabIndex = 23;
            line2.TabStop = false;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.BackColor = System.Drawing.Color.Transparent;
            label3.Location = new System.Drawing.Point(6, 187);
            label3.Name = "label3";
            label3.Size = new System.Drawing.Size(77, 13);
            label3.TabIndex = 21;
            label3.Text = "Anti Methods";
            // 
            // label9
            // 
            label9.AutoSize = true;
            label9.BackColor = System.Drawing.Color.Transparent;
            label9.Location = new System.Drawing.Point(17, 94);
            label9.Name = "label9";
            label9.Size = new System.Drawing.Size(370, 26);
            label9.TabIndex = 5;
            label9.Text = "A unique mutex ensures that only one instance of the client is running\r\non the same system.";
            // 
            // line6
            // 
            line6.BackColor = System.Drawing.SystemColors.Control;
            line6.Location = new System.Drawing.Point(85, 78);
            line6.Name = "line6";
            line6.Size = new System.Drawing.Size(300, 13);
            line6.TabIndex = 20;
            line6.TabStop = false;
            // 
            // label8
            // 
            label8.AutoSize = true;
            label8.BackColor = System.Drawing.Color.Transparent;
            label8.Location = new System.Drawing.Point(6, 78);
            label8.Name = "label8";
            label8.Size = new System.Drawing.Size(80, 13);
            label8.TabIndex = 4;
            label8.Text = "Process Mutex";
            // 
            // txtTag
            // 
            txtTag.Location = new System.Drawing.Point(130, 40);
            txtTag.Name = "txtTag";
            txtTag.Size = new System.Drawing.Size(255, 22);
            txtTag.TabIndex = 3;
            txtTag.TextChanged += HasChangedSetting;
            // 
            // label7
            // 
            label7.AutoSize = true;
            label7.Location = new System.Drawing.Point(17, 20);
            label7.Name = "label7";
            label7.Size = new System.Drawing.Size(231, 13);
            label7.TabIndex = 1;
            label7.Text = "You can choose a tag to identify your client.";
            // 
            // lblTag
            // 
            lblTag.AutoSize = true;
            lblTag.Location = new System.Drawing.Point(17, 43);
            lblTag.Name = "lblTag";
            lblTag.Size = new System.Drawing.Size(60, 13);
            lblTag.TabIndex = 2;
            lblTag.Text = "Client Tag:";
            // 
            // txtMutex
            // 
            txtMutex.Location = new System.Drawing.Point(130, 130);
            txtMutex.MaxLength = 64;
            txtMutex.Name = "txtMutex";
            txtMutex.Size = new System.Drawing.Size(253, 22);
            txtMutex.TabIndex = 7;
            txtMutex.TextChanged += HasChangedSetting;
            // 
            // btnMutex
            // 
            btnMutex.BackColor = System.Drawing.Color.Transparent;
            btnMutex.Location = new System.Drawing.Point(262, 158);
            btnMutex.Name = "btnMutex";
            btnMutex.Size = new System.Drawing.Size(121, 23);
            btnMutex.TabIndex = 8;
            btnMutex.Text = "Random Mutex";
            btnMutex.UseVisualStyleBackColor = false;
            btnMutex.Click += btnMutex_Click;
            // 
            // line5
            // 
            line5.BackColor = System.Drawing.SystemColors.Control;
            line5.Location = new System.Drawing.Point(112, 5);
            line5.Name = "line5";
            line5.Size = new System.Drawing.Size(271, 13);
            line5.TabIndex = 15;
            line5.TabStop = false;
            // 
            // lblMutex
            // 
            lblMutex.AutoSize = true;
            lblMutex.BackColor = System.Drawing.Color.Transparent;
            lblMutex.Location = new System.Drawing.Point(17, 133);
            lblMutex.Name = "lblMutex";
            lblMutex.Size = new System.Drawing.Size(42, 13);
            lblMutex.TabIndex = 6;
            lblMutex.Text = "Mutex:";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new System.Drawing.Point(6, 5);
            label6.Name = "label6";
            label6.Size = new System.Drawing.Size(109, 13);
            label6.TabIndex = 0;
            label6.Text = "Client Identification";
            // 
            // connectionPage
            // 
            connectionPage.BackColor = System.Drawing.SystemColors.Control;
            connectionPage.Controls.Add(line11);
            connectionPage.Controls.Add(checkBox1);
            connectionPage.Controls.Add(label13);
            connectionPage.Controls.Add(txtPastebin);
            connectionPage.Controls.Add(numericUpDownPort);
            connectionPage.Controls.Add(numericUpDownDelay);
            connectionPage.Controls.Add(line3);
            connectionPage.Controls.Add(label4);
            connectionPage.Controls.Add(line1);
            connectionPage.Controls.Add(label1);
            connectionPage.Controls.Add(lstHosts);
            connectionPage.Controls.Add(btnAddHost);
            connectionPage.Controls.Add(lblMS);
            connectionPage.Controls.Add(lblHost);
            connectionPage.Controls.Add(txtHost);
            connectionPage.Controls.Add(lblDelay);
            connectionPage.Controls.Add(lblPort);
            connectionPage.Location = new System.Drawing.Point(140, 4);
            connectionPage.Name = "connectionPage";
            connectionPage.Padding = new System.Windows.Forms.Padding(3);
            connectionPage.Size = new System.Drawing.Size(391, 376);
            connectionPage.TabIndex = 0;
            connectionPage.Text = "Connection Settings";
            connectionPage.Click += connectionPage_Click;
            // 
            // line11
            // 
            line11.BackColor = System.Drawing.SystemColors.Control;
            line11.Location = new System.Drawing.Point(175, 107);
            line11.Name = "line11";
            line11.Size = new System.Drawing.Size(216, 19);
            line11.TabIndex = 22;
            line11.TabStop = false;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new System.Drawing.Point(370, 129);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new System.Drawing.Size(15, 14);
            checkBox1.TabIndex = 21;
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // label13
            // 
            label13.AutoSize = true;
            label13.Location = new System.Drawing.Point(175, 129);
            label13.Name = "label13";
            label13.Size = new System.Drawing.Size(60, 13);
            label13.TabIndex = 20;
            label13.Text = "Pastebin : ";
            // 
            // txtPastebin
            // 
            txtPastebin.Location = new System.Drawing.Point(254, 126);
            txtPastebin.Name = "txtPastebin";
            txtPastebin.Size = new System.Drawing.Size(110, 22);
            txtPastebin.TabIndex = 19;
            txtPastebin.TextChanged += txtPastebin_TextChanged;
            // 
            // numericUpDownPort
            // 
            numericUpDownPort.Location = new System.Drawing.Point(254, 51);
            numericUpDownPort.Maximum = new decimal(new int[] { 65535, 0, 0, 0 });
            numericUpDownPort.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numericUpDownPort.Name = "numericUpDownPort";
            numericUpDownPort.Size = new System.Drawing.Size(129, 22);
            numericUpDownPort.TabIndex = 3;
            numericUpDownPort.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // numericUpDownDelay
            // 
            numericUpDownDelay.Location = new System.Drawing.Point(274, 223);
            numericUpDownDelay.Maximum = new decimal(new int[] { 600000, 0, 0, 0 });
            numericUpDownDelay.Name = "numericUpDownDelay";
            numericUpDownDelay.Size = new System.Drawing.Size(80, 22);
            numericUpDownDelay.TabIndex = 10;
            numericUpDownDelay.Value = new decimal(new int[] { 5000, 0, 0, 0 });
            numericUpDownDelay.ValueChanged += HasChangedSetting;
            // 
            // line3
            // 
            line3.BackColor = System.Drawing.SystemColors.Control;
            line3.Location = new System.Drawing.Point(104, 199);
            line3.Name = "line3";
            line3.Size = new System.Drawing.Size(290, 13);
            line3.TabIndex = 18;
            line3.TabStop = false;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new System.Drawing.Point(6, 199);
            label4.Name = "label4";
            label4.Size = new System.Drawing.Size(92, 13);
            label4.TabIndex = 17;
            label4.Text = "Reconnect Delay";
            // 
            // line1
            // 
            line1.BackColor = System.Drawing.SystemColors.Control;
            line1.Location = new System.Drawing.Point(104, 5);
            line1.Name = "line1";
            line1.Size = new System.Drawing.Size(281, 13);
            line1.TabIndex = 13;
            line1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(6, 5);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(99, 13);
            label1.TabIndex = 14;
            label1.Text = "Connection Hosts";
            // 
            // lstHosts
            // 
            lstHosts.ContextMenuStrip = contextMenuStrip;
            lstHosts.FormattingEnabled = true;
            lstHosts.HorizontalScrollbar = true;
            lstHosts.Location = new System.Drawing.Point(20, 21);
            lstHosts.Name = "lstHosts";
            lstHosts.Size = new System.Drawing.Size(149, 121);
            lstHosts.TabIndex = 5;
            lstHosts.TabStop = false;
            // 
            // btnAddHost
            // 
            btnAddHost.Location = new System.Drawing.Point(254, 79);
            btnAddHost.Name = "btnAddHost";
            btnAddHost.Size = new System.Drawing.Size(129, 22);
            btnAddHost.TabIndex = 4;
            btnAddHost.Text = "Add Host";
            btnAddHost.UseVisualStyleBackColor = true;
            btnAddHost.Click += btnAddHost_Click;
            // 
            // lblMS
            // 
            lblMS.AutoSize = true;
            lblMS.Location = new System.Drawing.Point(360, 225);
            lblMS.Name = "lblMS";
            lblMS.Size = new System.Drawing.Size(21, 13);
            lblMS.TabIndex = 11;
            lblMS.Text = "ms";
            // 
            // lblHost
            // 
            lblHost.AutoSize = true;
            lblHost.Location = new System.Drawing.Point(175, 25);
            lblHost.Name = "lblHost";
            lblHost.Size = new System.Drawing.Size(75, 13);
            lblHost.TabIndex = 0;
            lblHost.Text = "IP/Hostname:";
            // 
            // txtHost
            // 
            txtHost.Location = new System.Drawing.Point(254, 22);
            txtHost.Name = "txtHost";
            txtHost.Size = new System.Drawing.Size(129, 22);
            txtHost.TabIndex = 1;
            txtHost.TextChanged += txtHost_TextChanged_1;
            // 
            // lblDelay
            // 
            lblDelay.AutoSize = true;
            lblDelay.Location = new System.Drawing.Point(6, 225);
            lblDelay.Name = "lblDelay";
            lblDelay.Size = new System.Drawing.Size(199, 13);
            lblDelay.TabIndex = 9;
            lblDelay.Text = "Time to wait between reconnect tries:";
            // 
            // lblPort
            // 
            lblPort.AutoSize = true;
            lblPort.Location = new System.Drawing.Point(175, 53);
            lblPort.Name = "lblPort";
            lblPort.Size = new System.Drawing.Size(31, 13);
            lblPort.TabIndex = 2;
            lblPort.Text = "Port:";
            // 
            // installationPage
            // 
            installationPage.BackColor = System.Drawing.SystemColors.Control;
            installationPage.Controls.Add(chkHideSubDirectory);
            installationPage.Controls.Add(line7);
            installationPage.Controls.Add(label10);
            installationPage.Controls.Add(line4);
            installationPage.Controls.Add(label5);
            installationPage.Controls.Add(picUAC2);
            installationPage.Controls.Add(picUAC1);
            installationPage.Controls.Add(chkInstall);
            installationPage.Controls.Add(rbSystem);
            installationPage.Controls.Add(lblInstallName);
            installationPage.Controls.Add(rbProgramFiles);
            installationPage.Controls.Add(txtInstallName);
            installationPage.Controls.Add(txtRegistryKeyName);
            installationPage.Controls.Add(lblExtension);
            installationPage.Controls.Add(lblRegistryKeyName);
            installationPage.Controls.Add(chkStartup);
            installationPage.Controls.Add(rbAppdata);
            installationPage.Controls.Add(chkHide);
            installationPage.Controls.Add(lblInstallDirectory);
            installationPage.Controls.Add(lblInstallSubDirectory);
            installationPage.Controls.Add(lblPreviewPath);
            installationPage.Controls.Add(txtInstallSubDirectory);
            installationPage.Controls.Add(txtPreviewPath);
            installationPage.Location = new System.Drawing.Point(140, 4);
            installationPage.Name = "installationPage";
            installationPage.Padding = new System.Windows.Forms.Padding(3);
            installationPage.Size = new System.Drawing.Size(391, 376);
            installationPage.TabIndex = 1;
            installationPage.Text = "Installation Settings";
            // 
            // chkHideSubDirectory
            // 
            chkHideSubDirectory.AutoSize = true;
            chkHideSubDirectory.Location = new System.Drawing.Point(186, 185);
            chkHideSubDirectory.Name = "chkHideSubDirectory";
            chkHideSubDirectory.Size = new System.Drawing.Size(185, 17);
            chkHideSubDirectory.TabIndex = 37;
            chkHideSubDirectory.Text = "Set subdir attributes to hidden";
            chkHideSubDirectory.UseVisualStyleBackColor = true;
            // 
            // line7
            // 
            line7.BackColor = System.Drawing.SystemColors.Control;
            line7.Location = new System.Drawing.Point(60, 274);
            line7.Name = "line7";
            line7.Size = new System.Drawing.Size(323, 13);
            line7.TabIndex = 36;
            line7.TabStop = false;
            // 
            // label10
            // 
            label10.AutoSize = true;
            label10.Location = new System.Drawing.Point(7, 274);
            label10.Name = "label10";
            label10.Size = new System.Drawing.Size(55, 13);
            label10.TabIndex = 14;
            label10.Text = "Autostart";
            // 
            // line4
            // 
            line4.BackColor = System.Drawing.SystemColors.Control;
            line4.Location = new System.Drawing.Point(117, 5);
            line4.Name = "line4";
            line4.Size = new System.Drawing.Size(266, 13);
            line4.TabIndex = 34;
            line4.TabStop = false;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new System.Drawing.Point(6, 5);
            label5.Name = "label5";
            label5.Size = new System.Drawing.Size(112, 13);
            label5.TabIndex = 0;
            label5.Text = "Installation Location";
            // 
            // chkInstall
            // 
            chkInstall.AutoSize = true;
            chkInstall.Location = new System.Drawing.Point(20, 21);
            chkInstall.Name = "chkInstall";
            chkInstall.Size = new System.Drawing.Size(90, 17);
            chkInstall.TabIndex = 1;
            chkInstall.Text = "Install Client";
            chkInstall.UseVisualStyleBackColor = true;
            chkInstall.CheckedChanged += chkInstall_CheckedChanged;
            // 
            // lblInstallName
            // 
            lblInstallName.AutoSize = true;
            lblInstallName.Location = new System.Drawing.Point(17, 156);
            lblInstallName.Name = "lblInstallName";
            lblInstallName.Size = new System.Drawing.Size(73, 13);
            lblInstallName.TabIndex = 8;
            lblInstallName.Text = "Install Name:";
            // 
            // txtInstallName
            // 
            txtInstallName.Location = new System.Drawing.Point(182, 153);
            txtInstallName.Name = "txtInstallName";
            txtInstallName.Size = new System.Drawing.Size(170, 22);
            txtInstallName.TabIndex = 9;
            txtInstallName.TextChanged += HasChangedSettingAndFilePath;
            txtInstallName.KeyPress += txtInstallname_KeyPress;
            // 
            // txtRegistryKeyName
            // 
            txtRegistryKeyName.Location = new System.Drawing.Point(182, 324);
            txtRegistryKeyName.Name = "txtRegistryKeyName";
            txtRegistryKeyName.Size = new System.Drawing.Size(201, 22);
            txtRegistryKeyName.TabIndex = 17;
            txtRegistryKeyName.TextChanged += HasChangedSetting;
            // 
            // lblExtension
            // 
            lblExtension.AutoSize = true;
            lblExtension.Location = new System.Drawing.Point(352, 159);
            lblExtension.Name = "lblExtension";
            lblExtension.Size = new System.Drawing.Size(27, 13);
            lblExtension.TabIndex = 10;
            lblExtension.Text = ".exe";
            // 
            // lblRegistryKeyName
            // 
            lblRegistryKeyName.AutoSize = true;
            lblRegistryKeyName.Location = new System.Drawing.Point(17, 327);
            lblRegistryKeyName.Name = "lblRegistryKeyName";
            lblRegistryKeyName.Size = new System.Drawing.Size(80, 13);
            lblRegistryKeyName.TabIndex = 16;
            lblRegistryKeyName.Text = "Startup Name:";
            // 
            // chkStartup
            // 
            chkStartup.AutoSize = true;
            chkStartup.Location = new System.Drawing.Point(20, 298);
            chkStartup.Name = "chkStartup";
            chkStartup.Size = new System.Drawing.Size(215, 17);
            chkStartup.TabIndex = 15;
            chkStartup.Text = "Run Client when the computer starts";
            chkStartup.UseVisualStyleBackColor = true;
            chkStartup.CheckedChanged += chkStartup_CheckedChanged;
            // 
            // rbAppdata
            // 
            rbAppdata.AutoSize = true;
            rbAppdata.Checked = true;
            rbAppdata.Location = new System.Drawing.Point(241, 45);
            rbAppdata.Name = "rbAppdata";
            rbAppdata.Size = new System.Drawing.Size(137, 17);
            rbAppdata.TabIndex = 3;
            rbAppdata.TabStop = true;
            rbAppdata.Text = "User Application Data";
            rbAppdata.UseVisualStyleBackColor = true;
            rbAppdata.CheckedChanged += HasChangedSettingAndFilePath;
            // 
            // chkHide
            // 
            chkHide.AutoSize = true;
            chkHide.Location = new System.Drawing.Point(20, 185);
            chkHide.Name = "chkHide";
            chkHide.Size = new System.Drawing.Size(168, 17);
            chkHide.TabIndex = 11;
            chkHide.Text = "Set file attributes to hidden";
            chkHide.UseVisualStyleBackColor = true;
            chkHide.CheckedChanged += HasChangedSetting;
            // 
            // lblInstallDirectory
            // 
            lblInstallDirectory.AutoSize = true;
            lblInstallDirectory.Location = new System.Drawing.Point(17, 47);
            lblInstallDirectory.Name = "lblInstallDirectory";
            lblInstallDirectory.Size = new System.Drawing.Size(90, 13);
            lblInstallDirectory.TabIndex = 2;
            lblInstallDirectory.Text = "Install Directory:";
            // 
            // lblInstallSubDirectory
            // 
            lblInstallSubDirectory.AutoSize = true;
            lblInstallSubDirectory.Location = new System.Drawing.Point(17, 126);
            lblInstallSubDirectory.Name = "lblInstallSubDirectory";
            lblInstallSubDirectory.Size = new System.Drawing.Size(109, 13);
            lblInstallSubDirectory.TabIndex = 6;
            lblInstallSubDirectory.Text = "Install Subdirectory:";
            // 
            // lblPreviewPath
            // 
            lblPreviewPath.AutoSize = true;
            lblPreviewPath.Location = new System.Drawing.Point(17, 218);
            lblPreviewPath.Name = "lblPreviewPath";
            lblPreviewPath.Size = new System.Drawing.Size(157, 13);
            lblPreviewPath.TabIndex = 12;
            lblPreviewPath.Text = "Installation Location Preview:";
            // 
            // txtInstallSubDirectory
            // 
            txtInstallSubDirectory.Location = new System.Drawing.Point(182, 123);
            txtInstallSubDirectory.Name = "txtInstallSubDirectory";
            txtInstallSubDirectory.Size = new System.Drawing.Size(201, 22);
            txtInstallSubDirectory.TabIndex = 7;
            txtInstallSubDirectory.TextChanged += HasChangedSettingAndFilePath;
            txtInstallSubDirectory.KeyPress += txtInstallsub_KeyPress;
            // 
            // txtPreviewPath
            // 
            txtPreviewPath.Location = new System.Drawing.Point(20, 234);
            txtPreviewPath.Name = "txtPreviewPath";
            txtPreviewPath.ReadOnly = true;
            txtPreviewPath.Size = new System.Drawing.Size(363, 22);
            txtPreviewPath.TabIndex = 13;
            txtPreviewPath.TabStop = false;
            // 
            // assemblyPage
            // 
            assemblyPage.BackColor = System.Drawing.SystemColors.Control;
            assemblyPage.Controls.Add(btnClone);
            assemblyPage.Controls.Add(iconPreview);
            assemblyPage.Controls.Add(btnBrowseIcon);
            assemblyPage.Controls.Add(txtIconPath);
            assemblyPage.Controls.Add(line8);
            assemblyPage.Controls.Add(label11);
            assemblyPage.Controls.Add(chkChangeAsmInfo);
            assemblyPage.Controls.Add(txtFileVersion);
            assemblyPage.Controls.Add(line9);
            assemblyPage.Controls.Add(lblProductName);
            assemblyPage.Controls.Add(label12);
            assemblyPage.Controls.Add(chkChangeIcon);
            assemblyPage.Controls.Add(lblFileVersion);
            assemblyPage.Controls.Add(txtProductName);
            assemblyPage.Controls.Add(txtProductVersion);
            assemblyPage.Controls.Add(lblDescription);
            assemblyPage.Controls.Add(lblProductVersion);
            assemblyPage.Controls.Add(txtDescription);
            assemblyPage.Controls.Add(txtOriginalFilename);
            assemblyPage.Controls.Add(lblCompanyName);
            assemblyPage.Controls.Add(lblOriginalFilename);
            assemblyPage.Controls.Add(txtCompanyName);
            assemblyPage.Controls.Add(txtTrademarks);
            assemblyPage.Controls.Add(lblCopyright);
            assemblyPage.Controls.Add(lblTrademarks);
            assemblyPage.Controls.Add(txtCopyright);
            assemblyPage.Location = new System.Drawing.Point(140, 4);
            assemblyPage.Name = "assemblyPage";
            assemblyPage.Size = new System.Drawing.Size(391, 376);
            assemblyPage.TabIndex = 2;
            assemblyPage.Text = "Assembly Settings";
            // 
            // btnClone
            // 
            btnClone.Location = new System.Drawing.Point(9, 343);
            btnClone.Name = "btnClone";
            btnClone.Size = new System.Drawing.Size(152, 23);
            btnClone.TabIndex = 43;
            btnClone.Text = "Copy Assembly from Exe";
            btnClone.UseVisualStyleBackColor = true;
            btnClone.Click += btnClone_Click;
            // 
            // iconPreview
            // 
            iconPreview.BackColor = System.Drawing.Color.Transparent;
            iconPreview.Location = new System.Drawing.Point(319, 302);
            iconPreview.Name = "iconPreview";
            iconPreview.Size = new System.Drawing.Size(64, 64);
            iconPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            iconPreview.TabIndex = 42;
            iconPreview.TabStop = false;
            // 
            // btnBrowseIcon
            // 
            btnBrowseIcon.Location = new System.Drawing.Point(177, 343);
            btnBrowseIcon.Name = "btnBrowseIcon";
            btnBrowseIcon.Size = new System.Drawing.Size(125, 23);
            btnBrowseIcon.TabIndex = 41;
            btnBrowseIcon.Text = "Browse...";
            btnBrowseIcon.UseVisualStyleBackColor = true;
            btnBrowseIcon.Click += btnBrowseIcon_Click;
            // 
            // txtIconPath
            // 
            txtIconPath.Location = new System.Drawing.Point(20, 315);
            txtIconPath.Name = "txtIconPath";
            txtIconPath.Size = new System.Drawing.Size(282, 22);
            txtIconPath.TabIndex = 39;
            // 
            // line8
            // 
            line8.BackColor = System.Drawing.SystemColors.Control;
            line8.Location = new System.Drawing.Point(122, 5);
            line8.Name = "line8";
            line8.Size = new System.Drawing.Size(261, 13);
            line8.TabIndex = 36;
            line8.TabStop = false;
            // 
            // label11
            // 
            label11.AutoSize = true;
            label11.Location = new System.Drawing.Point(6, 5);
            label11.Name = "label11";
            label11.Size = new System.Drawing.Size(118, 13);
            label11.TabIndex = 35;
            label11.Text = "Assembly Information";
            // 
            // chkChangeAsmInfo
            // 
            chkChangeAsmInfo.AutoSize = true;
            chkChangeAsmInfo.Location = new System.Drawing.Point(20, 21);
            chkChangeAsmInfo.Name = "chkChangeAsmInfo";
            chkChangeAsmInfo.Size = new System.Drawing.Size(180, 17);
            chkChangeAsmInfo.TabIndex = 0;
            chkChangeAsmInfo.Text = "Change Assembly Information";
            chkChangeAsmInfo.UseVisualStyleBackColor = true;
            chkChangeAsmInfo.CheckedChanged += chkChangeAsmInfo_CheckedChanged;
            // 
            // txtFileVersion
            // 
            txtFileVersion.Location = new System.Drawing.Point(182, 240);
            txtFileVersion.Name = "txtFileVersion";
            txtFileVersion.Size = new System.Drawing.Size(201, 22);
            txtFileVersion.TabIndex = 16;
            txtFileVersion.TextChanged += HasChangedSetting;
            // 
            // line9
            // 
            line9.BackColor = System.Drawing.SystemColors.Control;
            line9.Location = new System.Drawing.Point(83, 276);
            line9.Name = "line9";
            line9.Size = new System.Drawing.Size(300, 13);
            line9.TabIndex = 38;
            line9.TabStop = false;
            // 
            // lblProductName
            // 
            lblProductName.AutoSize = true;
            lblProductName.Location = new System.Drawing.Point(17, 47);
            lblProductName.Name = "lblProductName";
            lblProductName.Size = new System.Drawing.Size(82, 13);
            lblProductName.TabIndex = 1;
            lblProductName.Text = "Product Name:";
            // 
            // label12
            // 
            label12.AutoSize = true;
            label12.Location = new System.Drawing.Point(6, 276);
            label12.Name = "label12";
            label12.Size = new System.Drawing.Size(79, 13);
            label12.TabIndex = 0;
            label12.Text = "Assembly Icon";
            // 
            // chkChangeIcon
            // 
            chkChangeIcon.AutoSize = true;
            chkChangeIcon.Location = new System.Drawing.Point(20, 294);
            chkChangeIcon.Name = "chkChangeIcon";
            chkChangeIcon.Size = new System.Drawing.Size(141, 17);
            chkChangeIcon.TabIndex = 2;
            chkChangeIcon.Text = "Change Assembly Icon";
            chkChangeIcon.UseVisualStyleBackColor = true;
            chkChangeIcon.CheckedChanged += chkChangeIcon_CheckedChanged;
            // 
            // lblFileVersion
            // 
            lblFileVersion.AutoSize = true;
            lblFileVersion.Location = new System.Drawing.Point(17, 243);
            lblFileVersion.Name = "lblFileVersion";
            lblFileVersion.Size = new System.Drawing.Size(69, 13);
            lblFileVersion.TabIndex = 15;
            lblFileVersion.Text = "File Version:";
            // 
            // txtProductName
            // 
            txtProductName.Location = new System.Drawing.Point(182, 44);
            txtProductName.Name = "txtProductName";
            txtProductName.Size = new System.Drawing.Size(201, 22);
            txtProductName.TabIndex = 2;
            txtProductName.TextChanged += HasChangedSetting;
            // 
            // txtProductVersion
            // 
            txtProductVersion.Location = new System.Drawing.Point(182, 212);
            txtProductVersion.Name = "txtProductVersion";
            txtProductVersion.Size = new System.Drawing.Size(201, 22);
            txtProductVersion.TabIndex = 14;
            txtProductVersion.TextChanged += HasChangedSetting;
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Location = new System.Drawing.Point(17, 75);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new System.Drawing.Size(69, 13);
            lblDescription.TabIndex = 3;
            lblDescription.Text = "Description:";
            // 
            // lblProductVersion
            // 
            lblProductVersion.AutoSize = true;
            lblProductVersion.Location = new System.Drawing.Point(17, 215);
            lblProductVersion.Name = "lblProductVersion";
            lblProductVersion.Size = new System.Drawing.Size(91, 13);
            lblProductVersion.TabIndex = 13;
            lblProductVersion.Text = "Product Version:";
            // 
            // txtDescription
            // 
            txtDescription.Location = new System.Drawing.Point(182, 72);
            txtDescription.Name = "txtDescription";
            txtDescription.Size = new System.Drawing.Size(201, 22);
            txtDescription.TabIndex = 4;
            txtDescription.TextChanged += HasChangedSetting;
            // 
            // txtOriginalFilename
            // 
            txtOriginalFilename.Location = new System.Drawing.Point(182, 184);
            txtOriginalFilename.Name = "txtOriginalFilename";
            txtOriginalFilename.Size = new System.Drawing.Size(201, 22);
            txtOriginalFilename.TabIndex = 12;
            txtOriginalFilename.TextChanged += HasChangedSetting;
            // 
            // lblCompanyName
            // 
            lblCompanyName.AutoSize = true;
            lblCompanyName.Location = new System.Drawing.Point(17, 103);
            lblCompanyName.Name = "lblCompanyName";
            lblCompanyName.Size = new System.Drawing.Size(90, 13);
            lblCompanyName.TabIndex = 5;
            lblCompanyName.Text = "Company Name:";
            // 
            // lblOriginalFilename
            // 
            lblOriginalFilename.AutoSize = true;
            lblOriginalFilename.Location = new System.Drawing.Point(17, 187);
            lblOriginalFilename.Name = "lblOriginalFilename";
            lblOriginalFilename.Size = new System.Drawing.Size(101, 13);
            lblOriginalFilename.TabIndex = 11;
            lblOriginalFilename.Text = "Original Filename:";
            // 
            // txtCompanyName
            // 
            txtCompanyName.Location = new System.Drawing.Point(182, 100);
            txtCompanyName.Name = "txtCompanyName";
            txtCompanyName.Size = new System.Drawing.Size(201, 22);
            txtCompanyName.TabIndex = 6;
            txtCompanyName.TextChanged += HasChangedSetting;
            // 
            // txtTrademarks
            // 
            txtTrademarks.Location = new System.Drawing.Point(182, 156);
            txtTrademarks.Name = "txtTrademarks";
            txtTrademarks.Size = new System.Drawing.Size(201, 22);
            txtTrademarks.TabIndex = 10;
            txtTrademarks.TextChanged += HasChangedSetting;
            // 
            // lblCopyright
            // 
            lblCopyright.AutoSize = true;
            lblCopyright.Location = new System.Drawing.Point(17, 131);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.Size = new System.Drawing.Size(61, 13);
            lblCopyright.TabIndex = 7;
            lblCopyright.Text = "Copyright:";
            // 
            // lblTrademarks
            // 
            lblTrademarks.AutoSize = true;
            lblTrademarks.Location = new System.Drawing.Point(17, 159);
            lblTrademarks.Name = "lblTrademarks";
            lblTrademarks.Size = new System.Drawing.Size(67, 13);
            lblTrademarks.TabIndex = 9;
            lblTrademarks.Text = "Trademarks:";
            // 
            // txtCopyright
            // 
            txtCopyright.Location = new System.Drawing.Point(182, 128);
            txtCopyright.Name = "txtCopyright";
            txtCopyright.Size = new System.Drawing.Size(201, 22);
            txtCopyright.TabIndex = 8;
            txtCopyright.TextChanged += HasChangedSetting;
            // 
            // monitoringTab
            // 
            monitoringTab.BackColor = System.Drawing.SystemColors.Control;
            monitoringTab.Controls.Add(chkHideLogDirectory);
            monitoringTab.Controls.Add(txtLogDirectoryName);
            monitoringTab.Controls.Add(lblLogDirectory);
            monitoringTab.Controls.Add(line10);
            monitoringTab.Controls.Add(label14);
            monitoringTab.Controls.Add(chkKeylogger);
            monitoringTab.Location = new System.Drawing.Point(140, 4);
            monitoringTab.Name = "monitoringTab";
            monitoringTab.Size = new System.Drawing.Size(391, 376);
            monitoringTab.TabIndex = 3;
            monitoringTab.Text = "Monitoring Settings";
            // 
            // chkHideLogDirectory
            // 
            chkHideLogDirectory.AutoSize = true;
            chkHideLogDirectory.Location = new System.Drawing.Point(20, 72);
            chkHideLogDirectory.Name = "chkHideLogDirectory";
            chkHideLogDirectory.Size = new System.Drawing.Size(197, 17);
            chkHideLogDirectory.TabIndex = 7;
            chkHideLogDirectory.Text = "Set directory attributes to hidden";
            chkHideLogDirectory.UseVisualStyleBackColor = true;
            chkHideLogDirectory.CheckedChanged += HasChangedSetting;
            // 
            // txtLogDirectoryName
            // 
            txtLogDirectoryName.Location = new System.Drawing.Point(262, 44);
            txtLogDirectoryName.Name = "txtLogDirectoryName";
            txtLogDirectoryName.Size = new System.Drawing.Size(118, 22);
            txtLogDirectoryName.TabIndex = 6;
            txtLogDirectoryName.TextChanged += HasChangedSetting;
            txtLogDirectoryName.KeyPress += txtLogDirectoryName_KeyPress;
            // 
            // lblLogDirectory
            // 
            lblLogDirectory.AutoSize = true;
            lblLogDirectory.Location = new System.Drawing.Point(17, 47);
            lblLogDirectory.Name = "lblLogDirectory";
            lblLogDirectory.Size = new System.Drawing.Size(110, 13);
            lblLogDirectory.TabIndex = 5;
            lblLogDirectory.Text = "Log Directory Name:";
            // 
            // line10
            // 
            line10.BackColor = System.Drawing.SystemColors.Control;
            line10.Location = new System.Drawing.Point(78, 5);
            line10.Name = "line10";
            line10.Size = new System.Drawing.Size(302, 13);
            line10.TabIndex = 41;
            line10.TabStop = false;
            // 
            // label14
            // 
            label14.AutoSize = true;
            label14.Location = new System.Drawing.Point(6, 5);
            label14.Name = "label14";
            label14.Size = new System.Drawing.Size(66, 13);
            label14.TabIndex = 3;
            label14.Text = "Monitoring";
            // 
            // chkKeylogger
            // 
            chkKeylogger.AutoSize = true;
            chkKeylogger.Location = new System.Drawing.Point(20, 21);
            chkKeylogger.Name = "chkKeylogger";
            chkKeylogger.Size = new System.Drawing.Size(156, 17);
            chkKeylogger.TabIndex = 4;
            chkKeylogger.Text = "Enable keyboard logging";
            chkKeylogger.UseVisualStyleBackColor = true;
            chkKeylogger.CheckedChanged += chkKeylogger_CheckedChanged;
            // 
            // btnShellcode
            // 
            btnShellcode.Location = new System.Drawing.Point(275, 390);
            btnShellcode.Name = "btnShellcode";
            btnShellcode.Size = new System.Drawing.Size(121, 23);
            btnShellcode.TabIndex = 2;
            btnShellcode.Text = "Build Shellcode";
            btnShellcode.UseVisualStyleBackColor = true;
            btnShellcode.Click += btnShellcode_Click;
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(253, 390);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(19, 23);
            button1.TabIndex = 3;
            button1.Text = "?";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // FrmBuilder
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            BackColor = System.Drawing.SystemColors.Control;
            ClientSize = new System.Drawing.Size(535, 424);
            Controls.Add(button1);
            Controls.Add(btnShellcode);
            Controls.Add(builderTabs);
            Controls.Add(btnBuild);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmBuilder";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Client Builder";
            FormClosing += FrmBuilder_FormClosing;
            Load += FrmBuilder_Load;
            ((System.ComponentModel.ISupportInitialize)picUAC2).EndInit();
            ((System.ComponentModel.ISupportInitialize)picUAC1).EndInit();
            contextMenuStrip.ResumeLayout(false);
            builderTabs.ResumeLayout(false);
            generalPage.ResumeLayout(false);
            generalPage.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            connectionPage.ResumeLayout(false);
            connectionPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)numericUpDownPort).EndInit();
            ((System.ComponentModel.ISupportInitialize)numericUpDownDelay).EndInit();
            installationPage.ResumeLayout(false);
            installationPage.PerformLayout();
            assemblyPage.ResumeLayout(false);
            assemblyPage.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)iconPreview).EndInit();
            monitoringTab.ResumeLayout(false);
            monitoringTab.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private System.Windows.Forms.Label lblPort;
        private System.Windows.Forms.TextBox txtHost;
        private System.Windows.Forms.Label lblHost;
        private System.Windows.Forms.Label lblDelay;
        private System.Windows.Forms.CheckBox chkInstall;
        private System.Windows.Forms.TextBox txtInstallName;
        private System.Windows.Forms.Label lblInstallName;
        private System.Windows.Forms.TextBox txtMutex;
        private System.Windows.Forms.Label lblMutex;
        private System.Windows.Forms.Label lblExtension;
        private System.Windows.Forms.Label lblInstallDirectory;
        private System.Windows.Forms.RadioButton rbAppdata;
        private System.Windows.Forms.TextBox txtInstallSubDirectory;
        private System.Windows.Forms.Label lblInstallSubDirectory;
        private System.Windows.Forms.Label lblPreviewPath;
        private System.Windows.Forms.TextBox txtPreviewPath;
        private System.Windows.Forms.Button btnMutex;
        private System.Windows.Forms.CheckBox chkHide;
        private System.Windows.Forms.TextBox txtRegistryKeyName;
        private System.Windows.Forms.Label lblRegistryKeyName;
        private System.Windows.Forms.CheckBox chkStartup;
        private System.Windows.Forms.Button btnBuild;
        private System.Windows.Forms.Label lblMS;
        private System.Windows.Forms.RadioButton rbSystem;
        private System.Windows.Forms.RadioButton rbProgramFiles;
        private System.Windows.Forms.PictureBox picUAC1;
        private System.Windows.Forms.PictureBox picUAC2;
        private System.Windows.Forms.ToolTip tooltip;
        private System.Windows.Forms.CheckBox chkChangeIcon;
        private System.Windows.Forms.TextBox txtDescription;
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.TextBox txtProductName;
        private System.Windows.Forms.Label lblProductName;
        private System.Windows.Forms.TextBox txtOriginalFilename;
        private System.Windows.Forms.Label lblOriginalFilename;
        private System.Windows.Forms.TextBox txtTrademarks;
        private System.Windows.Forms.Label lblTrademarks;
        private System.Windows.Forms.TextBox txtCopyright;
        private System.Windows.Forms.Label lblCopyright;
        private System.Windows.Forms.TextBox txtCompanyName;
        private System.Windows.Forms.Label lblCompanyName;
        private System.Windows.Forms.TextBox txtFileVersion;
        private System.Windows.Forms.Label lblFileVersion;
        private System.Windows.Forms.TextBox txtProductVersion;
        private System.Windows.Forms.Label lblProductVersion;
        private System.Windows.Forms.CheckBox chkChangeAsmInfo;
        private System.Windows.Forms.CheckBox chkKeylogger;
        private Controls.DotNetBarTabControl builderTabs;
        private System.Windows.Forms.TabPage connectionPage;
        private System.Windows.Forms.TabPage installationPage;
        private System.Windows.Forms.TabPage assemblyPage;
        private System.Windows.Forms.TabPage monitoringTab;
        private System.Windows.Forms.ListBox lstHosts;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.Button btnAddHost;
        private System.Windows.Forms.ToolStripMenuItem removeHostToolStripMenuItem;
        private Controls.Line line1;
        private System.Windows.Forms.Label label1;
        private Controls.Line line3;
        private System.Windows.Forms.Label label4;
        private Controls.Line line4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TabPage generalPage;
        private System.Windows.Forms.TextBox txtTag;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label lblTag;
        private Controls.Line line5;
        private System.Windows.Forms.Label label6;
        private Controls.Line line6;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label9;
        private Controls.Line line7;
        private System.Windows.Forms.Label label10;
        private Controls.Line line8;
        private System.Windows.Forms.Label label11;
        private Controls.Line line9;
        private System.Windows.Forms.Label label12;
        private Controls.Line line10;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.Button btnBrowseIcon;
        private System.Windows.Forms.TextBox txtIconPath;
        private System.Windows.Forms.PictureBox iconPreview;
        private System.Windows.Forms.Label lblLogDirectory;
        private System.Windows.Forms.TextBox txtLogDirectoryName;
        private System.Windows.Forms.CheckBox chkHideLogDirectory;
        private System.Windows.Forms.NumericUpDown numericUpDownDelay;
        private System.Windows.Forms.NumericUpDown numericUpDownPort;
        private System.Windows.Forms.CheckBox chkHideSubDirectory;
        private System.Windows.Forms.Label label2;
        private Line line2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox chkAntiDebug;
        private System.Windows.Forms.CheckBox chkVM;
        private Line line11;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox txtPastebin;
        private System.Windows.Forms.CheckBox chkUACBypass;
        private System.Windows.Forms.CheckBox chkCriticalProcess;
        private System.Windows.Forms.Button btnShellcode;
        private System.Windows.Forms.Button btnClone;
        private System.Windows.Forms.CheckBox chkCryptable;
        private System.Windows.Forms.GroupBox groupBox1;
        public System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label15;
        public System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.Button button1;
    }
}
