namespace Pulsar.Server.Forms
{
    partial class FrmLoader
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
            FilePath = new System.Windows.Forms.TextBox();
            groupBox3 = new System.Windows.Forms.GroupBox();
            custompath = new System.Windows.Forms.CheckBox();
            custompathbutton = new System.Windows.Forms.Button();
            Pathtxt = new System.Windows.Forms.TextBox();
            checkBox3 = new System.Windows.Forms.CheckBox();
            checkBox2 = new System.Windows.Forms.CheckBox();
            checkBox1 = new System.Windows.Forms.CheckBox();
            button2 = new System.Windows.Forms.Button();
            RunPE = new System.Windows.Forms.RadioButton();
            button1 = new System.Windows.Forms.Button();
            groupBox3.SuspendLayout();
            SuspendLayout();
            // 
            // FilePath
            // 
            FilePath.Location = new System.Drawing.Point(102, 22);
            FilePath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            FilePath.Name = "FilePath";
            FilePath.Size = new System.Drawing.Size(375, 23);
            FilePath.TabIndex = 0;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(custompath);
            groupBox3.Controls.Add(custompathbutton);
            groupBox3.Controls.Add(Pathtxt);
            groupBox3.Controls.Add(checkBox3);
            groupBox3.Controls.Add(checkBox2);
            groupBox3.Controls.Add(checkBox1);
            groupBox3.Controls.Add(button2);
            groupBox3.Controls.Add(RunPE);
            groupBox3.Controls.Add(button1);
            groupBox3.Controls.Add(FilePath);
            groupBox3.Location = new System.Drawing.Point(14, 14);
            groupBox3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox3.Name = "groupBox3";
            groupBox3.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            groupBox3.Size = new System.Drawing.Size(484, 305);
            groupBox3.TabIndex = 2;
            groupBox3.TabStop = false;
            groupBox3.Text = "File";
            // 
            // custompath
            // 
            custompath.AutoSize = true;
            custompath.Location = new System.Drawing.Point(407, 80);
            custompath.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            custompath.Name = "custompath";
            custompath.Size = new System.Drawing.Size(68, 19);
            custompath.TabIndex = 19;
            custompath.Text = "Custom";
            custompath.UseVisualStyleBackColor = true;
            custompath.CheckedChanged += custompath_CheckedChanged;
            // 
            // custompathbutton
            // 
            custompathbutton.Location = new System.Drawing.Point(6, 49);
            custompathbutton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            custompathbutton.Name = "custompathbutton";
            custompathbutton.Size = new System.Drawing.Size(88, 27);
            custompathbutton.TabIndex = 18;
            custompathbutton.Text = "Path";
            custompathbutton.UseVisualStyleBackColor = true;
            custompathbutton.Visible = false;
            custompathbutton.Click += custompathbutton_Click;
            // 
            // Pathtxt
            // 
            Pathtxt.Location = new System.Drawing.Point(101, 51);
            Pathtxt.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Pathtxt.Name = "Pathtxt";
            Pathtxt.Size = new System.Drawing.Size(375, 23);
            Pathtxt.TabIndex = 17;
            Pathtxt.Visible = false;
            // 
            // checkBox3
            // 
            checkBox3.AutoSize = true;
            checkBox3.Location = new System.Drawing.Point(233, 149);
            checkBox3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBox3.Name = "checkBox3";
            checkBox3.Size = new System.Drawing.Size(70, 19);
            checkBox3.TabIndex = 16;
            checkBox3.Text = "MSBuild";
            checkBox3.UseVisualStyleBackColor = true;
            // 
            // checkBox2
            // 
            checkBox2.AutoSize = true;
            checkBox2.Location = new System.Drawing.Point(311, 149);
            checkBox2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBox2.Name = "checkBox2";
            checkBox2.Size = new System.Drawing.Size(69, 19);
            checkBox2.TabIndex = 15;
            checkBox2.Text = "RegSvcs";
            checkBox2.UseVisualStyleBackColor = true;
            // 
            // checkBox1
            // 
            checkBox1.AutoSize = true;
            checkBox1.Location = new System.Drawing.Point(155, 149);
            checkBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            checkBox1.Name = "checkBox1";
            checkBox1.Size = new System.Drawing.Size(70, 19);
            checkBox1.TabIndex = 14;
            checkBox1.Text = "RegAsm";
            checkBox1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            button2.Location = new System.Drawing.Point(388, 141);
            button2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            button2.Name = "button2";
            button2.Size = new System.Drawing.Size(88, 27);
            button2.TabIndex = 4;
            button2.Text = "Execute";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // RunPE
            // 
            RunPE.AutoSize = true;
            RunPE.Checked = true;
            RunPE.Location = new System.Drawing.Point(7, 149);
            RunPE.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            RunPE.Name = "RunPE";
            RunPE.Size = new System.Drawing.Size(59, 19);
            RunPE.TabIndex = 2;
            RunPE.TabStop = true;
            RunPE.Text = "RunPE";
            RunPE.UseVisualStyleBackColor = true;
            RunPE.Visible = false;
            // 
            // button1
            // 
            button1.Location = new System.Drawing.Point(7, 20);
            button1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            button1.Name = "button1";
            button1.Size = new System.Drawing.Size(88, 27);
            button1.TabIndex = 1;
            button1.Text = "File";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // FrmLoader
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(514, 194);
            Controls.Add(groupBox3);
            Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            Name = "FrmLoader";
            Text = "FrmLoader";
            FormClosing += FrmLoader_FormClosing;
            Load += FrmLoader_Load;
            groupBox3.ResumeLayout(false);
            groupBox3.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox FilePath;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.RadioButton RunPE;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.CheckBox checkBox3;
        private System.Windows.Forms.CheckBox checkBox2;
        private System.Windows.Forms.CheckBox checkBox1;
        private System.Windows.Forms.CheckBox custompath;
        private System.Windows.Forms.Button custompathbutton;
        private System.Windows.Forms.TextBox Pathtxt;
    }
}