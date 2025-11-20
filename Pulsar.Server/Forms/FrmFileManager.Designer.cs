using Pulsar.Server.Controls;

namespace Pulsar.Server.Forms
{
    partial class FrmFileManager
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmFileManager));
            contextMenuStripDirectory = new System.Windows.Forms.ContextMenuStrip(components);
            downloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            uploadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            lineToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            zipFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            executeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            renameToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            line2ToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            addToStartupToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            line3ToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openDirectoryInShellToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            imgListDirectory = new System.Windows.Forms.ImageList(components);
            statusStrip = new System.Windows.Forms.StatusStrip();
            stripLblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            contextMenuStripTransfers = new System.Windows.Forms.ContextMenuStrip(components);
            cancelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            executeFileOnServerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            imgListTransfers = new System.Windows.Forms.ImageList(components);
            TabControlFileManager = new DotNetBarTabControl();
            tabFileExplorer = new System.Windows.Forms.TabPage();
            btnRefresh = new System.Windows.Forms.Button();
            lblPath = new System.Windows.Forms.Label();
            txtPath = new System.Windows.Forms.TextBox();
            lstDirectory = new AeroListView();
            hName = new System.Windows.Forms.ColumnHeader();
            hSize = new System.Windows.Forms.ColumnHeader();
            hType = new System.Windows.Forms.ColumnHeader();
            lblDrive = new System.Windows.Forms.Label();
            cmbDrives = new System.Windows.Forms.ComboBox();
            tabTransfers = new System.Windows.Forms.TabPage();
            btnOpenDLFolder = new System.Windows.Forms.Button();
            lstTransfers = new AeroListView();
            hID = new System.Windows.Forms.ColumnHeader();
            hTransferType = new System.Windows.Forms.ColumnHeader();
            hStatus = new System.Windows.Forms.ColumnHeader();
            hFilename = new System.Windows.Forms.ColumnHeader();
            contextMenuStripDirectory.SuspendLayout();
            statusStrip.SuspendLayout();
            contextMenuStripTransfers.SuspendLayout();
            TabControlFileManager.SuspendLayout();
            tabFileExplorer.SuspendLayout();
            tabTransfers.SuspendLayout();
            SuspendLayout();
            // 
            // contextMenuStripDirectory
            // 
            contextMenuStripDirectory.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { downloadToolStripMenuItem, uploadToolStripMenuItem, lineToolStripMenuItem, zipFolderToolStripMenuItem, executeToolStripMenuItem, renameToolStripMenuItem, deleteToolStripMenuItem, line2ToolStripMenuItem, addToStartupToolStripMenuItem, line3ToolStripMenuItem, refreshToolStripMenuItem, openDirectoryInShellToolStripMenuItem });
            contextMenuStripDirectory.Name = "ctxtMenu";
            contextMenuStripDirectory.Size = new System.Drawing.Size(240, 220);
            // 
            // downloadToolStripMenuItem
            // 
            downloadToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            downloadToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("downloadToolStripMenuItem.Image");
            downloadToolStripMenuItem.Name = "downloadToolStripMenuItem";
            downloadToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            downloadToolStripMenuItem.Text = "Download";
            downloadToolStripMenuItem.Click += downloadToolStripMenuItem_Click;
            // 
            // uploadToolStripMenuItem
            // 
            uploadToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("uploadToolStripMenuItem.Image");
            uploadToolStripMenuItem.Name = "uploadToolStripMenuItem";
            uploadToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            uploadToolStripMenuItem.Text = "Upload";
            uploadToolStripMenuItem.Click += uploadToolStripMenuItem_Click;
            // 
            // lineToolStripMenuItem
            // 
            lineToolStripMenuItem.Name = "lineToolStripMenuItem";
            lineToolStripMenuItem.Size = new System.Drawing.Size(236, 6);
            // 
            // zipFolderToolStripMenuItem
            // 
            zipFolderToolStripMenuItem.Image = Properties.Resources.zipfolder;
            zipFolderToolStripMenuItem.Name = "zipFolderToolStripMenuItem";
            zipFolderToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            zipFolderToolStripMenuItem.Text = "Zip Folder";
            zipFolderToolStripMenuItem.Click += zipFolderToolStripMenuItem_Click;
            // 
            // executeToolStripMenuItem
            // 
            executeToolStripMenuItem.Image = (System.Drawing.Image)resources.GetObject("executeToolStripMenuItem.Image");
            executeToolStripMenuItem.Name = "executeToolStripMenuItem";
            executeToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            executeToolStripMenuItem.Text = "Execute";
            executeToolStripMenuItem.Click += executeToolStripMenuItem_Click;
            // 
            // renameToolStripMenuItem
            // 
            renameToolStripMenuItem.Image = Properties.Resources.textfield_rename;
            renameToolStripMenuItem.Name = "renameToolStripMenuItem";
            renameToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            renameToolStripMenuItem.Text = "Rename";
            renameToolStripMenuItem.Click += renameToolStripMenuItem_Click;
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Image = Properties.Resources.delete;
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            deleteToolStripMenuItem.Text = "Delete";
            deleteToolStripMenuItem.Click += deleteToolStripMenuItem_Click;
            // 
            // line2ToolStripMenuItem
            // 
            line2ToolStripMenuItem.Name = "line2ToolStripMenuItem";
            line2ToolStripMenuItem.Size = new System.Drawing.Size(236, 6);
            // 
            // addToStartupToolStripMenuItem
            // 
            addToStartupToolStripMenuItem.Image = Properties.Resources.application_add;
            addToStartupToolStripMenuItem.Name = "addToStartupToolStripMenuItem";
            addToStartupToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            addToStartupToolStripMenuItem.Text = "Add to Startup";
            addToStartupToolStripMenuItem.Click += addToStartupToolStripMenuItem_Click;
            // 
            // line3ToolStripMenuItem
            // 
            line3ToolStripMenuItem.Name = "line3ToolStripMenuItem";
            line3ToolStripMenuItem.Size = new System.Drawing.Size(236, 6);
            // 
            // refreshToolStripMenuItem
            // 
            refreshToolStripMenuItem.Image = Properties.Resources.refresh;
            refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            refreshToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            refreshToolStripMenuItem.Text = "Refresh";
            refreshToolStripMenuItem.Click += refreshToolStripMenuItem_Click;
            // 
            // openDirectoryInShellToolStripMenuItem
            // 
            openDirectoryInShellToolStripMenuItem.Image = Properties.Resources.terminal;
            openDirectoryInShellToolStripMenuItem.Name = "openDirectoryInShellToolStripMenuItem";
            openDirectoryInShellToolStripMenuItem.Size = new System.Drawing.Size(239, 22);
            openDirectoryInShellToolStripMenuItem.Text = "Open Directory in Remote Shell";
            openDirectoryInShellToolStripMenuItem.Click += openDirectoryToolStripMenuItem_Click;
            // 
            // imgListDirectory
            // 
            imgListDirectory.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            imgListDirectory.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("imgListDirectory.ImageStream");
            imgListDirectory.TransparentColor = System.Drawing.Color.Transparent;
            imgListDirectory.Images.SetKeyName(0, "back.png");
            imgListDirectory.Images.SetKeyName(1, "folder.png");
            imgListDirectory.Images.SetKeyName(2, "file.png");
            imgListDirectory.Images.SetKeyName(3, "application.png");
            imgListDirectory.Images.SetKeyName(4, "text.png");
            imgListDirectory.Images.SetKeyName(5, "archive.png");
            imgListDirectory.Images.SetKeyName(6, "word.png");
            imgListDirectory.Images.SetKeyName(7, "pdf.png");
            imgListDirectory.Images.SetKeyName(8, "image.png");
            imgListDirectory.Images.SetKeyName(9, "movie.png");
            imgListDirectory.Images.SetKeyName(10, "music.png");
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { stripLblStatus });
            statusStrip.Location = new System.Drawing.Point(0, 456);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new System.Drawing.Size(858, 22);
            statusStrip.TabIndex = 3;
            statusStrip.Text = "statusStrip1";
            // 
            // stripLblStatus
            // 
            stripLblStatus.Name = "stripLblStatus";
            stripLblStatus.Size = new System.Drawing.Size(131, 17);
            stripLblStatus.Text = "Status: Loading drives...";
            // 
            // contextMenuStripTransfers
            // 
            contextMenuStripTransfers.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { cancelToolStripMenuItem, toolStripMenuItem1, clearToolStripMenuItem, executeFileOnServerToolStripMenuItem });
            contextMenuStripTransfers.Name = "ctxtMenu2";
            contextMenuStripTransfers.Size = new System.Drawing.Size(206, 76);
            // 
            // cancelToolStripMenuItem
            // 
            cancelToolStripMenuItem.Image = Properties.Resources.cancel;
            cancelToolStripMenuItem.Name = "cancelToolStripMenuItem";
            cancelToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            cancelToolStripMenuItem.Text = "Cancel";
            cancelToolStripMenuItem.Click += cancelToolStripMenuItem_Click;
            // 
            // toolStripMenuItem1
            // 
            toolStripMenuItem1.Name = "toolStripMenuItem1";
            toolStripMenuItem1.Size = new System.Drawing.Size(202, 6);
            // 
            // clearToolStripMenuItem
            // 
            clearToolStripMenuItem.Image = Properties.Resources.broom;
            clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            clearToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            clearToolStripMenuItem.Text = "Clear transfers";
            clearToolStripMenuItem.Click += clearToolStripMenuItem_Click;
            // 
            // executeFileOnServerToolStripMenuItem
            // 
            executeFileOnServerToolStripMenuItem.Image = Properties.Resources.application_go;
            executeFileOnServerToolStripMenuItem.Name = "executeFileOnServerToolStripMenuItem";
            executeFileOnServerToolStripMenuItem.Size = new System.Drawing.Size(205, 22);
            executeFileOnServerToolStripMenuItem.Text = "Execute Downloaded File";
            executeFileOnServerToolStripMenuItem.Click += executeFileOnServerToolStripMenuItem_Click;
            // 
            // imgListTransfers
            // 
            imgListTransfers.ColorDepth = System.Windows.Forms.ColorDepth.Depth8Bit;
            imgListTransfers.ImageStream = (System.Windows.Forms.ImageListStreamer)resources.GetObject("imgListTransfers.ImageStream");
            imgListTransfers.TransparentColor = System.Drawing.Color.Transparent;
            imgListTransfers.Images.SetKeyName(0, "cancel.png");
            imgListTransfers.Images.SetKeyName(1, "done.png");
            // 
            // TabControlFileManager
            // 
            TabControlFileManager.Alignment = System.Windows.Forms.TabAlignment.Left;
            TabControlFileManager.Controls.Add(tabFileExplorer);
            TabControlFileManager.Controls.Add(tabTransfers);
            TabControlFileManager.Dock = System.Windows.Forms.DockStyle.Fill;
            TabControlFileManager.ItemSize = new System.Drawing.Size(44, 136);
            TabControlFileManager.Location = new System.Drawing.Point(0, 0);
            TabControlFileManager.Multiline = true;
            TabControlFileManager.Name = "TabControlFileManager";
            TabControlFileManager.SelectedIndex = 0;
            TabControlFileManager.Size = new System.Drawing.Size(858, 456);
            TabControlFileManager.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            TabControlFileManager.TabIndex = 5;
            // 
            // tabFileExplorer
            // 
            tabFileExplorer.BackColor = System.Drawing.SystemColors.Control;
            tabFileExplorer.Controls.Add(btnRefresh);
            tabFileExplorer.Controls.Add(lblPath);
            tabFileExplorer.Controls.Add(txtPath);
            tabFileExplorer.Controls.Add(lstDirectory);
            tabFileExplorer.Controls.Add(lblDrive);
            tabFileExplorer.Controls.Add(cmbDrives);
            tabFileExplorer.Location = new System.Drawing.Point(140, 4);
            tabFileExplorer.Name = "tabFileExplorer";
            tabFileExplorer.Padding = new System.Windows.Forms.Padding(3);
            tabFileExplorer.Size = new System.Drawing.Size(714, 448);
            tabFileExplorer.TabIndex = 0;
            tabFileExplorer.Text = "File Explorer";
            // 
            // btnRefresh
            // 
            btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Popup;
            btnRefresh.Image = Properties.Resources.refresh;
            btnRefresh.ImageAlign = System.Drawing.ContentAlignment.BottomCenter;
            btnRefresh.Location = new System.Drawing.Point(682, 8);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new System.Drawing.Size(22, 22);
            btnRefresh.TabIndex = 5;
            btnRefresh.UseVisualStyleBackColor = true;
            btnRefresh.Click += btnRefresh_Click;
            // 
            // lblPath
            // 
            lblPath.AutoSize = true;
            lblPath.Location = new System.Drawing.Point(279, 12);
            lblPath.Name = "lblPath";
            lblPath.Size = new System.Drawing.Size(75, 13);
            lblPath.TabIndex = 4;
            lblPath.Text = "Remote Path:";
            // 
            // txtPath
            // 
            txtPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtPath.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            txtPath.Location = new System.Drawing.Point(360, 8);
            txtPath.Name = "txtPath";
            txtPath.Size = new System.Drawing.Size(323, 22);
            txtPath.TabIndex = 3;
            txtPath.Text = "\\";
            // 
            // lstDirectory
            // 
            lstDirectory.AllowDrop = true;
            lstDirectory.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lstDirectory.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { hName, hSize, hType });
            lstDirectory.ContextMenuStrip = contextMenuStripDirectory;
            lstDirectory.FullRowSelect = true;
            lstDirectory.Location = new System.Drawing.Point(8, 35);
            lstDirectory.Name = "lstDirectory";
            lstDirectory.Size = new System.Drawing.Size(700, 406);
            lstDirectory.SmallImageList = imgListDirectory;
            lstDirectory.TabIndex = 2;
            lstDirectory.UseCompatibleStateImageBehavior = false;
            lstDirectory.View = System.Windows.Forms.View.Details;
            lstDirectory.ColumnClick += lstDirectory_ColumnClick;
            lstDirectory.DragDrop += lstDirectory_DragDrop;
            lstDirectory.DragEnter += lstDirectory_DragEnter;
            lstDirectory.DoubleClick += lstDirectory_DoubleClick;
            // 
            // hName
            // 
            hName.Text = "Name";
            hName.Width = 360;
            // 
            // hSize
            // 
            hSize.Text = "Size";
            hSize.Width = 125;
            // 
            // hType
            // 
            hType.Text = "Type";
            hType.Width = 211;
            // 
            // lblDrive
            // 
            lblDrive.AutoSize = true;
            lblDrive.Location = new System.Drawing.Point(8, 12);
            lblDrive.Name = "lblDrive";
            lblDrive.Size = new System.Drawing.Size(36, 13);
            lblDrive.TabIndex = 0;
            lblDrive.Text = "Drive:";
            // 
            // cmbDrives
            // 
            cmbDrives.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbDrives.FormattingEnabled = true;
            cmbDrives.Location = new System.Drawing.Point(50, 8);
            cmbDrives.Name = "cmbDrives";
            cmbDrives.Size = new System.Drawing.Size(212, 21);
            cmbDrives.TabIndex = 1;
            cmbDrives.SelectedIndexChanged += cmbDrives_SelectedIndexChanged;
            // 
            // tabTransfers
            // 
            tabTransfers.BackColor = System.Drawing.SystemColors.Control;
            tabTransfers.Controls.Add(btnOpenDLFolder);
            tabTransfers.Controls.Add(lstTransfers);
            tabTransfers.Location = new System.Drawing.Point(140, 4);
            tabTransfers.Name = "tabTransfers";
            tabTransfers.Padding = new System.Windows.Forms.Padding(3);
            tabTransfers.Size = new System.Drawing.Size(714, 448);
            tabTransfers.TabIndex = 1;
            tabTransfers.Text = "Transfers";
            // 
            // btnOpenDLFolder
            // 
            btnOpenDLFolder.Location = new System.Drawing.Point(8, 2);
            btnOpenDLFolder.Name = "btnOpenDLFolder";
            btnOpenDLFolder.Size = new System.Drawing.Size(145, 22);
            btnOpenDLFolder.TabIndex = 0;
            btnOpenDLFolder.Text = "&Open Download Folder";
            btnOpenDLFolder.UseVisualStyleBackColor = true;
            btnOpenDLFolder.Click += btnOpenDLFolder_Click;
            // 
            // lstTransfers
            // 
            lstTransfers.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lstTransfers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { hID, hTransferType, hStatus, hFilename });
            lstTransfers.ContextMenuStrip = contextMenuStripTransfers;
            lstTransfers.FullRowSelect = true;
            lstTransfers.Location = new System.Drawing.Point(8, 35);
            lstTransfers.Name = "lstTransfers";
            lstTransfers.Size = new System.Drawing.Size(698, 407);
            lstTransfers.SmallImageList = imgListTransfers;
            lstTransfers.TabIndex = 1;
            lstTransfers.UseCompatibleStateImageBehavior = false;
            lstTransfers.View = System.Windows.Forms.View.Details;
            // 
            // hID
            // 
            hID.Text = "ID";
            hID.Width = 128;
            // 
            // hTransferType
            // 
            hTransferType.Text = "Transfer Type";
            hTransferType.Width = 93;
            // 
            // hStatus
            // 
            hStatus.Text = "Status";
            hStatus.Width = 173;
            // 
            // hFilename
            // 
            hFilename.Text = "Filename";
            hFilename.Width = 300;
            // 
            // FrmFileManager
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(858, 478);
            Controls.Add(TabControlFileManager);
            Controls.Add(statusStrip);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            MinimumSize = new System.Drawing.Size(663, 377);
            Name = "FrmFileManager";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "File Manager []";
            FormClosing += FrmFileManager_FormClosing;
            Load += FrmFileManager_Load;
            KeyDown += FrmFileManager_KeyDown;
            contextMenuStripDirectory.ResumeLayout(false);
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            contextMenuStripTransfers.ResumeLayout(false);
            TabControlFileManager.ResumeLayout(false);
            tabFileExplorer.ResumeLayout(false);
            tabFileExplorer.PerformLayout();
            tabTransfers.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblDrive;
        private System.Windows.Forms.ImageList imgListDirectory;
        private System.Windows.Forms.ColumnHeader hName;
        private System.Windows.Forms.ColumnHeader hSize;
        private System.Windows.Forms.ColumnHeader hType;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripDirectory;
        private System.Windows.Forms.ToolStripMenuItem downloadToolStripMenuItem;
        private System.Windows.Forms.Button btnOpenDLFolder;
        private DotNetBarTabControl TabControlFileManager;
        private System.Windows.Forms.TabPage tabFileExplorer;
        private System.Windows.Forms.TabPage tabTransfers;
        private System.Windows.Forms.ColumnHeader hStatus;
        private System.Windows.Forms.ColumnHeader hFilename;
        private System.Windows.Forms.ColumnHeader hID;
        private System.Windows.Forms.ImageList imgListTransfers;
        private System.Windows.Forms.ToolStripMenuItem executeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator line3ToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator lineToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem renameToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator line2ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem addToStartupToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripTransfers;
        private System.Windows.Forms.ToolStripMenuItem cancelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDirectoryInShellToolStripMenuItem;
        private System.Windows.Forms.ComboBox cmbDrives;
        private AeroListView lstDirectory;
        private AeroListView lstTransfers;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel stripLblStatus;
        private System.Windows.Forms.Label lblPath;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.ToolStripMenuItem uploadToolStripMenuItem;
        private System.Windows.Forms.ColumnHeader hTransferType;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem zipFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem executeFileOnServerToolStripMenuItem;
    }
}