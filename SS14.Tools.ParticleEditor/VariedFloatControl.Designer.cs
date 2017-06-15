namespace SS14.Tools.ParticleEditor
{
    partial class VariedFloatControl
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
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDownVariation = new System.Windows.Forms.NumericUpDown();
            this.numericUpDownValue = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownVariation)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownValue)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(65, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(18, 20);
            this.label1.TabIndex = 4;
            this.label1.Text = "±";
            // 
            // numericUpDownVariation
            // 
            this.numericUpDownVariation.DecimalPlaces = 2;
            this.numericUpDownVariation.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDownVariation.Location = new System.Drawing.Point(84, 3);
            this.numericUpDownVariation.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownVariation.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.numericUpDownVariation.Name = "numericUpDownVariation";
            this.numericUpDownVariation.Size = new System.Drawing.Size(58, 20);
            this.numericUpDownVariation.TabIndex = 3;
            // 
            // numericUpDownValue
            // 
            this.numericUpDownValue.DecimalPlaces = 2;
            this.numericUpDownValue.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numericUpDownValue.Location = new System.Drawing.Point(4, 3);
            this.numericUpDownValue.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericUpDownValue.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.numericUpDownValue.Name = "numericUpDownValue";
            this.numericUpDownValue.Size = new System.Drawing.Size(60, 20);
            this.numericUpDownValue.TabIndex = 5;
            // 
            // VariedFloatControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.numericUpDownValue);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDownVariation);
            this.Name = "VariedFloatControl";
            this.Size = new System.Drawing.Size(143, 25);
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownVariation)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownValue)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDownVariation;
        private System.Windows.Forms.NumericUpDown numericUpDownValue;
    }
}
