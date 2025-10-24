using Pulsar.Server.Forms.DarkMode;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Pulsar.Server.Forms
{
    partial class FrmPluginManager
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
            this.btnManage = new System.Windows.Forms.Button();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.listView = new NonResizableListView();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colVersion = new System.Windows.Forms.ColumnHeader();
            this.colStatus = new System.Windows.Forms.ColumnHeader();
            this.colType = new System.Windows.Forms.ColumnHeader();
            this.colActions = new System.Windows.Forms.ColumnHeader();
            this.SuspendLayout();

            this.btnManage.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.btnManage.Location = new System.Drawing.Point(12, 45);
            this.btnManage.Name = "btnManage";
            this.btnManage.Size = new System.Drawing.Size(80, 30);
            this.btnManage.TabIndex = 0;
            this.btnManage.Text = "Manage";
            this.btnManage.UseVisualStyleBackColor = true;

            this.btnRefresh.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left;
            this.btnRefresh.Location = new System.Drawing.Point(100, 45);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(80, 30);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;

            this.btnBrowse.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.btnBrowse.Location = new System.Drawing.Point(580, 45);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(120, 30);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "Browse Plugins";
            this.btnBrowse.UseVisualStyleBackColor = true;

            this.listView.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.listView.CheckBoxes = true;
            this.listView.FullRowSelect = true;
            this.listView.GridLines = false;
            this.listView.Location = new System.Drawing.Point(12, 85);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(688, 350);
            this.listView.TabIndex = 3;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            this.listView.Sorting = System.Windows.Forms.SortOrder.None;
            this.listView.AllowColumnReorder = false;
            this.listView.OwnerDraw = true;
            this.listView.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.listView_DrawColumnHeader);
            this.listView.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.listView_DrawItem);
            this.listView.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.listView_DrawSubItem);
            this.listView.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView_ColumnClick);
            this.listView.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listView_MouseClick);
            this.listView.DoubleClick += new System.EventHandler(this.listView_DoubleClick);
            this.listView.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            var rowHeightImages = new ImageList();
            rowHeightImages.ImageSize = new Size(1, 28);
            this.listView.SmallImageList = rowHeightImages;

            this.colName.Text = "Plugin Name";
            this.colName.Width = 320;

            this.colVersion.Text = "Version";
            this.colVersion.Width = 110;

            this.colStatus.Text = "Status";
            this.colStatus.Width = 110;

            this.colType.Text = "Type";
            this.colType.Width = 200;

            this.colActions.Text = "Actions";
            this.colActions.Width = 125;
            this.colActions.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;

            this.btnSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnSave.Location = new System.Drawing.Point(500, 450);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(120, 30);
            this.btnSave.TabIndex = 4;
            this.btnSave.Text = "Save & Restart";
            this.btnSave.UseVisualStyleBackColor = true;

            this.btnClose.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnClose.Location = new System.Drawing.Point(630, 450);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(80, 30);
            this.btnClose.TabIndex = 5;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;

            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(12, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(120, 21);
            this.lblTitle.TabIndex = 6;
            this.lblTitle.Text = "Plugin Manager";

            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(720, 500);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.listView);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.btnManage);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new System.Drawing.Size(600, 400);
            this.Name = "FrmPluginManager";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Plugin Manager";
            this.ResumeLayout(false);
            this.PerformLayout();

            this.listView.Columns.Add(this.colName);
            this.listView.Columns.Add(this.colVersion);
            this.listView.Columns.Add(this.colStatus);
            this.listView.Columns.Add(this.colType);
            this.listView.Columns.Add(this.colActions);
        }

        #endregion
        private System.Windows.Forms.Button btnManage;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnBrowse;
        private NonResizableListView listView;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colVersion;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colType;
        private System.Windows.Forms.ColumnHeader colActions;
    }
}