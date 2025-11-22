using Pulsar.Server.Controls;

namespace Pulsar.Server.Forms
{
    partial class FrmConnections
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmConnections));
            contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            closeConnectionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            searchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            autorefreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            lstConnections = new AeroListView();
            columnHeader1 = new System.Windows.Forms.ColumnHeader();
            columnHeader2 = new System.Windows.Forms.ColumnHeader();
            columnHeader3 = new System.Windows.Forms.ColumnHeader();
            columnHeader4 = new System.Windows.Forms.ColumnHeader();
            columnHeader5 = new System.Windows.Forms.ColumnHeader();
            columnHeader6 = new System.Windows.Forms.ColumnHeader();
            contextMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { closeConnectionToolStripMenuItem, toolStripSeparator1, searchToolStripMenuItem, refreshToolStripMenuItem, autorefreshToolStripMenuItem });
            contextMenuStrip.Name = "contextMenuStrip";
            contextMenuStrip.Size = new System.Drawing.Size(197, 98);
            // 
            // closeConnectionToolStripMenuItem
            // 
            closeConnectionToolStripMenuItem.Image = Properties.Resources.uac_shield;
            closeConnectionToolStripMenuItem.Name = "closeConnectionToolStripMenuItem";
            closeConnectionToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            closeConnectionToolStripMenuItem.Text = "Close Connection (Del)";
            closeConnectionToolStripMenuItem.Click += closeConnectionToolStripMenuItem_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new System.Drawing.Size(193, 6);
            // 
            // searchToolStripMenuItem
            // 
            searchToolStripMenuItem.Image = Properties.Resources.magnifyingglassicon;
            searchToolStripMenuItem.Name = "searchToolStripMenuItem";
            searchToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            searchToolStripMenuItem.Text = "Search (Ctrl + F)";
            searchToolStripMenuItem.Click += searchToolStripMenuItem_Click;
            // 
            // refreshToolStripMenuItem
            // 
            refreshToolStripMenuItem.Image = Properties.Resources.refresh;
            refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            refreshToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            refreshToolStripMenuItem.Text = "Refresh";
            refreshToolStripMenuItem.Click += refreshToolStripMenuItem_Click;
            // 
            // autorefreshToolStripMenuItem
            // 
            autorefreshToolStripMenuItem.Image = Properties.Resources.autorefreshicon;
            autorefreshToolStripMenuItem.Name = "autorefreshToolStripMenuItem";
            autorefreshToolStripMenuItem.Size = new System.Drawing.Size(196, 22);
            autorefreshToolStripMenuItem.Text = "Autorefresh";
            autorefreshToolStripMenuItem.Click += autorefreshToolStripMenuItem_Click;
            // 
            // lstConnections
            // 
            lstConnections.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4, columnHeader5, columnHeader6 });
            lstConnections.ContextMenuStrip = contextMenuStrip;
            lstConnections.Dock = System.Windows.Forms.DockStyle.Fill;
            lstConnections.FullRowSelect = true;
            lstConnections.Location = new System.Drawing.Point(0, 0);
            lstConnections.Name = "lstConnections";
            lstConnections.Size = new System.Drawing.Size(703, 421);
            lstConnections.TabIndex = 0;
            lstConnections.UseCompatibleStateImageBehavior = false;
            lstConnections.View = System.Windows.Forms.View.Details;
            lstConnections.ColumnClick += lstConnections_ColumnClick;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Process";
            columnHeader1.Width = 179;
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Local Address";
            columnHeader2.Width = 95;
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Local Port";
            columnHeader3.Width = 75;
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "Remote Address";
            columnHeader4.Width = 95;
            // 
            // columnHeader5
            // 
            columnHeader5.Text = "Remote Port";
            columnHeader5.Width = 75;
            // 
            // columnHeader6
            // 
            columnHeader6.Text = "State";
            columnHeader6.Width = 180;
            // 
            // FrmConnections
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(703, 421);
            Controls.Add(lstConnections);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "FrmConnections";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Connections []";
            FormClosing += FrmConnections_FormClosing;
            Load += FrmConnections_Load;
            contextMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Controls.AeroListView lstConnections;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.ColumnHeader columnHeader2;
        private System.Windows.Forms.ColumnHeader columnHeader3;
        private System.Windows.Forms.ColumnHeader columnHeader4;
        private System.Windows.Forms.ColumnHeader columnHeader5;
        private System.Windows.Forms.ColumnHeader columnHeader6;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem closeConnectionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem autorefreshToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem searchToolStripMenuItem;
    }
}