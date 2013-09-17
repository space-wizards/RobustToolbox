namespace ParticleEditor
{
    partial class ParticleEditorMainForm
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
            this.particleDisplay = new ParticleEditor.ParticleDisplay();
            this.particleConfigurator = new ParticleEditor.ParticleConfigurator();
            this.SuspendLayout();
            // 
            // particleDisplay
            // 
            this.particleDisplay.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.particleDisplay.AutoSize = true;
            this.particleDisplay.Location = new System.Drawing.Point(367, 12);
            this.particleDisplay.MainForm = null;
            this.particleDisplay.Name = "particleDisplay";
            this.particleDisplay.ParticleConfigurator = null;
            this.particleDisplay.ResourceManager = null;
            this.particleDisplay.Size = new System.Drawing.Size(716, 761);
            this.particleDisplay.TabIndex = 1;
            // 
            // particleConfigurator
            // 
            this.particleConfigurator.Location = new System.Drawing.Point(12, 12);
            this.particleConfigurator.MainForm = null;
            this.particleConfigurator.Name = "particleConfigurator";
            this.particleConfigurator.Size = new System.Drawing.Size(349, 764);
            this.particleConfigurator.TabIndex = 0;
            // 
            // ParticleEditorMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1095, 796);
            this.Controls.Add(this.particleDisplay);
            this.Controls.Add(this.particleConfigurator);
            this.Name = "ParticleEditorMainForm";
            this.Text = "ParticleEditorMainForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private ParticleConfigurator particleConfigurator;
        private ParticleDisplay particleDisplay;
    }
}