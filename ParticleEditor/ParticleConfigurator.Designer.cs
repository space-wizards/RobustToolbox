using ParticleEditor;

namespace ParticleEditor
{
    partial class ParticleConfigurator
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
            ParticleEditor.Properties.Settings settings1 = new ParticleEditor.Properties.Settings();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ParticleConfigurator));
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxParticleEmitRate = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkShowFPS = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.numericUpDownColorVariation = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.ApplyButton = new System.Windows.Forms.Button();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxMaxDisplayedParticles = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.trackBarMaxDisplayedParticles = new System.Windows.Forms.TrackBar();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label10 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label13 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.label14 = new System.Windows.Forms.Label();
            this.label15 = new System.Windows.Forms.Label();
            this.label16 = new System.Windows.Forms.Label();
            this.variedFloatControlLifetime = new ParticleEditor.VariedFloatControl();
            this.variedFloatRangeControlSize = new ParticleEditor.VariedFloatRangeControl();
            this.variedFloatRangeControlSpin = new ParticleEditor.VariedFloatRangeControl();
            this.variedVector2Velocity = new ParticleEditor.VariedVector2Control();
            this.variedVector2Acceleration = new ParticleEditor.VariedVector2Control();
            this.variedFloatControlRadialVelocity = new ParticleEditor.VariedFloatControl();
            this.variedFloatControlRadialAcceleration = new ParticleEditor.VariedFloatControl();
            this.variedFloatControlTangentialVelocity = new ParticleEditor.VariedFloatControl();
            this.variedFloatControlTangentialAcceleration = new ParticleEditor.VariedFloatControl();
            this.vector2ControlEmitterPosition = new ParticleEditor.Vector2Control();
            this.floatRangeControlEmitRadius = new ParticleEditor.FloatRangeControl();
            this.vector2ControlEmitOffset = new ParticleEditor.Vector2Control();
            this.colorRangeControl1 = new ParticleEditor.ColorRangeControl();
            this.colorSwatchBackgroundColor = new ParticleEditor.ColorSwatch();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownColorVariation)).BeginInit();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarMaxDisplayedParticles)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(114, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(132, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Screen Background Color:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 22);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(116, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Particle Emit Rate (p/s)";
            // 
            // textBoxParticleEmitRate
            // 
            this.textBoxParticleEmitRate.Location = new System.Drawing.Point(220, 19);
            this.textBoxParticleEmitRate.Name = "textBoxParticleEmitRate";
            this.textBoxParticleEmitRate.Size = new System.Drawing.Size(100, 20);
            this.textBoxParticleEmitRate.TabIndex = 5;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.colorSwatchBackgroundColor);
            this.groupBox1.Controls.Add(this.checkShowFPS);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Location = new System.Drawing.Point(3, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(340, 41);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Display Options";
            // 
            // checkShowFPS
            // 
            this.checkShowFPS.AutoSize = true;
            this.checkShowFPS.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
            settings1.BackgroundColor = System.Drawing.Color.Black;
            settings1.ParticleRate = 1;
            settings1.SettingsKey = "";
            settings1.ShowFPS = false;
            this.checkShowFPS.Checked = settings1.ShowFPS;
            this.checkShowFPS.DataBindings.Add(new System.Windows.Forms.Binding("Checked", settings1, "ShowFPS", true, System.Windows.Forms.DataSourceUpdateMode.OnPropertyChanged));
            this.checkShowFPS.Location = new System.Drawing.Point(7, 18);
            this.checkShowFPS.Name = "checkShowFPS";
            this.checkShowFPS.Size = new System.Drawing.Size(79, 17);
            this.checkShowFPS.TabIndex = 0;
            this.checkShowFPS.Text = "Show FPS:";
            this.checkShowFPS.UseVisualStyleBackColor = true;
            this.checkShowFPS.CheckStateChanged += new System.EventHandler(this.checkShowFPS_CheckStateChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.colorRangeControl1);
            this.groupBox2.Controls.Add(this.numericUpDownColorVariation);
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Location = new System.Drawing.Point(3, 59);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(340, 72);
            this.groupBox2.TabIndex = 7;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Colors";
            // 
            // numericUpDownColorVariation
            // 
            this.numericUpDownColorVariation.Location = new System.Drawing.Point(108, 46);
            this.numericUpDownColorVariation.Maximum = new decimal(new int[] {
            255,
            0,
            0,
            0});
            this.numericUpDownColorVariation.Name = "numericUpDownColorVariation";
            this.numericUpDownColorVariation.Size = new System.Drawing.Size(53, 20);
            this.numericUpDownColorVariation.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(27, 48);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(78, 13);
            this.label5.TabIndex = 7;
            this.label5.Text = "Color Variation:";
            // 
            // ApplyButton
            // 
            this.ApplyButton.Location = new System.Drawing.Point(219, 742);
            this.ApplyButton.Name = "ApplyButton";
            this.ApplyButton.Size = new System.Drawing.Size(75, 23);
            this.ApplyButton.TabIndex = 8;
            this.ApplyButton.Text = "Apply";
            this.ApplyButton.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.tableLayoutPanel2);
            this.groupBox3.Controls.Add(this.textBoxMaxDisplayedParticles);
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.trackBarMaxDisplayedParticles);
            this.groupBox3.Controls.Add(this.textBoxParticleEmitRate);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.trackBar1);
            this.groupBox3.Location = new System.Drawing.Point(3, 137);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(340, 211);
            this.groupBox3.TabIndex = 9;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Emitter Options";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 70);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(85, 33);
            this.label4.TabIndex = 22;
            this.label4.Text = "Emit Radius:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // textBoxMaxDisplayedParticles
            // 
            this.textBoxMaxDisplayedParticles.Location = new System.Drawing.Point(220, 60);
            this.textBoxMaxDisplayedParticles.Name = "textBoxMaxDisplayedParticles";
            this.textBoxMaxDisplayedParticles.Size = new System.Drawing.Size(100, 20);
            this.textBoxMaxDisplayedParticles.TabIndex = 20;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(143, 13);
            this.label3.TabIndex = 19;
            this.label3.Text = "Maximum Displayed Particles";
            // 
            // trackBarMaxDisplayedParticles
            // 
            this.trackBarMaxDisplayedParticles.AutoSize = false;
            this.trackBarMaxDisplayedParticles.Location = new System.Drawing.Point(6, 79);
            this.trackBarMaxDisplayedParticles.Maximum = 1000;
            this.trackBarMaxDisplayedParticles.Minimum = 1;
            this.trackBarMaxDisplayedParticles.Name = "trackBarMaxDisplayedParticles";
            this.trackBarMaxDisplayedParticles.Size = new System.Drawing.Size(314, 22);
            this.trackBarMaxDisplayedParticles.TabIndex = 18;
            this.trackBarMaxDisplayedParticles.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBarMaxDisplayedParticles.Value = 1;
            // 
            // label7
            // 
            this.label7.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 37);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(85, 33);
            this.label7.TabIndex = 15;
            this.label7.Text = "Emit Offset:";
            this.label7.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(85, 37);
            this.label6.TabIndex = 10;
            this.label6.Text = "Emitter Position:";
            this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // trackBar1
            // 
            this.trackBar1.AutoSize = false;
            this.trackBar1.Location = new System.Drawing.Point(6, 38);
            this.trackBar1.Maximum = 1000;
            this.trackBar1.Minimum = 1;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(314, 22);
            this.trackBar1.TabIndex = 3;
            this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBar1.Value = 1;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.tableLayoutPanel1);
            this.groupBox4.Location = new System.Drawing.Point(3, 511);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(340, 225);
            this.groupBox4.TabIndex = 10;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Movement Options";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28.17337F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 71.82662F));
            this.tableLayoutPanel1.Controls.Add(this.label10, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label11, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.label12, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label13, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.variedVector2Velocity, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label9, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.variedVector2Acceleration, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label8, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.variedFloatControlRadialVelocity, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.variedFloatControlRadialAcceleration, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.variedFloatControlTangentialVelocity, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.variedFloatControlTangentialAcceleration, 1, 5);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(10, 19);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 6;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(323, 200);
            this.tableLayoutPanel1.TabIndex = 3;
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(3, 66);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(84, 31);
            this.label10.TabIndex = 4;
            this.label10.Text = "Radial Velocity:";
            this.label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(3, 97);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(84, 31);
            this.label11.TabIndex = 5;
            this.label11.Text = "Radial Accel.:";
            this.label11.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label12
            // 
            this.label12.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(3, 128);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(84, 31);
            this.label12.TabIndex = 6;
            this.label12.Text = "Tang. Velocity:";
            this.label12.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label13
            // 
            this.label13.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(3, 159);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(84, 41);
            this.label13.TabIndex = 7;
            this.label13.Text = "Tangent Accel.:";
            this.label13.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label9
            // 
            this.label9.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 33);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(84, 33);
            this.label9.TabIndex = 1;
            this.label9.Text = "Acceleration:";
            this.label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label8
            // 
            this.label8.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 0);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(84, 33);
            this.label8.TabIndex = 0;
            this.label8.Text = "Velocity:";
            this.label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.tableLayoutPanel3);
            this.groupBox5.Location = new System.Drawing.Point(3, 354);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(340, 121);
            this.groupBox5.TabIndex = 11;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Particle Options";
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.AutoSize = true;
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28.39506F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 71.60494F));
            this.tableLayoutPanel2.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel2.Controls.Add(this.label6, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.label7, 0, 1);
            this.tableLayoutPanel2.Controls.Add(this.vector2ControlEmitterPosition, 1, 0);
            this.tableLayoutPanel2.Controls.Add(this.floatRangeControlEmitRadius, 1, 2);
            this.tableLayoutPanel2.Controls.Add(this.vector2ControlEmitOffset, 1, 1);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(10, 107);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 3;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(324, 103);
            this.tableLayoutPanel2.TabIndex = 21;
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.AutoSize = true;
            this.tableLayoutPanel3.ColumnCount = 2;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 28.74618F));
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 71.25382F));
            this.tableLayoutPanel3.Controls.Add(this.label14, 0, 0);
            this.tableLayoutPanel3.Controls.Add(this.label15, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.label16, 0, 2);
            this.tableLayoutPanel3.Controls.Add(this.variedFloatControlLifetime, 1, 0);
            this.tableLayoutPanel3.Controls.Add(this.variedFloatRangeControlSize, 1, 1);
            this.tableLayoutPanel3.Controls.Add(this.variedFloatRangeControlSpin, 1, 2);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(6, 20);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 3;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(327, 97);
            this.tableLayoutPanel3.TabIndex = 0;
            // 
            // label14
            // 
            this.label14.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(3, 0);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(88, 31);
            this.label14.TabIndex = 0;
            this.label14.Text = "Lifetime:";
            this.label14.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label15
            // 
            this.label15.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(3, 31);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(88, 33);
            this.label15.TabIndex = 1;
            this.label15.Text = "Size:";
            this.label15.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label16
            // 
            this.label16.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(3, 64);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(88, 33);
            this.label16.TabIndex = 2;
            this.label16.Text = "Spin:";
            this.label16.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // variedFloatControlLifetime
            // 
            this.variedFloatControlLifetime.Location = new System.Drawing.Point(97, 3);
            this.variedFloatControlLifetime.Name = "variedFloatControlLifetime";
            this.variedFloatControlLifetime.Size = new System.Drawing.Size(143, 25);
            this.variedFloatControlLifetime.TabIndex = 3;
            this.variedFloatControlLifetime.Value = 0F;
            this.variedFloatControlLifetime.Variation = 0F;
            // 
            // variedFloatRangeControlSize
            // 
            this.variedFloatRangeControlSize.Location = new System.Drawing.Point(97, 34);
            this.variedFloatRangeControlSize.Name = "variedFloatRangeControlSize";
            this.variedFloatRangeControlSize.Range = ((System.Drawing.PointF)(resources.GetObject("variedFloatRangeControlSize.Range")));
            this.variedFloatRangeControlSize.Size = new System.Drawing.Size(221, 27);
            this.variedFloatRangeControlSize.TabIndex = 4;
            this.variedFloatRangeControlSize.Variation = 0F;
            this.variedFloatRangeControlSize.X = 0F;
            this.variedFloatRangeControlSize.Y = 0F;
            // 
            // variedFloatRangeControlSpin
            // 
            this.variedFloatRangeControlSpin.Location = new System.Drawing.Point(97, 67);
            this.variedFloatRangeControlSpin.Name = "variedFloatRangeControlSpin";
            this.variedFloatRangeControlSpin.Range = ((System.Drawing.PointF)(resources.GetObject("variedFloatRangeControlSpin.Range")));
            this.variedFloatRangeControlSpin.Size = new System.Drawing.Size(221, 27);
            this.variedFloatRangeControlSpin.TabIndex = 5;
            this.variedFloatRangeControlSpin.Variation = 0F;
            this.variedFloatRangeControlSpin.X = 0F;
            this.variedFloatRangeControlSpin.Y = 0F;
            // 
            // variedVector2Velocity
            // 
            this.variedVector2Velocity.Location = new System.Drawing.Point(93, 3);
            this.variedVector2Velocity.Name = "variedVector2Velocity";
            this.variedVector2Velocity.Point = ((System.Drawing.PointF)(resources.GetObject("variedVector2Velocity.Point")));
            this.variedVector2Velocity.Size = new System.Drawing.Size(217, 27);
            this.variedVector2Velocity.TabIndex = 8;
            this.variedVector2Velocity.Variation = 0F;
            this.variedVector2Velocity.X = 0F;
            this.variedVector2Velocity.Y = 0F;
            // 
            // variedVector2Acceleration
            // 
            this.variedVector2Acceleration.Location = new System.Drawing.Point(93, 36);
            this.variedVector2Acceleration.Name = "variedVector2Acceleration";
            this.variedVector2Acceleration.Point = ((System.Drawing.PointF)(resources.GetObject("variedVector2Acceleration.Point")));
            this.variedVector2Acceleration.Size = new System.Drawing.Size(217, 27);
            this.variedVector2Acceleration.TabIndex = 9;
            this.variedVector2Acceleration.Variation = 0F;
            this.variedVector2Acceleration.X = 0F;
            this.variedVector2Acceleration.Y = 0F;
            // 
            // variedFloatControlRadialVelocity
            // 
            this.variedFloatControlRadialVelocity.Location = new System.Drawing.Point(93, 69);
            this.variedFloatControlRadialVelocity.Name = "variedFloatControlRadialVelocity";
            this.variedFloatControlRadialVelocity.Size = new System.Drawing.Size(143, 25);
            this.variedFloatControlRadialVelocity.TabIndex = 14;
            this.variedFloatControlRadialVelocity.Value = 0F;
            this.variedFloatControlRadialVelocity.Variation = 0F;
            // 
            // variedFloatControlRadialAcceleration
            // 
            this.variedFloatControlRadialAcceleration.Location = new System.Drawing.Point(93, 100);
            this.variedFloatControlRadialAcceleration.Name = "variedFloatControlRadialAcceleration";
            this.variedFloatControlRadialAcceleration.Size = new System.Drawing.Size(143, 25);
            this.variedFloatControlRadialAcceleration.TabIndex = 15;
            this.variedFloatControlRadialAcceleration.Value = 0F;
            this.variedFloatControlRadialAcceleration.Variation = 0F;
            // 
            // variedFloatControlTangentialVelocity
            // 
            this.variedFloatControlTangentialVelocity.Location = new System.Drawing.Point(93, 131);
            this.variedFloatControlTangentialVelocity.Name = "variedFloatControlTangentialVelocity";
            this.variedFloatControlTangentialVelocity.Size = new System.Drawing.Size(143, 25);
            this.variedFloatControlTangentialVelocity.TabIndex = 16;
            this.variedFloatControlTangentialVelocity.Value = 0F;
            this.variedFloatControlTangentialVelocity.Variation = 0F;
            // 
            // variedFloatControlTangentialAcceleration
            // 
            this.variedFloatControlTangentialAcceleration.Location = new System.Drawing.Point(93, 162);
            this.variedFloatControlTangentialAcceleration.Name = "variedFloatControlTangentialAcceleration";
            this.variedFloatControlTangentialAcceleration.Size = new System.Drawing.Size(143, 25);
            this.variedFloatControlTangentialAcceleration.TabIndex = 17;
            this.variedFloatControlTangentialAcceleration.Value = 0F;
            this.variedFloatControlTangentialAcceleration.Variation = 0F;
            // 
            // vector2ControlEmitterPosition
            // 
            this.vector2ControlEmitterPosition.Location = new System.Drawing.Point(94, 3);
            this.vector2ControlEmitterPosition.Name = "vector2ControlEmitterPosition";
            this.vector2ControlEmitterPosition.Point = ((System.Drawing.PointF)(resources.GetObject("vector2ControlEmitterPosition.Point")));
            this.vector2ControlEmitterPosition.Size = new System.Drawing.Size(137, 31);
            this.vector2ControlEmitterPosition.TabIndex = 16;
            this.vector2ControlEmitterPosition.X = 0F;
            this.vector2ControlEmitterPosition.Y = 0F;
            // 
            // floatRangeControlEmitRadius
            // 
            this.floatRangeControlEmitRadius.End = 0F;
            this.floatRangeControlEmitRadius.Location = new System.Drawing.Point(94, 73);
            this.floatRangeControlEmitRadius.Name = "floatRangeControlEmitRadius";
            this.floatRangeControlEmitRadius.Range = ((System.Drawing.PointF)(resources.GetObject("floatRangeControlEmitRadius.Range")));
            this.floatRangeControlEmitRadius.Size = new System.Drawing.Size(137, 27);
            this.floatRangeControlEmitRadius.Start = 0F;
            this.floatRangeControlEmitRadius.TabIndex = 21;
            // 
            // vector2ControlEmitOffset
            // 
            this.vector2ControlEmitOffset.Location = new System.Drawing.Point(94, 40);
            this.vector2ControlEmitOffset.Name = "vector2ControlEmitOffset";
            this.vector2ControlEmitOffset.Point = ((System.Drawing.PointF)(resources.GetObject("vector2ControlEmitOffset.Point")));
            this.vector2ControlEmitOffset.Size = new System.Drawing.Size(137, 27);
            this.vector2ControlEmitOffset.TabIndex = 17;
            this.vector2ControlEmitOffset.X = 0F;
            this.vector2ControlEmitOffset.Y = 0F;
            // 
            // colorRangeControl1
            // 
            this.colorRangeControl1.EndColor = System.Drawing.Color.Black;
            this.colorRangeControl1.Location = new System.Drawing.Point(7, 18);
            this.colorRangeControl1.Name = "colorRangeControl1";
            this.colorRangeControl1.Size = new System.Drawing.Size(259, 26);
            this.colorRangeControl1.StartColor = System.Drawing.Color.Black;
            this.colorRangeControl1.TabIndex = 9;
            // 
            // colorSwatchBackgroundColor
            // 
            this.colorSwatchBackgroundColor.Location = new System.Drawing.Point(251, 14);
            this.colorSwatchBackgroundColor.Name = "colorSwatchBackgroundColor";
            this.colorSwatchBackgroundColor.SelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.colorSwatchBackgroundColor.Size = new System.Drawing.Size(20, 20);
            this.colorSwatchBackgroundColor.TabIndex = 3;
            this.colorSwatchBackgroundColor.Text = "colorSwatchBackgroundColor";
            this.colorSwatchBackgroundColor.Click += new System.EventHandler(this.colorSwatchBackgroundColor_Click);
            // 
            // ParticleConfigurator
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox5);
            this.Controls.Add(this.groupBox4);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.ApplyButton);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Name = "ParticleConfigurator";
            this.Size = new System.Drawing.Size(346, 768);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownColorVariation)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBarMaxDisplayedParticles)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.tableLayoutPanel2.PerformLayout();
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.CheckBox checkShowFPS;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxParticleEmitRate;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button ApplyButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.NumericUpDown numericUpDownColorVariation;
        private ColorRangeControl colorRangeControl1;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private Vector2Control vector2ControlEmitOffset;
        private Vector2Control vector2ControlEmitterPosition;
        private ColorSwatch colorSwatchBackgroundColor;
        private System.Windows.Forms.TextBox textBoxMaxDisplayedParticles;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TrackBar trackBarMaxDisplayedParticles;
        private System.Windows.Forms.Label label4;
        private FloatRangeControl floatRangeControlEmitRadius;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.Label label13;
        private VariedVector2Control variedVector2Velocity;
        private VariedVector2Control variedVector2Acceleration;
        private VariedFloatControl variedFloatControlRadialVelocity;
        private VariedFloatControl variedFloatControlRadialAcceleration;
        private VariedFloatControl variedFloatControlTangentialVelocity;
        private VariedFloatControl variedFloatControlTangentialAcceleration;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.Label label16;
        private VariedFloatControl variedFloatControlLifetime;
        private VariedFloatRangeControl variedFloatRangeControlSize;
        private VariedFloatRangeControl variedFloatRangeControlSpin;

    }
}