using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ParticleEditor
{
    public partial class Vector2Control : UserControl, INotifyPropertyChanged
    {
        private PointF _point = new PointF(0,0);

        [Description("Vector"), Category("Data"),
        Bindable(true)]
        public PointF Point
        {
            get { return _point; }
            set
            {
                _point = value; 
                OnPropertyChanged("Point");
                OnPropertyChanged("X");
                OnPropertyChanged("Y");
            }
        }

        [Description("X component"), Category("Data"),
        Bindable(true)]
        public float X
        {
            get { return Point.X; }
            set
            {
                Point = new PointF(value, Point.Y);
            }
        }

        [Description("Y Component"), Category("Data"),
        Bindable(true)]
        public float Y
        {
            get { return Point.Y; }
            set
            {
                Point = new PointF(Point.X, value);
            }
        }
        
        public Vector2Control()
        {
            InitializeComponent();
            numericUpDownX.DataBindings.Add(new Binding("Value", this, "X", true));
            numericUpDownY.DataBindings.Add(new Binding("Value", this, "Y", true));
        }

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
