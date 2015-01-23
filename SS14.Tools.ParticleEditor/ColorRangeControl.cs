using Cyotek.Windows.Forms;
using SS14.Shared.Utility;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;

namespace SS14.Tools.ParticleEditor
{
    public partial class ColorRangeControl : UserControl, INotifyPropertyChanged
    {
        #region variables

        private Range<Color> _colorRange = new Range<Color>(Color.Black, Color.Transparent);
        #endregion

        [Description("Color range"), Category("Data"),
        Bindable(true)]
        public Range<Color> ColorRange
        {
            get { return _colorRange; }
            set
            {
                _colorRange = value;
                colorSwatchStartColor.SelectedColor = value.Start;
                colorSwatchEndColor.SelectedColor = value.End;
                OnPropertyChanged("ColorRange");
            }
        }
            
        [Description("Starting color of the range"), Category("Data"),
        Browsable(true), Bindable(true), EditorBrowsable(EditorBrowsableState.Always),
        EditorAttribute("ParticleEditor.ColorPickerDoohickey", typeof(UITypeEditor)),
        TypeConverter(typeof(MyColorConverter))]
        public Color StartColor
        {
            get { return _colorRange.Start; }
            set { 
                ColorRange.Start = value;
                colorSwatchStartColor.SelectedColor = value;
                OnPropertyChanged("ColorRange");
            }
        }

        [Description("Ending color of the range"), Category("Data"),
        Browsable(true), Bindable(true), EditorBrowsable(EditorBrowsableState.Always),
        EditorAttribute("ParticleEditor.ColorPickerDoohickey", typeof(UITypeEditor)),
        TypeConverter(typeof(MyColorConverter))]
        public Color EndColor
        {
            get { return _colorRange.End; }
            set {
                ColorRange.End = value;
                colorSwatchEndColor.SelectedColor = value;
                OnPropertyChanged("ColorRange");
            }
        }

        private void pictureBoxStartColor_Click(object sender, EventArgs e)
        {
            var dialog = new ColorPickerDialog();
            dialog.Color = StartColor;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                StartColor = dialog.Color;
            }
        }

        private void pictureBoxEndColor_Click(object sender, EventArgs e)
        {
            var dialog = new ColorPickerDialog();
            dialog.Color = EndColor;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                EndColor = dialog.Color;
            }
        }

        public ColorRangeControl()
        {
            InitializeComponent();
            StartColor = Color.Black;
            EndColor = Color.Black;
        }

        private void OnPropertyChanged(string property)
        {
            if(PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
