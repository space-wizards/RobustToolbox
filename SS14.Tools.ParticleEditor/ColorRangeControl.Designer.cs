namespace SS14.Tools.ParticleEditor
{
    partial class ColorRangeControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.colorSwatchStartColor = new SS14.Tools.ParticleEditor.ColorSwatch();
            this.colorSwatchEndColor = new SS14.Tools.ParticleEditor.ColorSwatch();
            this.SuspendLayout();
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(135, 7);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(94, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Particle End Color:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(1, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Particle Start Color:";
            // 
            // colorSwatchStartColor
            // 
            this.colorSwatchStartColor.Location = new System.Drawing.Point(101, 4);
            this.colorSwatchStartColor.Name = "colorSwatchStartColor";
            this.colorSwatchStartColor.SelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.colorSwatchStartColor.Size = new System.Drawing.Size(27, 19);
            this.colorSwatchStartColor.TabIndex = 10;
            this.colorSwatchStartColor.Text = "colorSwatchStartColor";
            this.colorSwatchStartColor.Click += new System.EventHandler(this.pictureBoxStartColor_Click);
            // 
            // colorSwatchEndColor
            // 
            this.colorSwatchEndColor.Location = new System.Drawing.Point(230, 4);
            this.colorSwatchEndColor.Name = "colorSwatchEndColor";
            this.colorSwatchEndColor.SelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.colorSwatchEndColor.Size = new System.Drawing.Size(27, 19);
            this.colorSwatchEndColor.TabIndex = 11;
            this.colorSwatchEndColor.Text = "colorSwatchEndColor";
            this.colorSwatchEndColor.Click += new System.EventHandler(this.pictureBoxEndColor_Click);
            // 
            // ColorRangeControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.colorSwatchEndColor);
            this.Controls.Add(this.colorSwatchStartColor);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Name = "ColorRangeControl";
            this.Size = new System.Drawing.Size(259, 26);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private ColorSwatch colorSwatchStartColor;
        private ColorSwatch colorSwatchEndColor;
    }
}
