namespace Pulsar.Server.Forms
{
    partial class FrmCertificate
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmCertificate));
            lblInfo = new System.Windows.Forms.Label();
            btnCreate = new System.Windows.Forms.Button();
            lblDescription = new System.Windows.Forms.Label();
            txtDetails = new System.Windows.Forms.TextBox();
            btnImport = new System.Windows.Forms.Button();
            btnSave = new System.Windows.Forms.Button();
            label1 = new System.Windows.Forms.Label();
            btnExit = new System.Windows.Forms.Button();
            SuspendLayout();
            // 
            // lblInfo
            // 
            lblInfo.AutoSize = true;
            lblInfo.Location = new System.Drawing.Point(12, 53);
            lblInfo.Name = "lblInfo";
            lblInfo.Size = new System.Drawing.Size(130, 13);
            lblInfo.TabIndex = 3;
            lblInfo.Text = "(this might take a while)";
            // 
            // btnCreate
            // 
            btnCreate.Location = new System.Drawing.Point(12, 27);
            btnCreate.Name = "btnCreate";
            btnCreate.Size = new System.Drawing.Size(75, 23);
            btnCreate.TabIndex = 1;
            btnCreate.Text = "Create";
            btnCreate.UseVisualStyleBackColor = true;
            btnCreate.Click += btnCreate_Click;
            // 
            // lblDescription
            // 
            lblDescription.AutoSize = true;
            lblDescription.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblDescription.Location = new System.Drawing.Point(9, 9);
            lblDescription.Name = "lblDescription";
            lblDescription.Size = new System.Drawing.Size(485, 15);
            lblDescription.TabIndex = 0;
            lblDescription.Text = "To use Pulsar create a new certificate or import an existing one from a previous installation.";
            // 
            // txtDetails
            // 
            txtDetails.Location = new System.Drawing.Point(12, 69);
            txtDetails.Multiline = true;
            txtDetails.Name = "txtDetails";
            txtDetails.ReadOnly = true;
            txtDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtDetails.Size = new System.Drawing.Size(517, 230);
            txtDetails.TabIndex = 4;
            // 
            // btnImport
            // 
            btnImport.Location = new System.Drawing.Point(93, 27);
            btnImport.Name = "btnImport";
            btnImport.Size = new System.Drawing.Size(130, 23);
            btnImport.TabIndex = 2;
            btnImport.Text = "Browse && Import";
            btnImport.UseVisualStyleBackColor = true;
            btnImport.Click += btnImport_Click;
            // 
            // btnSave
            // 
            btnSave.Enabled = false;
            btnSave.Location = new System.Drawing.Point(373, 305);
            btnSave.Name = "btnSave";
            btnSave.Size = new System.Drawing.Size(75, 23);
            btnSave.TabIndex = 6;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
            label1.Location = new System.Drawing.Point(12, 310);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(355, 15);
            label1.TabIndex = 5;
            label1.Text = "KEEP THIS FILE SAFE! LOSS RESULTS IN LOOSING ALL CLIENTS!";
            // 
            // btnExit
            // 
            btnExit.Location = new System.Drawing.Point(454, 306);
            btnExit.Name = "btnExit";
            btnExit.Size = new System.Drawing.Size(75, 23);
            btnExit.TabIndex = 7;
            btnExit.Text = "Exit";
            btnExit.UseVisualStyleBackColor = true;
            btnExit.Click += btnExit_Click;
            // 
            // FrmCertificate
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            ClientSize = new System.Drawing.Size(541, 341);
            Controls.Add(btnExit);
            Controls.Add(label1);
            Controls.Add(btnSave);
            Controls.Add(lblDescription);
            Controls.Add(txtDetails);
            Controls.Add(lblInfo);
            Controls.Add(btnImport);
            Controls.Add(btnCreate);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmCertificate";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Pulsar Premium - Certificate Wizard";
            Load += FrmCertificate_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
        private System.Windows.Forms.Label lblDescription;
        private System.Windows.Forms.Label lblInfo;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.TextBox txtDetails;
        private System.Windows.Forms.Button btnImport;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnExit;
    }
}
