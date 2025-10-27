namespace Pulsar.Server.Forms
{
    partial class FrmKeylogger
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmKeylogger));
            this.lstLogs = new System.Windows.Forms.ListView();
            this.hLogs = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.stripLblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnGetLogs = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblSearch = new System.Windows.Forms.Label();
            this.rtbLogViewer = new System.Windows.Forms.RichTextBox();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // lstLogs
            // 
            this.lstLogs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstLogs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.hLogs});
            this.lstLogs.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.lstLogs.FullRowSelect = true;
            this.lstLogs.GridLines = true;
            this.lstLogs.Location = new System.Drawing.Point(0, 84);
            this.lstLogs.Name = "lstLogs";
            this.lstLogs.Size = new System.Drawing.Size(153, 378);
            this.lstLogs.TabIndex = 0;
            this.lstLogs.UseCompatibleStateImageBehavior = false;
            this.lstLogs.View = System.Windows.Forms.View.Details;
            this.lstLogs.ItemActivate += new System.EventHandler(this.lstLogs_ItemActivate);
            // 
            // hLogs
            // 
            this.hLogs.Text = "Logs";
            this.hLogs.Width = 149;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.stripLblStatus});
            this.statusStrip.Location = new System.Drawing.Point(0, 460);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(862, 22);
            this.statusStrip.TabIndex = 6;
            this.statusStrip.Text = "statusStrip1";
            // 
            // stripLblStatus
            // 
            this.stripLblStatus.Name = "stripLblStatus";
            this.stripLblStatus.Size = new System.Drawing.Size(77, 17);
            this.stripLblStatus.Text = "Status: Ready";
            // 
            // btnGetLogs
            // 
            this.btnGetLogs.Location = new System.Drawing.Point(2, 3);
            this.btnGetLogs.Name = "btnGetLogs";
            this.btnGetLogs.Size = new System.Drawing.Size(149, 23);
            this.btnGetLogs.TabIndex = 7;
            this.btnGetLogs.Text = "Get Logs";
            this.btnGetLogs.UseVisualStyleBackColor = true;
            this.btnGetLogs.Click += new System.EventHandler(this.btnGetLogs_Click);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(2, 29);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(149, 23);
            this.btnRefresh.TabIndex = 8;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // txtSearch
            // 
            this.txtSearch.Location = new System.Drawing.Point(44, 58);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(107, 22);
            this.txtSearch.TabIndex = 9;
            this.txtSearch.TextChanged += new System.EventHandler(this.txtSearch_TextChanged);
            // 
            // lblSearch
            // 
            this.lblSearch.AutoSize = true;
            this.lblSearch.Location = new System.Drawing.Point(2, 61);
            this.lblSearch.Name = "lblSearch";
            this.lblSearch.Size = new System.Drawing.Size(42, 13);
            this.lblSearch.TabIndex = 10;
            this.lblSearch.Text = "Search:";
            // 
            // rtbLogViewer
            // 
            this.rtbLogViewer.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.rtbLogViewer.Location = new System.Drawing.Point(154, 3);
            this.rtbLogViewer.Name = "rtbLogViewer";
            this.rtbLogViewer.ReadOnly = true;
            this.rtbLogViewer.Size = new System.Drawing.Size(708, 456);
            this.rtbLogViewer.TabIndex = 11;
            this.rtbLogViewer.Text = "";
            // 
            // FrmKeylogger
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(862, 482);
            this.Controls.Add(this.rtbLogViewer);
            this.Controls.Add(this.lblSearch);
            this.Controls.Add(this.txtSearch);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnGetLogs);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.lstLogs);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(630, 465);
            this.Name = "FrmKeylogger";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Keylogger []";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmKeylogger_FormClosing);
            this.Load += new System.EventHandler(this.FrmKeylogger_Load);
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion        

        private System.Windows.Forms.ColumnHeader hLogs;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.RichTextBox rtbLogViewer;
        private System.Windows.Forms.ListView lstLogs;
        private System.Windows.Forms.Button btnGetLogs;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblSearch;
        private System.Windows.Forms.ToolStripStatusLabel stripLblStatus;
    }
}