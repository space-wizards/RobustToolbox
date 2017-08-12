using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SS14.Tools.ParticleEditor
{
    public partial class VariedFloatRangeControl : UserControl, INotifyPropertyChanged
    {
        private float _variation;

        [Description("Range"), Category("Data"),
        Bindable(true)]
        public PointF Range
        {
            get { return floatRangeControlRange.Range; }
            set
            {
                floatRangeControlRange.Range = value;
                OnPropertyChanged("Range");
                OnPropertyChanged("Start");
                OnPropertyChanged("End");
            }
        }

        [Description("Start"), Category("Data"),
        Bindable(true)]
        public float Start
        {
            get { return Range.X; }
            set
            {
                Range = new PointF(value, Range.Y);
            }
        }

        [Description("End"), Category("Data"),
        Bindable(true)]
        public float End
        {
            get { return Range.Y; }
            set
            {
                Range = new PointF(Range.X, value);
            }
        }

        [Description("Variation"), Category("Data"),
        Bindable(true)]
        public float Variation
        {
            get { return _variation; }
            set
            {
                _variation = value;
                OnPropertyChanged("Variation");
            }
        }
        
        public VariedFloatRangeControl()
        {
            InitializeComponent();
            floatRangeControlRange.DataBindings.Add(new Binding("Range", this, "Range", true));
            numericUpDown1.DataBindings.Add(new Binding("Value", this, "Variation", true));
        }

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
