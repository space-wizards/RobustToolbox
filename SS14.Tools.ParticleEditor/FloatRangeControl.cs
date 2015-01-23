using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SS14.Tools.ParticleEditor
{
    public partial class FloatRangeControl : UserControl, INotifyPropertyChanged
    {
        private PointF _range = new PointF(0,0);

        [Description("Range"), Category("Data"),
        Bindable(true)]
        public PointF Range
        {
            get { return _range; }
            set
            {
                _range = value;
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
        
        public FloatRangeControl()
        {
            InitializeComponent();
            numericUpDownX.DataBindings.Add(new Binding("Value", this, "Start", true));
            numericUpDownY.DataBindings.Add(new Binding("Value", this, "End", true));
        }

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
