using Pulsar.Server.Controls;

namespace Pulsar.Server.Forms
{
    partial class FrmSystemInformation
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmSystemInformation));
            lstSystem = new AeroListView();
            hComponent = new System.Windows.Forms.ColumnHeader();
            hValue = new System.Windows.Forms.ColumnHeader();
            contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            copyToClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copyAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            copySelectedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            toolStripMenuItem2 = new System.Windows.Forms.ToolStripSeparator();
            refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            contextMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // lstSystem
            // 
            lstSystem.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            lstSystem.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { hComponent, hValue });
            lstSystem.ContextMenuStrip = contextMenuStrip;
            lstSystem.FullRowSelect = true;
            lstSystem.Location = new System.Drawing.Point(12, 12);
            lstSystem.Name = "lstSystem";
            lstSystem.Size = new System.Drawing.Size(536, 311);
            lstSystem.TabIndex = 0;
            lstSystem.UseCompatibleStateImageBehavior = false;
            lstSystem.View = System.Windows.Forms.View.Details;
            // 
            // hComponent
            // 
            hComponent.Text = "Component";
            hComponent.Width = 172;
            // 
            // hValue
            // 
            hValue.Text = "Value";
            hValue.Width = 360;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { copyToClipboardToolStripMenuItem, toolStripMenuItem2, refreshToolStripMenuItem });
            contextMenuStrip.Name = "ctxtMenu";
            contextMenuStrip.Size = new System.Drawing.Size(172, 54);
            // 
            // copyToClipboardToolStripMenuItem
            // 
            copyToClipboardToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { copyAllToolStripMenuItem, copySelectedToolStripMenuItem });
            copyToClipboardToolStripMenuItem.Image = Properties.Resources.page_copy;
            copyToClipboardToolStripMenuItem.Name = "copyToClipboardToolStripMenuItem";
            copyToClipboardToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            copyToClipboardToolStripMenuItem.Text = "Copy to Clipboard";
            // 
            // copyAllToolStripMenuItem
            // 
            copyAllToolStripMenuItem.Name = "copyAllToolStripMenuItem";
            copyAllToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            copyAllToolStripMenuItem.Text = "All";
            copyAllToolStripMenuItem.Click += copyAllToolStripMenuItem_Click;
            // 
            // copySelectedToolStripMenuItem
            // 
            copySelectedToolStripMenuItem.Name = "copySelectedToolStripMenuItem";
            copySelectedToolStripMenuItem.Size = new System.Drawing.Size(118, 22);
            copySelectedToolStripMenuItem.Text = "Selected";
            copySelectedToolStripMenuItem.Click += copySelectedToolStripMenuItem_Click;
            // 
            // toolStripMenuItem2
            // 
            toolStripMenuItem2.Name = "toolStripMenuItem2";
            toolStripMenuItem2.Size = new System.Drawing.Size(168, 6);
            // 
            // refreshToolStripMenuItem
            // 
            refreshToolStripMenuItem.Image = Properties.Resources.refresh;
            refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            refreshToolStripMenuItem.Size = new System.Drawing.Size(171, 22);
            refreshToolStripMenuItem.Text = "Refresh";
            refreshToolStripMenuItem.Click += refreshToolStripMenuItem_Click;
            // 
            // FrmSystemInformation
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(560, 335);
            Controls.Add(lstSystem);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(576, 373);
            Name = "FrmSystemInformation";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "System Information []";
            FormClosing += FrmSystemInformation_FormClosing;
            Load += FrmSystemInformation_Load;
            contextMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ColumnHeader hComponent;
        private System.Windows.Forms.ColumnHeader hValue;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem copyToClipboardToolStripMenuItem;
        private AeroListView lstSystem;
        private System.Windows.Forms.ToolStripMenuItem copyAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copySelectedToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
    }
}