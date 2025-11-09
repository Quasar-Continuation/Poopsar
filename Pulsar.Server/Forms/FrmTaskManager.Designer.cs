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
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmTaskManager));
            contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(components);
            killProcessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            suspendProcessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topmostWindowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topmostOnToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            topmostOffToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            lineToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator();
            startProcessToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            dumpMemoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            processTreeHost = new ElementHost();
            statusStrip = new System.Windows.Forms.StatusStrip();
            processesToolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            contextMenuStrip.SuspendLayout();
            tableLayoutPanel.SuspendLayout();
            statusStrip.SuspendLayout();
            SuspendLayout();
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { killProcessToolStripMenuItem, suspendProcessToolStripMenuItem, topmostWindowToolStripMenuItem, lineToolStripMenuItem, startProcessToolStripMenuItem, dumpMemoryToolStripMenuItem, refreshToolStripMenuItem });
            contextMenuStrip.Name = "ctxtMenu";
            contextMenuStrip.Size = new System.Drawing.Size(169, 142);
            // 
            // killProcessToolStripMenuItem
            // 
            killProcessToolStripMenuItem.Image = Properties.Resources.cancel;
            killProcessToolStripMenuItem.Name = "killProcessToolStripMenuItem";
            killProcessToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            killProcessToolStripMenuItem.Text = "Kill Process";
            killProcessToolStripMenuItem.Click += killProcessToolStripMenuItem_Click;
            // 
            // suspendProcessToolStripMenuItem
            // 
            suspendProcessToolStripMenuItem.Image = Properties.Resources.wait;
            suspendProcessToolStripMenuItem.Name = "suspendProcessToolStripMenuItem";
            suspendProcessToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            suspendProcessToolStripMenuItem.Text = "Suspend Process";
            suspendProcessToolStripMenuItem.Click += suspendProcessToolStripMenuItem_Click;
            // 
            // topmostWindowToolStripMenuItem
            // 
            topmostWindowToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { topmostOnToolStripMenuItem, topmostOffToolStripMenuItem });
            topmostWindowToolStripMenuItem.Image = Properties.Resources.topmostwindowicon;
            topmostWindowToolStripMenuItem.Name = "topmostWindowToolStripMenuItem";
            topmostWindowToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            topmostWindowToolStripMenuItem.Text = "Topmost Window";
            // 
            // topmostOnToolStripMenuItem
            // 
            topmostOnToolStripMenuItem.Image = Properties.Resources.startbutton;
            topmostOnToolStripMenuItem.Name = "topmostOnToolStripMenuItem";
            topmostOnToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            topmostOnToolStripMenuItem.Text = "Topmost On";
            topmostOnToolStripMenuItem.Click += topmostOnToolStripMenuItem_Click;
            // 
            // topmostOffToolStripMenuItem
            // 
            topmostOffToolStripMenuItem.Image = Properties.Resources.stopbutton;
            topmostOffToolStripMenuItem.Name = "topmostOffToolStripMenuItem";
            topmostOffToolStripMenuItem.Size = new System.Drawing.Size(141, 22);
            topmostOffToolStripMenuItem.Text = "Topmost Off";
            topmostOffToolStripMenuItem.Click += topmostOffToolStripMenuItem_Click;
            // 
            // lineToolStripMenuItem
            // 
            lineToolStripMenuItem.Name = "lineToolStripMenuItem";
            lineToolStripMenuItem.Size = new System.Drawing.Size(165, 6);
            // 
            // startProcessToolStripMenuItem
            // 
            startProcessToolStripMenuItem.Image = Properties.Resources.application_go;
            startProcessToolStripMenuItem.Name = "startProcessToolStripMenuItem";
            startProcessToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            startProcessToolStripMenuItem.Text = "Start Process";
            startProcessToolStripMenuItem.Click += startProcessToolStripMenuItem_Click;
            // 
            // dumpMemoryToolStripMenuItem
            // 
            dumpMemoryToolStripMenuItem.Image = Properties.Resources.broom;
            dumpMemoryToolStripMenuItem.Name = "dumpMemoryToolStripMenuItem";
            dumpMemoryToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            dumpMemoryToolStripMenuItem.Text = "Dump Memory";
            dumpMemoryToolStripMenuItem.Click += dumpMemoryToolStripMenuItem_Click;
            // 
            // refreshToolStripMenuItem
            // 
            refreshToolStripMenuItem.Image = Properties.Resources.refresh;
            refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            refreshToolStripMenuItem.Size = new System.Drawing.Size(168, 22);
            refreshToolStripMenuItem.Text = "Refresh";
            refreshToolStripMenuItem.Click += refreshToolStripMenuItem_Click;
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel.Controls.Add(processTreeHost, 0, 0);
            tableLayoutPanel.Controls.Add(statusStrip, 0, 1);
            tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            tableLayoutPanel.Size = new System.Drawing.Size(821, 493);
            tableLayoutPanel.TabIndex = 2;
            // 
            // processTreeHost
            // 
            processTreeHost.BackColor = System.Drawing.Color.Transparent;
            processTreeHost.ContextMenuStrip = contextMenuStrip;
            processTreeHost.Dock = System.Windows.Forms.DockStyle.Fill;
            processTreeHost.Location = new System.Drawing.Point(3, 3);
            processTreeHost.Name = "processTreeHost";
            processTreeHost.Size = new System.Drawing.Size(815, 465);
            processTreeHost.TabIndex = 1;
            processTreeHost.Text = "processTreeHost";
            // 
            // statusStrip
            // 
            statusStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { processesToolStripStatusLabel, toolStripStatusLabel1 });
            statusStrip.Location = new System.Drawing.Point(0, 471);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new System.Drawing.Size(821, 22);
            statusStrip.TabIndex = 2;
            statusStrip.Text = "statusStrip1";
            // 
            // processesToolStripStatusLabel
            // 
            processesToolStripStatusLabel.Name = "processesToolStripStatusLabel";
            processesToolStripStatusLabel.Size = new System.Drawing.Size(70, 17);
            processesToolStripStatusLabel.Text = "Processes: 0";
            // 
            // toolStripStatusLabel1
            // 
            toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            toolStripStatusLabel1.Size = new System.Drawing.Size(10, 17);
            toolStripStatusLabel1.Text = "‎ ";
            // 
            // FrmTaskManager
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(821, 493);
            Controls.Add(tableLayoutPanel);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MinimumSize = new System.Drawing.Size(351, 449);
            Name = "FrmTaskManager";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Task Manager []";
            FormClosing += FrmTaskManager_FormClosing;
            Load += FrmTaskManager_Load;
            contextMenuStrip.ResumeLayout(false);
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ResumeLayout(false);
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
        private System.Windows.Forms.ToolStripMenuItem topmostWindowToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topmostOnToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem topmostOffToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
    }
}