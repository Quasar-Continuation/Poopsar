using System.Windows.Forms.Integration;

namespace Pulsar.Server.Forms
{
    partial class FrmTaskManager
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmTaskManager));
            this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.killProcessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dumpMemoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startProcessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.suspendProcessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lineToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.processTreeHost = new System.Windows.Forms.Integration.ElementHost();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.processesToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.contextMenuStrip.SuspendLayout();
            this.tableLayoutPanel.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // contextMenuStrip
            // 
            this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.killProcessToolStripMenuItem,
            this.dumpMemoryToolStripMenuItem,
            this.startProcessToolStripMenuItem,
            this.suspendProcessToolStripMenuItem,
            this.lineToolStripMenuItem,
            this.refreshToolStripMenuItem});
            this.contextMenuStrip.Name = "ctxtMenu";
            this.contextMenuStrip.Size = new System.Drawing.Size(163, 120);
            // 
            // killProcessToolStripMenuItem
            // 
            this.killProcessToolStripMenuItem.Image = global::Pulsar.Server.Properties.Resources.cancel;
            this.killProcessToolStripMenuItem.Name = "killProcessToolStripMenuItem";
            this.killProcessToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.killProcessToolStripMenuItem.Text = "Kill Process";
            this.killProcessToolStripMenuItem.Click += new System.EventHandler(this.killProcessToolStripMenuItem_Click);
            // 
            // dumpMemoryToolStripMenuItem
            // 
            this.dumpMemoryToolStripMenuItem.Image = global::Pulsar.Server.Properties.Resources.broom;
            this.dumpMemoryToolStripMenuItem.Name = "dumpMemoryToolStripMenuItem";
            this.dumpMemoryToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.dumpMemoryToolStripMenuItem.Text = "Dump Memory";
            this.dumpMemoryToolStripMenuItem.Click += new System.EventHandler(this.dumpMemoryToolStripMenuItem_Click);
            // 
            // startProcessToolStripMenuItem
            // 
            this.startProcessToolStripMenuItem.Image = global::Pulsar.Server.Properties.Resources.application_go;
            this.startProcessToolStripMenuItem.Name = "startProcessToolStripMenuItem";
            this.startProcessToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.startProcessToolStripMenuItem.Text = "Start Process";
            this.startProcessToolStripMenuItem.Click += new System.EventHandler(this.startProcessToolStripMenuItem_Click);
            // 
            // suspendProcessToolStripMenuItem
            // 
            this.suspendProcessToolStripMenuItem.Image = global::Pulsar.Server.Properties.Resources.wait;
            this.suspendProcessToolStripMenuItem.Name = "suspendProcessToolStripMenuItem";
            this.suspendProcessToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.suspendProcessToolStripMenuItem.Text = "Suspend Process";
            this.suspendProcessToolStripMenuItem.Click += new System.EventHandler(this.suspendProcessToolStripMenuItem_Click);
            // 
            // lineToolStripMenuItem
            // 
            this.lineToolStripMenuItem.Name = "lineToolStripMenuItem";
            this.lineToolStripMenuItem.Size = new System.Drawing.Size(159, 6);
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Image = global::Pulsar.Server.Properties.Resources.refresh;
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(162, 22);
            this.refreshToolStripMenuItem.Text = "Refresh";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.ColumnCount = 1;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.Controls.Add(this.processTreeHost, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.statusStrip, 0, 1);
            this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 2;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(821, 493);
            this.tableLayoutPanel.TabIndex = 2;
            // 
            // processTreeHost
            // 
            this.processTreeHost.BackColor = System.Drawing.Color.Transparent;
            this.processTreeHost.ContextMenuStrip = this.contextMenuStrip;
            this.processTreeHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.processTreeHost.Location = new System.Drawing.Point(3, 3);
            this.processTreeHost.Name = "processTreeHost";
            this.processTreeHost.Size = new System.Drawing.Size(815, 465);
            this.processTreeHost.TabIndex = 1;
            this.processTreeHost.Text = "processTreeHost";
            this.processTreeHost.Child = null;
            // 
            // statusStrip
            // 
            this.statusStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.processesToolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 471);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(821, 22);
            this.statusStrip.TabIndex = 2;
            this.statusStrip.Text = "statusStrip1";
            // 
            // processesToolStripStatusLabel
            // 
            this.processesToolStripStatusLabel.Name = "processesToolStripStatusLabel";
            this.processesToolStripStatusLabel.Size = new System.Drawing.Size(70, 17);
            this.processesToolStripStatusLabel.Text = "Processes: 0";
            // 
            // FrmTaskManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(821, 493);
            this.Controls.Add(this.tableLayoutPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimumSize = new System.Drawing.Size(351, 449);
            this.Name = "FrmTaskManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Task Manager []";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmTaskManager_FormClosing);
            this.Load += new System.EventHandler(this.FrmTaskManager_Load);
            this.contextMenuStrip.ResumeLayout(false);
            this.tableLayoutPanel.ResumeLayout(false);
            this.tableLayoutPanel.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem killProcessToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem refreshToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startProcessToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator lineToolStripMenuItem;
        private ElementHost processTreeHost;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel processesToolStripStatusLabel;
        private System.Windows.Forms.ToolStripMenuItem dumpMemoryToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem suspendProcessToolStripMenuItem;
    }
}