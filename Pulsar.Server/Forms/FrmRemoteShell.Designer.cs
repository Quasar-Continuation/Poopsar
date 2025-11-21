namespace Pulsar.Server.Forms
{
    partial class FrmRemoteShell
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmRemoteShell));
            txtConsoleOutput = new System.Windows.Forms.RichTextBox();
            txtConsoleInput = new System.Windows.Forms.TextBox();
            tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            tableLayoutPanel.SuspendLayout();
            SuspendLayout();
            // 
            // txtConsoleOutput
            // 
            txtConsoleOutput.BackColor = System.Drawing.Color.Black;
            txtConsoleOutput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            txtConsoleOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            txtConsoleOutput.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtConsoleOutput.ForeColor = System.Drawing.Color.WhiteSmoke;
            txtConsoleOutput.Location = new System.Drawing.Point(3, 3);
            txtConsoleOutput.Name = "txtConsoleOutput";
            txtConsoleOutput.ReadOnly = true;
            txtConsoleOutput.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            txtConsoleOutput.Size = new System.Drawing.Size(631, 297);
            txtConsoleOutput.TabIndex = 1;
            txtConsoleOutput.Text = "";
            txtConsoleOutput.TextChanged += txtConsoleOutput_TextChanged;
            txtConsoleOutput.KeyPress += txtConsoleOutput_KeyPress;
            // 
            // txtConsoleInput
            // 
            txtConsoleInput.BackColor = System.Drawing.Color.Black;
            txtConsoleInput.BorderStyle = System.Windows.Forms.BorderStyle.None;
            txtConsoleInput.Dock = System.Windows.Forms.DockStyle.Fill;
            txtConsoleInput.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            txtConsoleInput.ForeColor = System.Drawing.Color.WhiteSmoke;
            txtConsoleInput.Location = new System.Drawing.Point(3, 306);
            txtConsoleInput.MaxLength = 200;
            txtConsoleInput.Name = "txtConsoleInput";
            txtConsoleInput.Size = new System.Drawing.Size(631, 16);
            txtConsoleInput.TabIndex = 0;
            txtConsoleInput.KeyDown += txtConsoleInput_KeyDown;
            // 
            // tableLayoutPanel
            // 
            tableLayoutPanel.BackColor = System.Drawing.Color.Black;
            tableLayoutPanel.ColumnCount = 1;
            tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel.Controls.Add(txtConsoleOutput, 0, 0);
            tableLayoutPanel.Controls.Add(txtConsoleInput, 0, 1);
            tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            tableLayoutPanel.Name = "tableLayoutPanel";
            tableLayoutPanel.RowCount = 2;
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            tableLayoutPanel.Size = new System.Drawing.Size(637, 323);
            tableLayoutPanel.TabIndex = 2;
            // 
            // FrmRemoteShell
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(637, 323);
            Controls.Add(tableLayoutPanel);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            Name = "FrmRemoteShell";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Remote Shell []";
            FormClosing += FrmRemoteShell_FormClosing;
            Load += FrmRemoteShell_Load;
            tableLayoutPanel.ResumeLayout(false);
            tableLayoutPanel.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.TextBox txtConsoleInput;
        private System.Windows.Forms.RichTextBox txtConsoleOutput;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
    }
}