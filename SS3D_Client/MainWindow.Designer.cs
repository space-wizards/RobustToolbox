namespace SS3D
{
    partial class MainWindow
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            this.disconnectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editModeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.quitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.turfToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noneToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.spaceToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.floorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wallToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.atomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.noneToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.itemToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.crowbarToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.welderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wrenchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.containerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolboxToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.miscToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.flashlightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.objectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.doorToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.lightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.wallLightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.Black;
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(0);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuToolStripMenuItem,
            this.editToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(0);
            this.menuStrip1.Size = new System.Drawing.Size(1008, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuToolStripMenuItem
            // 
            this.menuToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.connectToolStripMenuItem,
            this.disconnectToolStripMenuItem,
            this.editModeToolStripMenuItem,
            this.quitToolStripMenuItem});
            this.menuToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.menuToolStripMenuItem.Name = "menuToolStripMenuItem";
            this.menuToolStripMenuItem.Padding = new System.Windows.Forms.Padding(0);
            this.menuToolStripMenuItem.Size = new System.Drawing.Size(42, 24);
            this.menuToolStripMenuItem.Text = "Menu";
            // 
            // connectToolStripMenuItem
            // 
            this.connectToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.connectToolStripMenuItem.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.connectToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripTextBox1});
            this.connectToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            this.connectToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.connectToolStripMenuItem.Text = "Connect";
            // 
            // toolStripTextBox1
            // 
            this.toolStripTextBox1.BackColor = System.Drawing.SystemColors.MenuText;
            this.toolStripTextBox1.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            this.toolStripTextBox1.Size = new System.Drawing.Size(100, 23);
            this.toolStripTextBox1.Text = "127.0.0.1";
            this.toolStripTextBox1.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.toolStripTextBox1_KeyPress);
            // 
            // disconnectToolStripMenuItem
            // 
            this.disconnectToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.disconnectToolStripMenuItem.Enabled = false;
            this.disconnectToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.disconnectToolStripMenuItem.Name = "disconnectToolStripMenuItem";
            this.disconnectToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.disconnectToolStripMenuItem.Text = "Disconnect";
            this.disconnectToolStripMenuItem.Click += new System.EventHandler(this.disconnectToolStripMenuItem_Click);
            // 
            // editModeToolStripMenuItem
            // 
            this.editModeToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.editModeToolStripMenuItem.Enabled = false;
            this.editModeToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.editModeToolStripMenuItem.Name = "editModeToolStripMenuItem";
            this.editModeToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.editModeToolStripMenuItem.Text = "Edit mode";
            this.editModeToolStripMenuItem.Click += new System.EventHandler(this.editModeToolStripMenuItem_Click);
            // 
            // quitToolStripMenuItem
            // 
            this.quitToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.quitToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.quitToolStripMenuItem.Name = "quitToolStripMenuItem";
            this.quitToolStripMenuItem.Size = new System.Drawing.Size(133, 22);
            this.quitToolStripMenuItem.Text = "Quit";
            this.quitToolStripMenuItem.Click += new System.EventHandler(this.quitToolStripMenuItem_Click);
            // 
            // editToolStripMenuItem
            // 
            this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.turfToolStripMenuItem,
            this.atomToolStripMenuItem});
            this.editToolStripMenuItem.Enabled = false;
            this.editToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.editToolStripMenuItem.Name = "editToolStripMenuItem";
            this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 24);
            this.editToolStripMenuItem.Text = "Edit";
            this.editToolStripMenuItem.Visible = false;
            // 
            // turfToolStripMenuItem
            // 
            this.turfToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.turfToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem,
            this.spaceToolStripMenuItem,
            this.floorToolStripMenuItem,
            this.wallToolStripMenuItem});
            this.turfToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.turfToolStripMenuItem.Name = "turfToolStripMenuItem";
            this.turfToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.turfToolStripMenuItem.Text = "Turf";
            this.turfToolStripMenuItem.Click += new System.EventHandler(this.turfToolStripMenuItem_Click);
            // 
            // noneToolStripMenuItem
            // 
            this.noneToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.noneToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.noneToolStripMenuItem.Name = "noneToolStripMenuItem";
            this.noneToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.noneToolStripMenuItem.Text = "None";
            this.noneToolStripMenuItem.Click += new System.EventHandler(this.noneToolStripMenuItem_Click);
            // 
            // spaceToolStripMenuItem
            // 
            this.spaceToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.spaceToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.spaceToolStripMenuItem.Name = "spaceToolStripMenuItem";
            this.spaceToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.spaceToolStripMenuItem.Text = "Space";
            this.spaceToolStripMenuItem.Click += new System.EventHandler(this.spaceToolStripMenuItem_Click);
            // 
            // floorToolStripMenuItem
            // 
            this.floorToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.floorToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.floorToolStripMenuItem.Name = "floorToolStripMenuItem";
            this.floorToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.floorToolStripMenuItem.Text = "Floor";
            this.floorToolStripMenuItem.Click += new System.EventHandler(this.floorToolStripMenuItem_Click);
            // 
            // wallToolStripMenuItem
            // 
            this.wallToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.wallToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.wallToolStripMenuItem.Name = "wallToolStripMenuItem";
            this.wallToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.wallToolStripMenuItem.Text = "Wall";
            this.wallToolStripMenuItem.Click += new System.EventHandler(this.wallToolStripMenuItem_Click);
            // 
            // atomToolStripMenuItem
            // 
            this.atomToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.atomToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.noneToolStripMenuItem1,
            this.itemToolStripMenuItem,
            this.objectToolStripMenuItem});
            this.atomToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.atomToolStripMenuItem.Name = "atomToolStripMenuItem";
            this.atomToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.atomToolStripMenuItem.Text = "Atom";
            // 
            // noneToolStripMenuItem1
            // 
            this.noneToolStripMenuItem1.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.noneToolStripMenuItem1.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.noneToolStripMenuItem1.Name = "noneToolStripMenuItem1";
            this.noneToolStripMenuItem1.Size = new System.Drawing.Size(152, 22);
            this.noneToolStripMenuItem1.Text = "None";
            this.noneToolStripMenuItem1.Click += new System.EventHandler(this.noneToolStripMenuItem1_Click);
            // 
            // itemToolStripMenuItem
            // 
            this.itemToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.itemToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolToolStripMenuItem,
            this.containerToolStripMenuItem,
            this.miscToolStripMenuItem});
            this.itemToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.itemToolStripMenuItem.Name = "itemToolStripMenuItem";
            this.itemToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.itemToolStripMenuItem.Text = "Item";
            // 
            // toolToolStripMenuItem
            // 
            this.toolToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.toolToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.crowbarToolStripMenuItem,
            this.welderToolStripMenuItem,
            this.wrenchToolStripMenuItem});
            this.toolToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.toolToolStripMenuItem.Name = "toolToolStripMenuItem";
            this.toolToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.toolToolStripMenuItem.Text = "Tool";
            // 
            // crowbarToolStripMenuItem
            // 
            this.crowbarToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.crowbarToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.crowbarToolStripMenuItem.Name = "crowbarToolStripMenuItem";
            this.crowbarToolStripMenuItem.Size = new System.Drawing.Size(119, 22);
            this.crowbarToolStripMenuItem.Text = "Crowbar";
            this.crowbarToolStripMenuItem.Click += new System.EventHandler(this.crowbarToolStripMenuItem_Click);
            // 
            // welderToolStripMenuItem
            // 
            this.welderToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.welderToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.welderToolStripMenuItem.Name = "welderToolStripMenuItem";
            this.welderToolStripMenuItem.Size = new System.Drawing.Size(119, 22);
            this.welderToolStripMenuItem.Text = "Welder";
            this.welderToolStripMenuItem.Click += new System.EventHandler(this.welderToolStripMenuItem_Click);
            // 
            // wrenchToolStripMenuItem
            // 
            this.wrenchToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.wrenchToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.wrenchToolStripMenuItem.Name = "wrenchToolStripMenuItem";
            this.wrenchToolStripMenuItem.Size = new System.Drawing.Size(119, 22);
            this.wrenchToolStripMenuItem.Text = "Wrench";
            this.wrenchToolStripMenuItem.Click += new System.EventHandler(this.wrenchToolStripMenuItem_Click);
            // 
            // containerToolStripMenuItem
            // 
            this.containerToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.containerToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolboxToolStripMenuItem});
            this.containerToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.containerToolStripMenuItem.Name = "containerToolStripMenuItem";
            this.containerToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.containerToolStripMenuItem.Text = "Container";
            // 
            // toolboxToolStripMenuItem
            // 
            this.toolboxToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.toolboxToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.toolboxToolStripMenuItem.Name = "toolboxToolStripMenuItem";
            this.toolboxToolStripMenuItem.Size = new System.Drawing.Size(117, 22);
            this.toolboxToolStripMenuItem.Text = "Toolbox";
            this.toolboxToolStripMenuItem.Click += new System.EventHandler(this.toolboxToolStripMenuItem_Click);
            // 
            // miscToolStripMenuItem
            // 
            this.miscToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.miscToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.flashlightToolStripMenuItem});
            this.miscToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.miscToolStripMenuItem.Name = "miscToolStripMenuItem";
            this.miscToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.miscToolStripMenuItem.Text = "Misc";
            // 
            // flashlightToolStripMenuItem
            // 
            this.flashlightToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.flashlightToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.flashlightToolStripMenuItem.Name = "flashlightToolStripMenuItem";
            this.flashlightToolStripMenuItem.Size = new System.Drawing.Size(125, 22);
            this.flashlightToolStripMenuItem.Text = "Flashlight";
            this.flashlightToolStripMenuItem.Click += new System.EventHandler(this.flashlightToolStripMenuItem_Click);
            // 
            // objectToolStripMenuItem
            // 
            this.objectToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.objectToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.doorToolStripMenuItem,
            this.lightToolStripMenuItem});
            this.objectToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.objectToolStripMenuItem.Name = "objectToolStripMenuItem";
            this.objectToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.objectToolStripMenuItem.Text = "Object";
            // 
            // doorToolStripMenuItem
            // 
            this.doorToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.doorToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.doorToolStripMenuItem.Name = "doorToolStripMenuItem";
            this.doorToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.doorToolStripMenuItem.Text = "Door";
            this.doorToolStripMenuItem.Click += new System.EventHandler(this.doorToolStripMenuItem_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Enabled = false;
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 708);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1008, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            this.statusStrip1.Visible = false;
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(0, 17);
            // 
            // lightToolStripMenuItem
            // 
            this.lightToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lightToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.wallLightToolStripMenuItem});
            this.lightToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.lightToolStripMenuItem.Name = "lightToolStripMenuItem";
            this.lightToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.lightToolStripMenuItem.Text = "Light";
            // 
            // wallLightToolStripMenuItem
            // 
            this.wallLightToolStripMenuItem.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.wallLightToolStripMenuItem.ForeColor = System.Drawing.SystemColors.ActiveCaption;
            this.wallLightToolStripMenuItem.Name = "wallLightToolStripMenuItem";
            this.wallLightToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.wallLightToolStripMenuItem.Text = "Wall Light";
            this.wallLightToolStripMenuItem.Click += new System.EventHandler(this.wallLightToolStripMenuItem_Click);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ClientSize = new System.Drawing.Size(1008, 730);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainWindow";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.MainWindow_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem connectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem disconnectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem quitToolStripMenuItem;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
        private System.Windows.Forms.ToolStripMenuItem editModeToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem turfToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem atomToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripMenuItem spaceToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem floorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wallToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem itemToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem objectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem crowbarToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem welderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wrenchToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem containerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolboxToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem miscToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem flashlightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem doorToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem noneToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem lightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem wallLightToolStripMenuItem;
    }
}