using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GorgonLibrary;

namespace ParticleEditor
{
    public partial class ColorSwatch : Control
    {
        #region Variables
        private Color _selectedColor = Color.FromArgb(255,0,0,0);
        #endregion

        #region Properties
        [EditorAttribute("ParticleEditor.ColorPickerDoohickey", typeof(UITypeEditor)),
        TypeConverter(typeof(MyColorConverter))]
        public Color SelectedColor
        {
            get { return _selectedColor; }
            set { _selectedColor = value; Invalidate(); }
        }
        #endregion

        #region Methods
        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            var c1 = Color.FromArgb(0,255,255,255);
            c1 = AddColors(MultiplyColor(SelectedColor.A, SelectedColor), MultiplyColor(255 - SelectedColor.A, c1));
            c1 = Color.FromArgb(255, c1.R, c1.G, c1.B);
            var c2 = Color.FromArgb(0, Color.Gray.R, Color.Gray.G, Color.Gray.B);
            c2 = AddColors(MultiplyColor(SelectedColor.A, SelectedColor), MultiplyColor(255 - SelectedColor.A, c2));
            c2 = Color.FromArgb(255, c2.R, c2.G, c2.B);
            //Draw checkerboard
            Brush hatchBrush =
                new HatchBrush(HatchStyle.LargeCheckerBoard, c1, c2);
            pe.Graphics.FillRectangle(hatchBrush, ClientRectangle);
        }

        protected Color MultiplyColor(int mul, Color c)
        {
            return Color.FromArgb(MulColorVal(mul, c.A),
                                  MulColorVal(mul, c.R),
                                  MulColorVal(mul, c.G),
                                  MulColorVal(mul, c.B)
                );
        }

        protected Color AddColors(Color a, Color b)
        {
            return Color.FromArgb(ClampColorVal(a.A + b.A),
                                  ClampColorVal(a.R + b.R),
                                  ClampColorVal(a.G + b.G),
                                  ClampColorVal(a.B + b.B)
                );
        }

        protected int ClampColorVal(int val)
        {
            return Math.Max(0, Math.Min(val, 255));
        }

        protected float ColorToFloat(int val)
        {
            return ClampColorVal(val)/255f;
        }

        protected int ColorFromFloat(float val)
        {
            return ClampColorVal((int)(val*255));
        }


        protected int MulColorVal(int mul, int val)
        {
            mul = ClampColorVal(mul);
            val = ClampColorVal(val);
            var product = ColorFromFloat(ColorToFloat(mul) * ColorToFloat(val));
            return product;
        }
        #endregion

        
        #region Constructor
        public ColorSwatch()
        {
            InitializeComponent();
        }
        #endregion
    }
}
