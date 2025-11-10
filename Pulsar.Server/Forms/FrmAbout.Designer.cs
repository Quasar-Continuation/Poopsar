namespace Pulsar.Server.Forms
{
    partial class FrmAbout
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmAbout));
            picIcon = new System.Windows.Forms.PictureBox();
            lblTitle = new System.Windows.Forms.Label();
            lblVersion = new System.Windows.Forms.Label();
            btnOkay = new System.Windows.Forms.Button();
            rtxtContent = new System.Windows.Forms.RichTextBox();
            lblLicense = new System.Windows.Forms.Label();
            lnkCredits = new System.Windows.Forms.LinkLabel();
            lnkGithubPage = new System.Windows.Forms.LinkLabel();
            lnkTelegram = new System.Windows.Forms.LinkLabel();
            lblSubTitle = new System.Windows.Forms.Label();
            label1 = new System.Windows.Forms.Label();
            cntTxtContent = new System.Windows.Forms.RichTextBox();
            pictureBox1 = new System.Windows.Forms.PictureBox();
            button1 = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)picIcon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // picIcon
            // 
            picIcon.Image = Properties.Resources.Pulsar_Server;
            picIcon.Location = new System.Drawing.Point(12, 12);
            picIcon.Name = "picIcon";
            picIcon.Size = new System.Drawing.Size(64, 64);
            picIcon.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            picIcon.TabIndex = 0;
            picIcon.TabStop = false;
            // 
            // lblTitle
            // 
            lblTitle.AutoSize = true;
            lblTitle.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblTitle.Location = new System.Drawing.Point(82, 7);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new System.Drawing.Size(69, 30);
            lblTitle.TabIndex = 0;
            lblTitle.Text = "Pulsar";
            // 
            // lblVersion
            // 
            lblVersion.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Italic, System.Drawing.GraphicsUnit.Point, 0);
            lblVersion.Location = new System.Drawing.Point(438, 41);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new System.Drawing.Size(75, 13);
            lblVersion.TabIndex = 2;
            lblVersion.Text = "%VERSION%";
            lblVersion.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // btnOkay
            // 
            btnOkay.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnOkay.Location = new System.Drawing.Point(438, 370);
            btnOkay.Name = "btnOkay";
            btnOkay.Size = new System.Drawing.Size(75, 23);
            btnOkay.TabIndex = 8;
            btnOkay.Text = "&Okay";
            btnOkay.UseVisualStyleBackColor = true;
            btnOkay.Click += btnOkay_Click;
            // 
            // rtxtContent
            // 
            rtxtContent.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            rtxtContent.Location = new System.Drawing.Point(15, 219);
            rtxtContent.Name = "rtxtContent";
            rtxtContent.ReadOnly = true;
            rtxtContent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            rtxtContent.Size = new System.Drawing.Size(498, 145);
            rtxtContent.TabIndex = 7;
            rtxtContent.Text = "";
            // 
            // lblLicense
            // 
            lblLicense.AutoSize = true;
            lblLicense.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblLicense.Location = new System.Drawing.Point(12, 201);
            lblLicense.Name = "lblLicense";
            lblLicense.Size = new System.Drawing.Size(46, 15);
            lblLicense.TabIndex = 6;
            lblLicense.Text = "License";
            // 
            // lnkCredits
            // 
            lnkCredits.AutoSize = true;
            lnkCredits.Location = new System.Drawing.Point(415, 83);
            lnkCredits.Name = "lnkCredits";
            lnkCredits.Size = new System.Drawing.Size(97, 13);
            lnkCredits.TabIndex = 5;
            lnkCredits.TabStop = true;
            lnkCredits.Text = "3rd-party licenses";
            lnkCredits.LinkClicked += lnkCredits_LinkClicked;
            // 
            // lnkGithubPage
            // 
            lnkGithubPage.AutoSize = true;
            lnkGithubPage.Location = new System.Drawing.Point(441, 61);
            lnkGithubPage.Name = "lnkGithubPage";
            lnkGithubPage.Size = new System.Drawing.Size(73, 13);
            lnkGithubPage.TabIndex = 3;
            lnkGithubPage.TabStop = true;
            lnkGithubPage.Text = "GitHub page";
            lnkGithubPage.LinkClicked += lnkGithubPage_LinkClicked;
            // 
            // lnkTelegram
            // 
            lnkTelegram.AutoSize = true;
            lnkTelegram.Location = new System.Drawing.Point(344, 61);
            lnkTelegram.Name = "lnkTelegram";
            lnkTelegram.Size = new System.Drawing.Size(96, 13);
            lnkTelegram.TabIndex = 4;
            lnkTelegram.TabStop = true;
            lnkTelegram.Text = "Telegram channel";
            lnkTelegram.LinkClicked += lnkTelegram_LinkClicked;
            // 
            // lblSubTitle
            // 
            lblSubTitle.AutoSize = true;
            lblSubTitle.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            lblSubTitle.Location = new System.Drawing.Point(84, 37);
            lblSubTitle.Name = "lblSubTitle";
            lblSubTitle.Size = new System.Drawing.Size(170, 17);
            lblSubTitle.TabIndex = 1;
            lblSubTitle.Text = "Remote Administration Tool";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            label1.Location = new System.Drawing.Point(12, 108);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(354, 15);
            label1.TabIndex = 9;
            label1.Text = "Thanks to the contributors below for making this project possible:";
            // 
            // cntTxtContent
            // 
            cntTxtContent.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            cntTxtContent.Location = new System.Drawing.Point(14, 131);
            cntTxtContent.Name = "cntTxtContent";
            cntTxtContent.ReadOnly = true;
            cntTxtContent.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.Vertical;
            cntTxtContent.Size = new System.Drawing.Size(498, 62);
            cntTxtContent.TabIndex = 10;
            cntTxtContent.Text = "";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.aboutflag1;
            pictureBox1.Location = new System.Drawing.Point(7, 36);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new System.Drawing.Size(512, 512);
            pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
            pictureBox1.TabIndex = 11;
            pictureBox1.TabStop = false;
            // 
            // button1
            // 
            button1.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            button1.Location = new System.Drawing.Point(15, 370);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(188, 23);
            button1.TabIndex = 12;
            button1.Text = "&Message From Devs";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // FrmAbout
            // 
            AcceptButton = btnOkay;
            AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            CancelButton = btnOkay;
            ClientSize = new System.Drawing.Size(525, 405);
            Controls.Add(button1);
            Controls.Add(cntTxtContent);
            Controls.Add(label1);
            Controls.Add(lblSubTitle);
            Controls.Add(lnkTelegram);
            Controls.Add(lnkGithubPage);
            Controls.Add(lnkCredits);
            Controls.Add(lblLicense);
            Controls.Add(rtxtContent);
            Controls.Add(btnOkay);
            Controls.Add(lblVersion);
            Controls.Add(lblTitle);
            Controls.Add(picIcon);
            Controls.Add(pictureBox1);
            Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            Icon = (System.Drawing.Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "FrmAbout";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Pulsar Premium - About";
            Load += FrmAbout_Load;
            ((System.ComponentModel.ISupportInitialize)picIcon).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.PictureBox picIcon;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Button btnOkay;
        private System.Windows.Forms.RichTextBox rtxtContent;
        private System.Windows.Forms.Label lblLicense;
        private System.Windows.Forms.LinkLabel lnkCredits;
        private System.Windows.Forms.LinkLabel lnkGithubPage;
        private System.Windows.Forms.LinkLabel lnkTelegram;
        private System.Windows.Forms.Label lblSubTitle;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox cntTxtContent;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button button1;
    }
}